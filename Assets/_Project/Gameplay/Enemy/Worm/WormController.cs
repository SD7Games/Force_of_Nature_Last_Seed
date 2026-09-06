using System;
using System.Collections.Generic;
using LastSeed.Core.Collections;
using UnityEngine;
using Zenject;

public sealed class WormController : MonoBehaviour, IWormPathProgressProvider
{
    [Header("Rail")]
    [SerializeField] private RailPath _rail;
    [SerializeField] private WormMovementConfig _movementConfig;

    private WormCombatBurstController _combatBurstController;
    private WormFrameSimulation _frameSimulation;
    private WormRailTargetResolver _railTargetResolver;
    private WormPathProgressState _pathProgress;
    private WormSegmentChainPresenter _segmentChainPresenter;
    private WormReviveSequence _reviveSequence;
    private OrderedReferenceSet<WormSegment> _segmentChain;
    private WormSectionRollbackState<WormSegment> _sectionRollbackState;
    private float _waveTime;

    public bool HasWorm => _segmentChain != null && _segmentChain.Count > 0;
    public bool IsCatchingUpToCombatStart =>
        _pathProgress != null && _pathProgress.IsCatchingUp;
    public bool IsCombatBurstActive =>
        _combatBurstController != null && _combatBurstController.IsActive;

    [Inject]
    public void Construct(
        WormCombatBurstController combatBurstController,
        WormFrameSimulation frameSimulation,
        WormRailTargetResolver railTargetResolver,
        WormPathProgressState pathProgress,
        WormSegmentChainPresenter segmentChainPresenter,
        WormReviveSequence reviveSequence,
        OrderedReferenceSet<WormSegment> segmentChain,
        WormSectionRollbackState<WormSegment> sectionRollbackState)
    {
        _combatBurstController = combatBurstController;
        _frameSimulation = frameSimulation;
        _railTargetResolver = railTargetResolver;
        _pathProgress = pathProgress;
        _segmentChainPresenter = segmentChainPresenter;
        _reviveSequence = reviveSequence;
        _segmentChain = segmentChain;
        _sectionRollbackState = sectionRollbackState;

        if (_rail == null)
            throw new InvalidOperationException($"{nameof(WormController)} on '{name}' requires a rail path.");

        if (_movementConfig == null)
            throw new InvalidOperationException($"{nameof(WormController)} on '{name}' requires a movement config.");
    }

    public float HeadPathProgressNormalized
    {
        get
        {
            if (_rail == null || _rail.TotalLength <= 0f)
                return 0f;

            return Mathf.Clamp01(_pathProgress.HeadDistance / _rail.TotalLength);
        }
    }

    public float HeadControlPointProgressNormalized
    {
        get
        {
            if (_rail == null || _rail.PointCount <= 1)
                return HeadPathProgressNormalized;

            return _rail.GetControlPointProgressNormalized(_pathProgress.HeadDistance);
        }
    }

    private void OnValidate()
    {
        if (_rail == null)
            Debug.LogError($"{nameof(WormController)} requires a rail path.", this);

        if (_movementConfig == null)
            Debug.LogError($"{nameof(WormController)} requires a movement config.", this);

        ClearTargetDistanceCaches();
    }

    private void OnDestroy()
    {
        _reviveSequence?.Cancel();
    }

    public void Init(List<WormSegment> segments)
    {
        _reviveSequence.Cancel();
        _segmentChain.ReplaceWith(segments);

        _segmentChainPresenter.Reset();

        _sectionRollbackState.Complete();
        _combatBurstController.Reset(_movementConfig.BaseSpeed);
        ClearTargetDistanceCaches();
        _pathProgress.Reset(TryGetCatchUpTargetDistance(out _));

        UpdateSegments();
    }

    public void ClearWorm()
    {
        _reviveSequence.Cancel();
        _segmentChain.Clear();
        _segmentChainPresenter.Reset();
        _sectionRollbackState.Complete();
        _combatBurstController.Reset(_movementConfig.BaseSpeed);
        _pathProgress.Reset();
        ClearTargetDistanceCaches();
    }

    public WormFrameResult Tick(
        float deltaTime,
        float unscaledDeltaTime,
        float time,
        float unscaledTime)
    {
        _waveTime = (_sectionRollbackState.IsActive || _reviveSequence.IsActive
            ? unscaledTime
            : time) * _movementConfig.WaveSpeed;
        WormForwardMotionSettings settings = _movementConfig.CreateForwardMotionSettings();
        WormFrameContext context = new(
            _rail,
            settings,
            BuildSegmentLayout(),
            _movementConfig.BaseSpeed,
            _movementConfig.SectionRollbackForwardSpeedMultiplier,
            _movementConfig.RollbackSpeed,
            deltaTime,
            unscaledDeltaTime);

        bool pathCompleted = _frameSimulation.Tick(context);
        return new WormFrameResult(
            pathCompleted,
            pathCompleted ? HeadPathProgressNormalized : 0f);
    }

    private bool TryGetCatchUpTargetDistance(out float targetDistance)
    {
        return _railTargetResolver.TryGetCatchUpDistance(
            _rail,
            _movementConfig.CatchUpRailPointIndex,
            out targetDistance);
    }

    private bool TryGetReviveRollbackTargetDistance(out float targetDistance)
    {
        return _railTargetResolver.TryGetReviveDistance(
            _rail,
            _movementConfig.ReviveRollbackRailPointIndex,
            _movementConfig.CatchUpRailPointIndex,
            out targetDistance);
    }

    private void ClearTargetDistanceCaches()
    {
        _railTargetResolver?.Clear();
    }

    private void UpdateSegments()
    {
        _frameSimulation.Render(_rail, BuildSegmentLayout());
    }

    private WormSegmentChainLayout BuildSegmentLayout()
    {
        return _movementConfig.CreateSegmentLayout(
            _pathProgress.HeadDistance,
            _waveTime,
            _reviveSequence.VisualYOffset,
            _sectionRollbackState.IsActive,
            _reviveSequence.IsActive);
    }

    public int RemoveDestroyedSectionSegments(List<WormSegment> destroyed, out int firstRemovedIndex)
    {
        firstRemovedIndex = -1;

        if (destroyed == null || destroyed.Count == 0)
            return 0;

        int removed = _segmentChain.RemoveAll(destroyed, out firstRemovedIndex);
        _sectionRollbackState.Forget(destroyed);
        return removed;
    }

    public void RollbackDestroyedGap(int destroyedCount, int splitIndex)
    {
        if (destroyedCount <= 0)
            return;

        if (splitIndex < 0)
            return;

        if (_reviveSequence.IsActive)
            return;

        _sectionRollbackState.BeginOrExtend(
            _segmentChain.Items,
            splitIndex,
            destroyedCount,
            _pathProgress.HeadDistance,
            _movementConfig.SegmentSpacing);
        _segmentChainPresenter.Reset();
    }

    public bool RollbackToReviveStart(Action onComplete)
    {
        if (_segmentChain.Count == 0 || _rail == null)
            return false;

        float target = GetReviveRollbackTargetDistance();

        _reviveSequence.Cancel();

        ClearSectionRollbackState();
        _pathProgress.ReopenPath();

        if (_pathProgress.HeadDistance <= target)
        {
            _pathProgress.SetHeadDistance(target);
            UpdateSegments();
            onComplete?.Invoke();
            return true;
        }

        _segmentChainPresenter.Reset();
        _reviveSequence.Begin(
            _pathProgress.HeadDistance,
            target,
            _movementConfig.CreateReviveAnimationSettings(),
            _segmentChain.Items,
            onComplete);
        return true;
    }

    private float GetReviveRollbackTargetDistance()
    {
        if (_rail == null)
            return 0f;

        return TryGetReviveRollbackTargetDistance(out float targetDistance)
            ? Mathf.Clamp(targetDistance, 0f, _rail.TotalLength)
            : 0f;
    }

    private void ClearSectionRollbackState()
    {
        _segmentChainPresenter.Reset();
        _sectionRollbackState.Complete();
    }
}
