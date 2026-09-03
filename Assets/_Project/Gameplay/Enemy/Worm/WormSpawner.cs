using System.Collections;
using System.Collections.Generic;
using LastSeed.Gameplay.Signals;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

[DisallowMultipleComponent]
public sealed class WormSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private WormSegment _headPrefab;

    [SerializeField] private WormSegment _bodyPrefab;
    [SerializeField] private WormSegment _tailPrefab;

    [Header("Controllers")]
    [SerializeField] private WormController _wormController;

    [SerializeField] private WormCombatController _wormCombat;
    [SerializeField] private WormSectionHpPresenter _hpPresenter;

    [Header("Rewards")]
    [SerializeField] private RewardDatabase _rewardDatabase;

    [Header("Generation")]
    [Tooltip("Gameplay HP sections. Each section contains WormCocoonRules.SectionSize body segments. Head and tail are added separately.")]
    [Min(1)]
    [FormerlySerializedAs("_totalLength")]
    [SerializeField] private int _sectionCount = 9;

    [Header("Pooling")]
    [SerializeField] private int _poolPadding = 10;
    [SerializeField, Min(1)] private int _prewarmBatchSize = 64;

    private WormSegmentPool _segmentPool;
    private WormFactory _wormFactory;
    private WormAdaptiveHpController _adaptiveHpController;
    private readonly List<WormSection> _sections = new();
    private readonly List<WormSegment> _spawnedSegments = new();

    private bool _isSpawned;
    private int _bodyPoolCapacity;
    private WormFaceVisualController _activeFaceVisual;
    private SignalBus _signalBus;
    private bool _isSubscribedToSignals;
    private ProjectileWeapon _weapon;
    private AcaciaThornWeapon _acaciaThornWeapon;

    [Inject]
    public void Construct(
        SignalBus signalBus,
        WormAdaptiveHpController adaptiveHpController,
        ProjectileWeapon weapon,
        AcaciaThornWeapon acaciaThornWeapon)
    {
        _signalBus = signalBus;
        _adaptiveHpController = adaptiveHpController;
        _weapon = weapon;
        _acaciaThornWeapon = acaciaThornWeapon;
        SubscribeToSignals();
    }

#if UNITY_EDITOR
    public int EditorSectionCount => _sectionCount;
#endif

    private void OnEnable()
    {
        SubscribeToSignals();

        if (_weapon != null)
            _weapon.RuntimeStatsChanged += OnWeaponRuntimeStatsChanged;

        if (_acaciaThornWeapon != null)
            _acaciaThornWeapon.RuntimeStatsChanged += OnWeaponRuntimeStatsChanged;

        if (_isSpawned)
            BindWormFace(GetSpawnedHead());
    }

    private void OnDisable()
    {
        UnsubscribeFromSignals();

        if (_weapon != null)
            _weapon.RuntimeStatsChanged -= OnWeaponRuntimeStatsChanged;

        if (_acaciaThornWeapon != null)
            _acaciaThornWeapon.RuntimeStatsChanged -= OnWeaponRuntimeStatsChanged;

        UnbindWormFace();
    }

    private void Awake()
    {
        if (_wormController == null)
            Debug.LogError("WormController not assigned", this);

        if (_wormCombat == null)
            Debug.LogError("WormCombatController not assigned", this);

        _bodyPoolCapacity = WormPatternBuilder.GetBodySegmentCount(_sectionCount) + _poolPadding;
        _segmentPool = new WormSegmentPool(
            transform,
            _headPrefab,
            _bodyPrefab,
            _tailPrefab);

        _wormFactory = new WormFactory(_segmentPool);
    }

    private IEnumerator Start()
    {
        if (_segmentPool != null)
            yield return _segmentPool.PrewarmRoutine(_bodyPoolCapacity, _prewarmBatchSize);

        SpawnWorm();
    }

    public void SpawnWorm()
    {
        if (_isSpawned)
            return;

        List<WormPatternEntry> pattern =
            WormPatternBuilder.BuildPattern(_sectionCount);

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
        BindWormFace(head);
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
        UnbindWormFace();

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

    private void BindWormFace(WormSegment head)
    {
        UnbindWormFace();

        if (_wormController == null || head == null)
            return;

        _activeFaceVisual = head.GetComponentInChildren<WormFaceVisualController>(true);
        if (_activeFaceVisual == null)
            return;

        _activeFaceVisual.SetBoostActive(_wormController.IsCombatBurstActive);
        _wormController.CombatBurstStateChanged += OnCombatBurstStateChanged;
    }

    private void UnbindWormFace()
    {
        if (_wormController != null)
            _wormController.CombatBurstStateChanged -= OnCombatBurstStateChanged;

        if (_activeFaceVisual != null)
            _activeFaceVisual.SetBoostActive(false);

        _activeFaceVisual = null;
    }

    private void OnCombatBurstStateChanged(bool isActive)
    {
        if (_activeFaceVisual != null)
            _activeFaceVisual.SetBoostActive(isActive);
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

    private void OnWeaponRuntimeStatsChanged()
    {
        _adaptiveHpController.NotifyWeaponRuntimeStatsChanged(Time.time);
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
        _isSubscribedToSignals = true;
    }

    private void UnsubscribeFromSignals()
    {
        if (_signalBus == null || !_isSubscribedToSignals)
            return;

        _signalBus.Unsubscribe<WormReviveGrantedSignal>(HandleReviveGranted);
        _isSubscribedToSignals = false;
    }
}
