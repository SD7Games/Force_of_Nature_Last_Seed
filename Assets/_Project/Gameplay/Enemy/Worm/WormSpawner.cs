using System.Collections;
using System.Collections.Generic;
using LastSeed.Gameplay.Signals;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public sealed class WormSpawner : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private WormController _wormController;

    [SerializeField] private WormCombatController _wormCombat;
    [SerializeField] private WormSectionHpPresenter _hpPresenter;

    [Header("Rewards")]
    [SerializeField] private RewardDatabase _rewardDatabase;

    private WormSegmentPool _segmentPool;
    private WormFactory _wormFactory;
    private WormSpawnSettings _spawnSettings;
    private WormAdaptiveHpController _adaptiveHpController;
    private readonly List<WormSection> _sections = new();
    private readonly List<WormSegment> _spawnedSegments = new();

    private bool _isSpawned;
    private WormFaceBurstPresenter _faceBurstPresenter;
    private SignalBus _signalBus;
    private bool _isSubscribedToSignals;

    [Inject]
    public void Construct(
        SignalBus signalBus,
        WormAdaptiveHpController adaptiveHpController,
        WormSegmentPool segmentPool,
        WormFactory wormFactory,
        WormSpawnSettings spawnSettings,
        WormFaceBurstPresenter faceBurstPresenter)
    {
        _signalBus = signalBus;
        _adaptiveHpController = adaptiveHpController;
        _segmentPool = segmentPool;
        _wormFactory = wormFactory;
        _spawnSettings = spawnSettings;
        _faceBurstPresenter = faceBurstPresenter;
        SubscribeToSignals();
    }

    private void OnEnable()
    {
        SubscribeToSignals();

        if (_isSpawned)
            _faceBurstPresenter?.Bind(GetSpawnedHead()?.FaceVisual);
    }

    private void OnDisable()
    {
        UnsubscribeFromSignals();

        _faceBurstPresenter?.Unbind();
    }

    private IEnumerator Start()
    {
        yield return _segmentPool.PrewarmRoutine(
            _spawnSettings.BodyPoolCapacity,
            _spawnSettings.PrewarmBatchSize);

        SpawnWorm();
    }

    public void SpawnWorm()
    {
        if (_isSpawned)
            return;

        List<WormPatternEntry> pattern =
            WormPatternBuilder.BuildPattern(_spawnSettings.SectionCount);

        List<WormSegment> segments =
            _wormFactory.CreateSegments(
                pattern,
                out WormSegment head,
                out WormSegment tail);

        if (head == null || tail == null)
        {
            Debug.LogError("Worm spawn failed: head or tail missing", this);
            return;
        }

        _spawnedSegments.Clear();
        _spawnedSegments.AddRange(segments);

        List<WormSection> sections =
            WormSectionBuilder.BuildSections(
                segments,
                GetCocoonProfiles());

        _adaptiveHpController.InitializeSections(sections, Time.time);

        _sections.Clear();
        _sections.AddRange(sections);

        _wormFactory.AttachDamageReceivers(segments, _wormCombat);

        _wormController.Init(segments);
        _faceBurstPresenter.Bind(head.FaceVisual);
        _wormCombat.Init(head, tail, sections);
        _hpPresenter.BindSections(sections);

        _isSpawned = true;
    }

    public void RestartWorm()
    {
        DespawnWorm();
        SpawnWorm();
    }

    public void DespawnWorm()
    {
        _faceBurstPresenter?.Unbind();

        _hpPresenter?.Clear();
        _wormCombat?.Clear();
        _wormController?.ClearWorm();

        for (int i = 0; i < _spawnedSegments.Count; i++)
        {
            if (_segmentPool != null)
                _segmentPool.Release(_spawnedSegments[i]);
        }

        _spawnedSegments.Clear();
        _sections.Clear();
        _adaptiveHpController?.Reset(Time.time);
        _isSpawned = false;
    }

    private WormSegment GetSpawnedHead()
    {
        for (int i = 0; i < _spawnedSegments.Count; i++)
        {
            WormSegment segment = _spawnedSegments[i];

            if (segment != null && segment.Type == WormSegmentType.Head)
                return segment;
        }

        return null;
    }

    public void SetRuntimePressureMultiplier(float multiplier)
    {
        _adaptiveHpController.SetRuntimePressureMultiplier(multiplier);
    }

    private void OnWeaponRuntimeStatsChanged(WeaponRuntimeStatsChangedSignal signal)
    {
        _adaptiveHpController.NotifyWeaponRuntimeStatsChanged(signal.OccurredAt);
    }

    private IReadOnlyList<CocoonRewardProfile> GetCocoonProfiles()
    {
        return _rewardDatabase != null
            ? _rewardDatabase.CocoonProfiles
            : CocoonRewardProfile.Defaults;
    }

    private void HandleReviveGranted(WormReviveGrantedSignal signal)
    {
        _adaptiveHpController.NotifyReviveGranted();
    }

    private void SubscribeToSignals()
    {
        if (_signalBus == null || _isSubscribedToSignals || !isActiveAndEnabled)
            return;

        _signalBus.Subscribe<WormReviveGrantedSignal>(HandleReviveGranted);
        _signalBus.Subscribe<WeaponRuntimeStatsChangedSignal>(OnWeaponRuntimeStatsChanged);
        _isSubscribedToSignals = true;
    }

    private void UnsubscribeFromSignals()
    {
        if (_signalBus == null || !_isSubscribedToSignals)
            return;

        _signalBus.Unsubscribe<WormReviveGrantedSignal>(HandleReviveGranted);
        _signalBus.Unsubscribe<WeaponRuntimeStatsChangedSignal>(OnWeaponRuntimeStatsChanged);
        _isSubscribedToSignals = false;
    }
}
