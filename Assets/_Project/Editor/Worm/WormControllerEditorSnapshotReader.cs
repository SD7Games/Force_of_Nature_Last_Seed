using UnityEditor;
using UnityEngine;

internal static class WormControllerEditorSnapshotReader
{
    public static WormControllerEditorSnapshot Read(WormController controller)
    {
        SerializedObject serializedController = new(controller);
        RailPath rail = serializedController.FindProperty("_rail").objectReferenceValue as RailPath;
        WormMovementConfig movementConfig = serializedController
            .FindProperty("_movementConfig")
            .objectReferenceValue as WormMovementConfig;

        if (movementConfig == null)
            return new WormControllerEditorSnapshot(rail, 0f, 0f, 0f, 0f, 0f);

        return new WormControllerEditorSnapshot(
            rail,
            movementConfig.BaseSpeed,
            movementConfig.SegmentSpacing,
            movementConfig.RollbackSpeed,
            movementConfig.SectionRollbackForwardSpeedMultiplier,
            GetReviveRollbackProgress(
                rail,
                movementConfig.ReviveRollbackRailPointIndex,
                movementConfig.CatchUpRailPointIndex));
    }

    private static float GetReviveRollbackProgress(
        RailPath rail,
        int reviveRollbackRailPointIndex,
        int catchUpRailPointIndex)
    {
        if (rail == null)
            return 0f;

        int targetPointIndex = reviveRollbackRailPointIndex >= 0
            ? reviveRollbackRailPointIndex
            : catchUpRailPointIndex;

        if (!rail.TryGetControlPointDistance(targetPointIndex, out float targetDistance) ||
            rail.TotalLength <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(targetDistance / rail.TotalLength);
    }
}
