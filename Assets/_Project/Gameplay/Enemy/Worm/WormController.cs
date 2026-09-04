using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public sealed partial class WormController : MonoBehaviour, IWormPathProgressProvider
{
    [Header("Rail")]
    [SerializeField] private RailPath _rail;

    [Header("Movement")]
    [SerializeField] private float _speed = 1f;

    [Header("Catch Up")]
    [Tooltip("RailPath control point index. Use RailPath Scene View point labels.")]
    [SerializeField][Min(0)] private int _catchUpRailPointIndex;
    [SerializeField][Min(0f)] private float _catchUpSpeed = 6f;
    [SerializeField][Min(0f)] private float _catchUpStopOffset = 0f;
    [SerializeField][Min(0f)] private float _catchUpExtraDistance = 1.5f;

    [Header("Combat Speed Bursts")]
    [SerializeField] private bool _enableCombatSpeedBursts = true;
    [SerializeField][Min(0f)] private float _combatBurstSpeed = 2f;
    [SerializeField][Min(0.1f)] private float _combatBurstInterval = 10f;
    [SerializeField][Min(0.1f)] private float _combatBurstDuration = 2.5f;
    [Tooltip("RailPath control point index that disables combat speed bursts. Set -1 to use path progress instead.")]
    [SerializeField][Min(-1)] private int _combatBurstDisableRailPointIndex = -1;
    [SerializeField][Range(0f, 1f)] private float _combatBurstDisablePathProgress = 0.9f;
    [SerializeField][Min(0.01f)] private float _combatBurstSlowdownDuration = 0.35f;

    [Header("Segments")]
    [SerializeField] private float _segmentSpacing = 0.5f;
    [SerializeField][Min(0.01f)] private float _tailVisualSpacingMultiplier = 1f;

    [Header("Head Tail Bridge")]
    [SerializeField][Min(0.01f)] private float _headBridgeSpacingMultiplier = 1.25f;

    [Header("Optimization")]
    [SerializeField][Min(0f)] private float _activeDistancePadding = 0.5f;

    [Header("Wave")]
    [SerializeField] private float _waveAmplitude = 0.15f;

    [SerializeField] private float _waveFrequency = 6f;
    [SerializeField] private float _waveSpeed = 2f;

    [Header("Rollback")]
    [SerializeField] private float _rollbackSpeed = 8f;
    [SerializeField][Min(0f)] private float _sectionRollbackForwardSpeedMultiplier = 4f;

    [Header("Revive")]
    [Tooltip("RailPath control point index. Set -1 to use Catch Up Rail Point Index.")]
    [SerializeField][Min(-1)] private int _reviveRollbackRailPointIndex = -1;
    [SerializeField][Min(0.01f)] private float _reviveSquashDuration = 0.14f;
    [SerializeField][Min(0.01f)] private float _reviveThrowDuration = 0.75f;
    [SerializeField][Min(0.01f)] private float _reviveLandingDuration = 0.16f;
    [Tooltip("Last part of the rollback distance where revive throw slows down to regular gameplay speed.")]
    [SerializeField][Range(0f, 0.8f)] private float _reviveDecelerationPathFraction = 0.2f;
    [SerializeField][Min(0f)] private float _reviveArcHeight = 0.85f;
    [SerializeField][Range(1f, 1.8f)] private float _reviveSquashXScale = 1.22f;
    [SerializeField][Range(0.2f, 1f)] private float _reviveSquashYScale = 0.72f;
    [SerializeField][Range(0.6f, 1.2f)] private float _reviveLandingXScale = 1.1f;
    [SerializeField][Range(0.6f, 1.2f)] private float _reviveLandingYScale = 0.86f;

    private WormCombatBurstController _combatBurstController;
    private WormForwardMotionController _forwardMotionController;
    private WormRailTargetResolver _railTargetResolver;
    private WormPathProgressState _pathProgress;
    private WormSegmentChainPresenter _segmentChainPresenter;
    private WormReviveSequence _reviveSequence;
    private WormSegmentChain<WormSegment> _segmentChain;
    private WormSectionRollbackMotionController<WormSegment> _sectionRollbackMotionController;
    private WormSectionRollbackState<WormSegment> _sectionRollbackState;
    private float _waveTime;

    public event Action PathCompleted;

    public bool HasWorm => _segmentChain != null && _segmentChain.Count > 0;
    public bool IsCatchingUpToCombatStart =>
        _pathProgress != null && _pathProgress.IsCatchingUp;
    public bool IsCombatBurstActive =>
        _combatBurstController != null && _combatBurstController.IsActive;

    [Inject]
    public void Construct(
        WormCombatBurstController combatBurstController,
        WormForwardMotionController forwardMotionController,
        WormRailTargetResolver railTargetResolver,
        WormPathProgressState pathProgress,
        WormSegmentChainPresenter segmentChainPresenter,
        WormReviveSequence reviveSequence,
        WormSegmentChain<WormSegment> segmentChain,
        WormSectionRollbackMotionController<WormSegment> sectionRollbackMotionController,
        WormSectionRollbackState<WormSegment> sectionRollbackState)
    {
        _combatBurstController = combatBurstController;
        _forwardMotionController = forwardMotionController;
        _railTargetResolver = railTargetResolver;
        _pathProgress = pathProgress;
        _segmentChainPresenter = segmentChainPresenter;
        _reviveSequence = reviveSequence;
        _segmentChain = segmentChain;
        _sectionRollbackMotionController = sectionRollbackMotionController;
        _sectionRollbackState = sectionRollbackState;
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
        _combatBurstController.Reset(_speed);
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
        _combatBurstController.Reset(_speed);
        _pathProgress.Reset();
        ClearTargetDistanceCaches();
    }

    public void Tick(
        float deltaTime,
        float unscaledDeltaTime,
        float time,
        float unscaledTime)
    {
        if (_segmentChain.Count == 0 || _rail == null)
            return;

        _waveTime = (_sectionRollbackState.IsActive || _reviveSequence.IsActive
            ? unscaledTime
            : time) * _waveSpeed;

        if (_sectionRollbackState.IsActive)
            AdvanceSectionRollback(unscaledDeltaTime);
        else if (_reviveSequence.IsActive)
            AdvanceReviveAnimation(unscaledDeltaTime);
        else
            MoveForward(deltaTime);

        UpdateSegments();
    }

    private void MoveForward(float deltaTime)
    {
        WormCombatBurstSettings burstSettings = new(
            _enableCombatSpeedBursts,
            _combatBurstSpeed,
            _combatBurstInterval,
            _combatBurstDuration,
            _combatBurstSlowdownDuration);
        WormForwardMotionSettings settings = new(
            _speed,
            _catchUpSpeed,
            _catchUpRailPointIndex,
            _catchUpStopOffset,
            _catchUpExtraDistance,
            _combatBurstDisableRailPointIndex,
            _combatBurstDisablePathProgress,
            burstSettings);
        WormForwardMotionResult result = _forwardMotionController.Advance(
            _pathProgress.HeadDistance,
            deltaTime,
            _rail,
            settings);

        if (_pathProgress.Apply(result))
            PathCompleted?.Invoke();
    }

    private bool TryGetCatchUpTargetDistance(out float targetDistance)
    {
        return _railTargetResolver.TryGetCatchUpDistance(
            _rail,
            _catchUpRailPointIndex,
            out targetDistance);
    }

    private bool TryGetReviveRollbackTargetDistance(out float targetDistance)
    {
        return _railTargetResolver.TryGetReviveDistance(
            _rail,
            _reviveRollbackRailPointIndex,
            _catchUpRailPointIndex,
            out targetDistance);
    }

    private void ClearTargetDistanceCaches()
    {
        _railTargetResolver?.Clear();
    }

    private void UpdateSegments()
    {
        WormSegmentChainLayout layout = new(
            _pathProgress.HeadDistance,
            _segmentSpacing,
            _tailVisualSpacingMultiplier,
            _headBridgeSpacingMultiplier,
            _activeDistancePadding,
            _waveAmplitude,
            _waveFrequency,
            _waveTime,
            _reviveSequence.VisualYOffset,
            _sectionRollbackState.IsActive,
            _reviveSequence.IsActive);

        _segmentChainPresenter.Render(
            _segmentChain.Segments,
            _rail,
            _sectionRollbackState.AnchoredDistances,
            layout);
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
            _segmentChain.Segments,
            splitIndex,
            destroyedCount,
            _pathProgress.HeadDistance,
            _segmentSpacing);
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
            BuildReviveAnimationSettings(),
            _segmentChain.Segments,
            onComplete);
        return true;
    }

    private void AdvanceSectionRollback(float deltaTime)
    {
        if (_rail == null)
            return;

        WormSectionRollbackMotionResult result = _sectionRollbackMotionController.Advance(
            _pathProgress.HeadDistance,
            _segmentChain.Segments,
            _rail.TotalLength,
            _speed,
            _sectionRollbackForwardSpeedMultiplier,
            _rollbackSpeed,
            deltaTime);
        _pathProgress.SetHeadDistance(result.HeadDistance);

        if (!result.Completed)
            return;

        if (_pathProgress.TryComplete(_pathProgress.HeadDistance >= _rail.TotalLength))
            PathCompleted?.Invoke();

        _sectionRollbackState.Complete();
    }

    private void AdvanceReviveAnimation(float deltaTime)
    {
        WormReviveAnimationFrame frame = _reviveSequence.Advance(deltaTime);
        _pathProgress.SetHeadDistance(frame.HeadDistance);

        if (!frame.Completed)
            return;

        _reviveSequence.CompleteAfterFinalRender(UpdateSegments);
    }

    private WormReviveAnimationSettings BuildReviveAnimationSettings()
    {
        return new WormReviveAnimationSettings(
            _speed,
            _reviveSquashDuration,
            _reviveThrowDuration,
            _reviveLandingDuration,
            _reviveDecelerationPathFraction,
            _reviveArcHeight,
            _reviveSquashXScale,
            _reviveSquashYScale,
            _reviveLandingXScale,
            _reviveLandingYScale);
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
