using UnityEngine;

[CreateAssetMenu(menuName = "Game/Worm/Movement Config")]
public sealed class WormMovementConfig : ScriptableObject
{
    private const float MinimumDuration = 0.01f;
    private const float MinimumBurstDuration = 0.1f;
    private const float MinimumSpacingMultiplier = 0.01f;
    private const float MaximumReviveDecelerationPathFraction = 0.8f;
    private const float MinimumReviveSquashXScale = 1f;
    private const float MaximumReviveSquashXScale = 1.8f;
    private const float MinimumReviveSquashYScale = 0.2f;
    private const float MaximumReviveSquashYScale = 1f;
    private const float MinimumReviveLandingScale = 0.6f;
    private const float MaximumReviveLandingScale = 1.2f;

    [Header("Movement")]
    [SerializeField][Min(0f)] private float _baseSpeed = 1.2f;

    [Header("Catch Up")]
    [Tooltip("RailPath control point index. Use RailPath Scene View point labels.")]
    [SerializeField][Min(0)] private int _catchUpRailPointIndex = 3;
    [SerializeField][Min(0f)] private float _catchUpSpeed = 3f;
    [SerializeField][Min(0f)] private float _catchUpStopOffset;
    [SerializeField][Min(0f)] private float _catchUpExtraDistance = 1.5f;

    [Header("Combat Speed Bursts")]
    [SerializeField] private bool _enableCombatSpeedBursts = true;
    [SerializeField][Min(0f)] private float _combatBurstSpeed = 2.5f;
    [SerializeField][Min(MinimumBurstDuration)] private float _combatBurstInterval = 10f;
    [SerializeField][Min(MinimumBurstDuration)] private float _combatBurstDuration = 2.5f;
    [Tooltip("RailPath control point index that disables combat speed bursts. Set -1 to use path progress instead.")]
    [SerializeField][Min(-1)] private int _combatBurstDisableRailPointIndex = -1;
    [SerializeField][Range(0f, 1f)] private float _combatBurstDisablePathProgress = 0.9f;
    [SerializeField][Min(MinimumDuration)] private float _combatBurstSlowdownDuration = 0.35f;

    [Header("Segments")]
    [SerializeField][Min(0f)] private float _segmentSpacing = 0.6f;
    [SerializeField][Min(MinimumSpacingMultiplier)] private float _tailVisualSpacingMultiplier = 1f;

    [Header("Head Tail Bridge")]
    [SerializeField][Min(MinimumSpacingMultiplier)] private float _headBridgeSpacingMultiplier = 1.25f;

    [Header("Optimization")]
    [SerializeField][Min(0f)] private float _activeDistancePadding = 0.5f;

    [Header("Wave")]
    [SerializeField][Min(0f)] private float _waveAmplitude = 0.05f;
    [SerializeField][Min(0f)] private float _waveFrequency = 4f;
    [SerializeField][Min(0f)] private float _waveSpeed = 1f;

    [Header("Rollback")]
    [SerializeField][Min(0f)] private float _rollbackSpeed = 9f;
    [SerializeField][Min(0f)] private float _sectionRollbackForwardSpeedMultiplier = 4f;

    [Header("Revive")]
    [Tooltip("RailPath control point index. Set -1 to use Catch Up Rail Point Index.")]
    [SerializeField][Min(-1)] private int _reviveRollbackRailPointIndex = 8;
    [SerializeField][Min(MinimumDuration)] private float _reviveSquashDuration = 0.08f;
    [SerializeField][Min(MinimumDuration)] private float _reviveThrowDuration = 0.38f;
    [SerializeField][Min(MinimumDuration)] private float _reviveLandingDuration = 0.09f;
    [Tooltip("Last part of the rollback distance where revive throw slows down to regular gameplay speed.")]
    [SerializeField][Range(0f, MaximumReviveDecelerationPathFraction)]
    private float _reviveDecelerationPathFraction = 0.2f;
    [SerializeField][Min(0f)] private float _reviveArcHeight = 1.1f;
    [SerializeField][Range(MinimumReviveSquashXScale, MaximumReviveSquashXScale)]
    private float _reviveSquashXScale = 1.22f;
    [SerializeField][Range(MinimumReviveSquashYScale, MaximumReviveSquashYScale)]
    private float _reviveSquashYScale = 0.72f;
    [SerializeField][Range(MinimumReviveLandingScale, MaximumReviveLandingScale)]
    private float _reviveLandingXScale = 1.1f;
    [SerializeField][Range(MinimumReviveLandingScale, MaximumReviveLandingScale)]
    private float _reviveLandingYScale = 0.86f;

    public float BaseSpeed => _baseSpeed;
    public int CatchUpRailPointIndex => _catchUpRailPointIndex;
    public int ReviveRollbackRailPointIndex => _reviveRollbackRailPointIndex;
    public float SegmentSpacing => _segmentSpacing;
    public float WaveSpeed => _waveSpeed;
    public float RollbackSpeed => _rollbackSpeed;
    public float SectionRollbackForwardSpeedMultiplier => _sectionRollbackForwardSpeedMultiplier;

    public WormForwardMotionSettings CreateForwardMotionSettings()
    {
        WormCombatBurstSettings burstSettings = new(
            _enableCombatSpeedBursts,
            _combatBurstSpeed,
            _combatBurstInterval,
            _combatBurstDuration,
            _combatBurstSlowdownDuration);

        return new WormForwardMotionSettings(
            _baseSpeed,
            _catchUpSpeed,
            _catchUpRailPointIndex,
            _catchUpStopOffset,
            _catchUpExtraDistance,
            _combatBurstDisableRailPointIndex,
            _combatBurstDisablePathProgress,
            burstSettings);
    }

    public WormSegmentChainLayout CreateSegmentLayout(
        float headDistance,
        float waveTime,
        float verticalOffset,
        bool isSectionRollback,
        bool isReviveRollback)
    {
        return new WormSegmentChainLayout(
            headDistance,
            _segmentSpacing,
            _tailVisualSpacingMultiplier,
            _headBridgeSpacingMultiplier,
            _activeDistancePadding,
            _waveAmplitude,
            _waveFrequency,
            waveTime,
            verticalOffset,
            isSectionRollback,
            isReviveRollback);
    }

    public WormReviveAnimationSettings CreateReviveAnimationSettings()
    {
        return new WormReviveAnimationSettings(
            _baseSpeed,
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

    private void OnValidate()
    {
        _baseSpeed = Mathf.Max(0f, _baseSpeed);
        _catchUpRailPointIndex = Mathf.Max(0, _catchUpRailPointIndex);
        _catchUpSpeed = Mathf.Max(0f, _catchUpSpeed);
        _catchUpStopOffset = Mathf.Max(0f, _catchUpStopOffset);
        _catchUpExtraDistance = Mathf.Max(0f, _catchUpExtraDistance);
        _combatBurstSpeed = Mathf.Max(0f, _combatBurstSpeed);
        _combatBurstInterval = Mathf.Max(MinimumBurstDuration, _combatBurstInterval);
        _combatBurstDuration = Mathf.Max(MinimumBurstDuration, _combatBurstDuration);
        _combatBurstDisableRailPointIndex = Mathf.Max(-1, _combatBurstDisableRailPointIndex);
        _combatBurstDisablePathProgress = Mathf.Clamp01(_combatBurstDisablePathProgress);
        _combatBurstSlowdownDuration = Mathf.Max(MinimumDuration, _combatBurstSlowdownDuration);
        _segmentSpacing = Mathf.Max(0f, _segmentSpacing);
        _tailVisualSpacingMultiplier = Mathf.Max(MinimumSpacingMultiplier, _tailVisualSpacingMultiplier);
        _headBridgeSpacingMultiplier = Mathf.Max(MinimumSpacingMultiplier, _headBridgeSpacingMultiplier);
        _activeDistancePadding = Mathf.Max(0f, _activeDistancePadding);
        _waveAmplitude = Mathf.Max(0f, _waveAmplitude);
        _waveFrequency = Mathf.Max(0f, _waveFrequency);
        _waveSpeed = Mathf.Max(0f, _waveSpeed);
        _rollbackSpeed = Mathf.Max(0f, _rollbackSpeed);
        _sectionRollbackForwardSpeedMultiplier = Mathf.Max(0f, _sectionRollbackForwardSpeedMultiplier);
        _reviveRollbackRailPointIndex = Mathf.Max(-1, _reviveRollbackRailPointIndex);
        _reviveSquashDuration = Mathf.Max(MinimumDuration, _reviveSquashDuration);
        _reviveThrowDuration = Mathf.Max(MinimumDuration, _reviveThrowDuration);
        _reviveLandingDuration = Mathf.Max(MinimumDuration, _reviveLandingDuration);
        _reviveDecelerationPathFraction = Mathf.Clamp(
            _reviveDecelerationPathFraction,
            0f,
            MaximumReviveDecelerationPathFraction);
        _reviveArcHeight = Mathf.Max(0f, _reviveArcHeight);
        _reviveSquashXScale = Mathf.Clamp(
            _reviveSquashXScale,
            MinimumReviveSquashXScale,
            MaximumReviveSquashXScale);
        _reviveSquashYScale = Mathf.Clamp(
            _reviveSquashYScale,
            MinimumReviveSquashYScale,
            MaximumReviveSquashYScale);
        _reviveLandingXScale = Mathf.Clamp(
            _reviveLandingXScale,
            MinimumReviveLandingScale,
            MaximumReviveLandingScale);
        _reviveLandingYScale = Mathf.Clamp(
            _reviveLandingYScale,
            MinimumReviveLandingScale,
            MaximumReviveLandingScale);
    }
}
