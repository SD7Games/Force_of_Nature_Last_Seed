#if UNITY_EDITOR
using UnityEngine;

public sealed partial class WormController
{
    private const float MinimumCombatBurstSlowdownDuration = 0.01f;

    public RailPath EditorRail => _rail;
    public float EditorSpeed => _speed;
    public float EditorSegmentSpacing => _segmentSpacing;
    public float EditorRollbackSpeed => _rollbackSpeed;
    public float EditorSectionRollbackForwardSpeedMultiplier =>
        _sectionRollbackForwardSpeedMultiplier;

    public float EditorReviveRollbackProgressNormalized
    {
        get
        {
            if (_rail == null || _rail.TotalLength <= 0f)
                return 0f;

            return Mathf.Clamp01(GetReviveRollbackTargetDistance() / _rail.TotalLength);
        }
    }

    private void OnValidate()
    {
        _catchUpRailPointIndex = Mathf.Max(0, _catchUpRailPointIndex);
        _combatBurstDisableRailPointIndex = Mathf.Max(-1, _combatBurstDisableRailPointIndex);

        ClampRailPointIndices();

        _combatBurstDisablePathProgress = Mathf.Clamp01(_combatBurstDisablePathProgress);
        _combatBurstSlowdownDuration = Mathf.Max(
            MinimumCombatBurstSlowdownDuration,
            _combatBurstSlowdownDuration);
        _sectionRollbackForwardSpeedMultiplier = Mathf.Max(
            0f,
            _sectionRollbackForwardSpeedMultiplier);
        ClearTargetDistanceCaches();
    }

    private void ClampRailPointIndices()
    {
        if (_rail == null || _rail.PointCount <= 0)
            return;

        int lastPointIndex = _rail.PointCount - 1;
        _catchUpRailPointIndex = Mathf.Min(_catchUpRailPointIndex, lastPointIndex);

        if (_reviveRollbackRailPointIndex >= 0)
        {
            _reviveRollbackRailPointIndex = Mathf.Min(
                _reviveRollbackRailPointIndex,
                lastPointIndex);
        }

        if (_combatBurstDisableRailPointIndex >= 0)
        {
            _combatBurstDisableRailPointIndex = Mathf.Min(
                _combatBurstDisableRailPointIndex,
                lastPointIndex);
        }
    }
}
#endif
