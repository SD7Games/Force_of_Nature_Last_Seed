using UnityEditor;
using UnityEngine;

internal static class WormControllerEditorSnapshotReader
{
    public static WormControllerEditorSnapshot Read(WormController controller)
    {
        SerializedObject serializedController = new(controller);
        RailPath rail = serializedController.FindProperty("_rail").objectReferenceValue as RailPath;
        int catchUpRailPointIndex = serializedController
            .FindProperty("_catchUpRailPointIndex")
            .intValue;
        int reviveRollbackRailPointIndex = serializedController
            .FindProperty("_reviveRollbackRailPointIndex")
            .intValue;

        return new WormControllerEditorSnapshot(
            rail,
            serializedController.FindProperty("_speed").floatValue,
            serializedController.FindProperty("_segmentSpacing").floatValue,
            serializedController.FindProperty("_rollbackSpeed").floatValue,
            serializedController.FindProperty("_sectionRollbackForwardSpeedMultiplier").floatValue,
            GetReviveRollbackProgress(
                rail,
                reviveRollbackRailPointIndex,
                catchUpRailPointIndex));
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
