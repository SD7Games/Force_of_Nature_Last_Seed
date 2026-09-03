using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

/// <summary>
/// Controls movement and positioning of the worm segments along a rail path.
///
/// The worm is represented as a chain of segments that follow the head
/// using a fixed spacing distance.
///
/// Movement is rail-based which allows efficient positioning without
/// physics simulation.
///
/// The controller also handles the rollback mechanic which occurs when
/// a group of segments is destroyed. In this case the worm head moves
/// backwards until the remaining segments reconnect.
/// </summary>
public sealed class WormController : MonoBehaviour, IWormPathProgressProvider
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

    private readonly List<WormSegment> _segments = new();
    private readonly Dictionary<WormSegment, float> _rollbackAnchoredDistances = new();

    private float _headDistance;
    private Coroutine _rollbackRoutine;
    private Coroutine _reviveThrowbackRoutine;
    private bool _isSectionRollback;
    private bool _isReviveRollback;
    private float _sectionRollbackTargetDistance;
    private bool _hasReachedPathEnd;
    private float _reviveVisualYOffset;
    private WormCombatBurstController _combatBurstController;
    private WormRailTargetResolver _railTargetResolver;
    private WormSegmentChainPresenter _segmentChainPresenter;
    private WormReviveMotionCalculator _reviveMotionCalculator;
    private WormReviveVisualScaler _reviveVisualScaler;

    public event Action PathCompleted;
    public event Action<bool> CombatBurstStateChanged;

    public bool HasWorm => _segments.Count > 0;
    public bool IsCatchingUpToCombatStart { get; private set; }
    public bool IsCombatBurstActive =>
        _combatBurstController != null && _combatBurstController.IsActive;

    [Inject]
    public void Construct(
        WormCombatBurstController combatBurstController,
        WormRailTargetResolver railTargetResolver,
        WormSegmentChainPresenter segmentChainPresenter,
        WormReviveMotionCalculator reviveMotionCalculator,
        WormReviveVisualScaler reviveVisualScaler)
    {
        _combatBurstController = combatBurstController;
        _railTargetResolver = railTargetResolver;
        _segmentChainPresenter = segmentChainPresenter;
        _reviveMotionCalculator = reviveMotionCalculator;
        _reviveVisualScaler = reviveVisualScaler;
        _combatBurstController.ActiveStateChanged += HandleCombatBurstStateChanged;
    }

#if UNITY_EDITOR
    public RailPath EditorRail => _rail;
    public float EditorSpeed => _speed;
    public float EditorSegmentSpacing => _segmentSpacing;
    public float EditorRollbackSpeed => _rollbackSpeed;
    public float EditorSectionRollbackForwardSpeedMultiplier => _sectionRollbackForwardSpeedMultiplier;
    public float EditorReviveRollbackProgressNormalized
    {
        get
        {
            if (_rail == null || _rail.TotalLength <= 0f)
                return 0f;

            return Mathf.Clamp01(GetReviveRollbackTargetDistance() / _rail.TotalLength);
        }
    }
#endif

    public float HeadPathProgressNormalized
    {
        get
        {
            if (_rail == null || _rail.TotalLength <= 0f)
                return 0f;

            return Mathf.Clamp01(_headDistance / _rail.TotalLength);
        }
    }

    public float HeadControlPointProgressNormalized
    {
        get
        {
            if (_rail == null || _rail.PointCount <= 1)
                return HeadPathProgressNormalized;

            return _rail.GetControlPointProgressNormalized(_headDistance);
        }
    }

    private void OnValidate()
    {
        if (_catchUpRailPointIndex < 0)
            _catchUpRailPointIndex = 0;

        if (_combatBurstDisableRailPointIndex < -1)
            _combatBurstDisableRailPointIndex = -1;

        if (_rail != null && _rail.PointCount > 0)
        {
            _catchUpRailPointIndex = Mathf.Min(_catchUpRailPointIndex, _rail.PointCount - 1);
            if (_reviveRollbackRailPointIndex >= 0)
            {
                _reviveRollbackRailPointIndex = Mathf.Min(
                    _reviveRollbackRailPointIndex,
                    _rail.PointCount - 1);
            }

            if (_combatBurstDisableRailPointIndex >= 0)
            {
                _combatBurstDisableRailPointIndex = Mathf.Min(
                    _combatBurstDisableRailPointIndex,
                    _rail.PointCount - 1);
            }
        }

        _combatBurstDisablePathProgress = Mathf.Clamp01(_combatBurstDisablePathProgress);
        _combatBurstSlowdownDuration = Mathf.Max(0.01f, _combatBurstSlowdownDuration);
        _sectionRollbackForwardSpeedMultiplier = Mathf.Max(0f, _sectionRollbackForwardSpeedMultiplier);
        ClearTargetDistanceCaches();
    }

    private void OnDestroy()
    {
        if (_combatBurstController != null)
            _combatBurstController.ActiveStateChanged -= HandleCombatBurstStateChanged;

        CleanupReviveThrowbackVisuals();
    }

    /// <summary>
    /// Initializes worm movement with the generated segment list.
    /// Called by WormSpawner after all segments are created.
    /// </summary>
    public void Init(List<WormSegment> segments)
    {
        if (_reviveThrowbackRoutine != null)
        {
            StopCoroutine(_reviveThrowbackRoutine);
            _reviveThrowbackRoutine = null;
        }

        CleanupReviveThrowbackVisuals();
        _segments.Clear();
        _segments.AddRange(segments);

        _headDistance = 0f;
        _segmentChainPresenter.Reset();

        _isSectionRollback = false;
        _isReviveRollback = false;
        _reviveVisualYOffset = 0f;
        _rollbackAnchoredDistances.Clear();
        _sectionRollbackTargetDistance = 0f;
        _hasReachedPathEnd = false;
        _combatBurstController.Reset(_speed);
        ClearTargetDistanceCaches();
        IsCatchingUpToCombatStart = TryGetCatchUpTargetDistance(out _);

        UpdateSegments();
    }

    public void ClearWorm()
    {
        if (_rollbackRoutine != null)
        {
            StopCoroutine(_rollbackRoutine);
            _rollbackRoutine = null;
        }

        if (_reviveThrowbackRoutine != null)
        {
            StopCoroutine(_reviveThrowbackRoutine);
            _reviveThrowbackRoutine = null;
        }

        CleanupReviveThrowbackVisuals();
        _segments.Clear();
        _rollbackAnchoredDistances.Clear();
        _headDistance = 0f;
        _segmentChainPresenter.Reset();
        _isSectionRollback = false;
        _isReviveRollback = false;
        _reviveVisualYOffset = 0f;
        _sectionRollbackTargetDistance = 0f;
        _hasReachedPathEnd = false;
        _combatBurstController.Reset(_speed);
        IsCatchingUpToCombatStart = false;
        ClearTargetDistanceCaches();
    }

    private void Update()
    {
        if (_segments.Count == 0 || _rail == null)
            return;

        if (!_isSectionRollback && !_isReviveRollback)
            MoveForward(Time.deltaTime);

        UpdateSegments();
    }

    private void MoveForward(float deltaTime)
    {
        if (deltaTime <= 0f || _rail.TotalLength <= 0f)
            return;

        float previousDistance = _headDistance;
        float targetDistance = _rail.TotalLength;

        _headDistance = Mathf.Min(
            targetDistance,
            _headDistance + GetForwardSpeed(deltaTime) * deltaTime);

        CompletePathIfReached(previousDistance, targetDistance);
    }

    private void CompletePathIfReached(float previousDistance, float targetDistance)
    {
        if (_hasReachedPathEnd)
            return;

        if (previousDistance >= targetDistance || _headDistance < targetDistance)
            return;

        _hasReachedPathEnd = true;
        PathCompleted?.Invoke();
    }

    private float GetForwardSpeed(float deltaTime)
    {
        IsCatchingUpToCombatStart = ShouldCatchUp();
        WormCombatBurstSettings settings = new(
            _enableCombatSpeedBursts,
            _combatBurstSpeed,
            _combatBurstInterval,
            _combatBurstDuration,
            _combatBurstSlowdownDuration);

        return _combatBurstController.ResolveForwardSpeed(
            deltaTime,
            _speed,
            _catchUpSpeed,
            IsCatchingUpToCombatStart,
            CanUseCombatBurst(deltaTime),
            settings);
    }

    private bool ShouldCatchUp()
    {
        if (_rail == null)
            return false;

        if (!TryGetCatchUpTargetDistance(out float targetDistance))
            return false;

        targetDistance = Mathf.Max(
            0f,
            targetDistance - _catchUpStopOffset + _catchUpExtraDistance);

        return _headDistance < targetDistance;
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

    private bool CanUseCombatBurst(float deltaTime)
    {
        if (!TryGetCombatBurstDisableDistance(out float disableDistance))
            return true;

        float projectedDistance = _headDistance +
            (Mathf.Max(_speed, _combatBurstSpeed) * Mathf.Max(0f, deltaTime));

        return projectedDistance < disableDistance;
    }

    private bool TryGetCombatBurstDisableDistance(out float distance)
    {
        return _railTargetResolver.TryGetBurstDisableDistance(
            _rail,
            _combatBurstDisableRailPointIndex,
            _combatBurstDisablePathProgress,
            out distance);
    }

    private void HandleCombatBurstStateChanged(bool isActive)
    {
        CombatBurstStateChanged?.Invoke(isActive);
    }

    /// <summary>
    /// Updates position and rotation of all worm segments.
    /// Each segment samples a position along the rail using
    /// its offset distance relative to the head.
    /// </summary>
    private void UpdateSegments()
    {
        WormSegmentChainLayout layout = new(
            _headDistance,
            _segmentSpacing,
            _tailVisualSpacingMultiplier,
            _headBridgeSpacingMultiplier,
            _activeDistancePadding,
            _waveAmplitude,
            _waveFrequency,
            GetWaveTime(),
            _reviveVisualYOffset,
            _isSectionRollback,
            _isReviveRollback);

        _segmentChainPresenter.Render(
            _segments,
            _rail,
            _rollbackAnchoredDistances,
            layout);
    }

    private float GetWaveTime()
    {
        return (_isSectionRollback || _isReviveRollback
            ? Time.unscaledTime
            : Time.time) * _waveSpeed;
    }

    /// <summary>
    /// Removes destroyed segments from the internal segment list
    /// and returns how many segments were removed.
    /// </summary>
    public int RemoveDestroyedSectionSegments(List<WormSegment> destroyed, out int firstRemovedIndex)
    {
        firstRemovedIndex = -1;

        if (destroyed == null || destroyed.Count == 0)
            return 0;

        HashSet<WormSegment> destroyedSet = new(destroyed);

        for (int i = 0; i < _segments.Count; i++)
        {
            if (destroyedSet.Contains(_segments[i]))
            {
                firstRemovedIndex = i;
                break;
            }
        }

        int removed = _segments.RemoveAll(seg => seg != null && destroyedSet.Contains(seg));

        for (int i = 0; i < destroyed.Count; i++)
        {
            WormSegment segment = destroyed[i];

            if (segment != null)
                _rollbackAnchoredDistances.Remove(segment);
        }

        return removed;
    }

    /// <summary>
    /// Starts rollback movement after a section of segments
    /// has been destroyed.
    /// </summary>
    public void RollbackDestroyedGap(int destroyedCount, int splitIndex)
    {
        if (destroyedCount <= 0)
            return;

        if (splitIndex < 0)
            return;

        if (_isReviveRollback)
            return;

        float rollbackDistance = destroyedCount * Mathf.Max(0.01f, _segmentSpacing);
        bool rollbackInProgress = _isSectionRollback || _rollbackRoutine != null;

        AnchorRollbackTail(splitIndex, destroyedCount);

        _sectionRollbackTargetDistance = Mathf.Max(
            0f,
            (rollbackInProgress
                ? _sectionRollbackTargetDistance
                : _headDistance) - rollbackDistance);

        if (rollbackInProgress)
            return;

        _rollbackRoutine = StartCoroutine(SectionRollbackRoutine());
    }

    public bool RollbackToReviveStart(Action onComplete)
    {
        if (_segments.Count == 0 || _rail == null)
            return false;

        float target = GetReviveRollbackTargetDistance();

        if (_reviveThrowbackRoutine != null)
        {
            StopCoroutine(_reviveThrowbackRoutine);
            _reviveThrowbackRoutine = null;
            CleanupReviveThrowbackVisuals();
        }

        if (_rollbackRoutine != null)
        {
            StopCoroutine(_rollbackRoutine);
            _rollbackRoutine = null;
        }

        ClearSectionRollbackState();
        _isReviveRollback = false;
        _reviveVisualYOffset = 0f;
        _hasReachedPathEnd = false;

        if (_headDistance <= target)
        {
            _headDistance = target;
            UpdateSegments();
            onComplete?.Invoke();
            return true;
        }

        _reviveThrowbackRoutine = StartCoroutine(ReviveThrowbackRoutine(target, onComplete));
        return true;
    }

    /// <summary>
    /// Performs smooth rollback of the worm head until
    /// the destroyed gap is closed.
    /// Additional destroyed sections can extend the target distance
    /// without restarting the animation.
    /// Uses unscaled time so the chain can visually reconnect while
    /// the reward popup keeps gameplay paused through Time.timeScale.
    /// </summary>
    private IEnumerator SectionRollbackRoutine()
    {
        _isSectionRollback = true;

        while (_headDistance > _sectionRollbackTargetDistance)
        {
            float deltaTime = Time.unscaledDeltaTime;
            float target = _sectionRollbackTargetDistance;

            AdvanceSectionRollbackTail(deltaTime);
            target = _sectionRollbackTargetDistance;

            if (_headDistance <= target)
                break;

            _headDistance = Mathf.MoveTowards(
                _headDistance,
                target,
                _rollbackSpeed * deltaTime
            );

            UpdateSegments();
            yield return null;
        }

        _headDistance = Mathf.Min(_headDistance, _sectionRollbackTargetDistance);
        UpdateSegments();
        CompletePathIfReached(_headDistance - 0.001f, _rail.TotalLength);

        _isSectionRollback = false;
        _rollbackAnchoredDistances.Clear();
        _sectionRollbackTargetDistance = 0f;
        _rollbackRoutine = null;
    }

    private void AdvanceSectionRollbackTail(float deltaTime)
    {
        if (deltaTime <= 0f || _rail == null)
            return;

        float forwardDistance = Mathf.Max(0f, _speed) *
            Mathf.Max(0f, _sectionRollbackForwardSpeedMultiplier) *
            deltaTime;

        if (forwardDistance <= 0f)
            return;

        float maxDistance = _rail.TotalLength;
        _sectionRollbackTargetDistance = Mathf.Min(
            maxDistance,
            _sectionRollbackTargetDistance + forwardDistance);

        for (int i = 0; i < _segments.Count; i++)
        {
            WormSegment segment = _segments[i];

            if (segment == null)
                continue;

            if (_rollbackAnchoredDistances.TryGetValue(segment, out float distance))
            {
                _rollbackAnchoredDistances[segment] = Mathf.Min(
                    maxDistance,
                    distance + forwardDistance);
            }
        }
    }

    private IEnumerator ReviveThrowbackRoutine(float target, Action onComplete)
    {
        _isReviveRollback = true;
        _segmentChainPresenter.Reset();
        _reviveVisualYOffset = 0f;

        float start = _headDistance;

        _reviveVisualScaler.Capture(_segments);

        yield return PlayReviveSquashPhase();
        yield return PlayReviveThrowPhase(start, target);
        yield return PlayReviveLandingPhase(target);

        _headDistance = target;
        _reviveVisualYOffset = 0f;
        UpdateSegments();

        CleanupReviveThrowbackVisuals();
        _isReviveRollback = false;
        _reviveThrowbackRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator PlayReviveSquashPhase()
    {
        float duration = Mathf.Max(0.01f, _reviveSquashDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = _reviveMotionCalculator.EaseOutCubic(t);

            _reviveVisualScaler.Apply(
                _segments,
                Mathf.LerpUnclamped(1f, _reviveSquashXScale, eased),
                Mathf.LerpUnclamped(1f, _reviveSquashYScale, eased));

            UpdateSegments();
            yield return null;
        }

        _reviveVisualScaler.Apply(
            _segments,
            _reviveSquashXScale,
            _reviveSquashYScale);
    }

    private IEnumerator PlayReviveThrowPhase(float start, float target)
    {
        float rollbackDistance = Mathf.Max(0f, start - target);
        float cruiseSpeed = _reviveMotionCalculator.CalculateCruiseSpeed(
            rollbackDistance,
            _reviveThrowDuration,
            _reviveDecelerationPathFraction,
            _speed);

        if (rollbackDistance <= 0.001f)
        {
            _headDistance = target;
            _reviveVisualYOffset = 0f;
            UpdateSegments();
            yield break;
        }

        while (_headDistance > target)
        {
            float remainingDistance = Mathf.Max(0f, _headDistance - target);
            float speed = _reviveMotionCalculator.CalculateThrowSpeed(
                remainingDistance,
                rollbackDistance,
                cruiseSpeed,
                _reviveDecelerationPathFraction,
                _speed);

            _headDistance = Mathf.Max(
                target,
                _headDistance - (speed * Time.unscaledDeltaTime));

            remainingDistance = Mathf.Max(0f, _headDistance - target);
            float distanceProgress = 1f - Mathf.Clamp01(remainingDistance / rollbackDistance);

            _reviveVisualYOffset = Mathf.Sin(distanceProgress * Mathf.PI) * _reviveArcHeight;

            WormScale2 travelScale = _reviveMotionCalculator.CalculateTravelScale(
                distanceProgress,
                _reviveSquashXScale,
                _reviveSquashYScale);
            _reviveVisualScaler.Apply(_segments, travelScale.X, travelScale.Y);
            UpdateSegments();

            yield return null;
        }

        _headDistance = target;
        _reviveVisualYOffset = 0f;
        _reviveVisualScaler.Apply(_segments, 1f, 1f);
        UpdateSegments();
    }

    private IEnumerator PlayReviveLandingPhase(float target)
    {
        float duration = Mathf.Max(0.01f, _reviveLandingDuration);
        float elapsed = 0f;

        _headDistance = target;
        _reviveVisualYOffset = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = _reviveMotionCalculator.EaseOutBack(t);

            _reviveVisualYOffset = Mathf.Sin(t * Mathf.PI) * (_reviveArcHeight * 0.12f);

            _reviveVisualScaler.Apply(
                _segments,
                Mathf.LerpUnclamped(_reviveLandingXScale, 1f, eased),
                Mathf.LerpUnclamped(_reviveLandingYScale, 1f, eased));

            UpdateSegments();

            yield return null;
        }

        _reviveVisualScaler.Apply(_segments, 1f, 1f);
    }

    private void CleanupReviveThrowbackVisuals()
    {
        _reviveVisualYOffset = 0f;
        _reviveVisualScaler?.RestoreAndClear(_segments);
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
        _isSectionRollback = false;
        _segmentChainPresenter.Reset();
        _rollbackAnchoredDistances.Clear();
        _sectionRollbackTargetDistance = 0f;
    }

    private void AnchorRollbackTail(int splitIndex, int destroyedCount)
    {
        float spacing = Mathf.Max(0.01f, _segmentSpacing);
        int startIndex = Mathf.Clamp(splitIndex, 0, _segments.Count);

        for (int i = startIndex; i < _segments.Count; i++)
        {
            WormSegment segment = _segments[i];

            if (segment == null || _rollbackAnchoredDistances.ContainsKey(segment))
                continue;

            float anchoredDistance = _headDistance - ((i + destroyedCount) * spacing);
            _rollbackAnchoredDistances.Add(segment, anchoredDistance);
        }

        _segmentChainPresenter.Reset();
    }
}
