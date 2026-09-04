#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

public sealed partial class RailPath
{
    private const float GizmoPointRadius = 0.08f;

    public int LegacyWaypointCount => CountValidLegacyWaypoints();
    public int ChildTransformCount => transform.childCount;

    private void OnDrawGizmosSelected()
    {
        if (_localPoints == null || _localPoints.Count < 2)
        {
            DrawLegacyWaypointGizmos();
            return;
        }

        Gizmos.color = new Color(0.1f, 1f, 0.25f, 0.9f);

        Vector3[] previewPoints = GetPreviewWorldPoints();
        if (previewPoints == null || previewPoints.Length < 2)
            return;

        DrawPathGizmos(previewPoints);

        for (int i = 0; i < _localPoints.Count; i++)
            Gizmos.DrawSphere(transform.TransformPoint(_localPoints[i]), GizmoPointRadius);
    }

    public Vector3 GetEditorWorldPoint(int index)
    {
        return transform.TransformPoint(_localPoints[index]);
    }

    public Vector3[] GetEditorPreviewWorldPoints()
    {
        return GetPreviewWorldPoints();
    }

    public void SetEditorWorldPoint(int index, Vector3 worldPosition)
    {
        EnsureLocalPoints();

        if (index < 0 || index >= _localPoints.Count)
            return;

        _localPoints[index] = transform.InverseTransformPoint(worldPosition);
        Invalidate();
    }

    public void AddEditorWorldPoint(Vector3 worldPosition)
    {
        EnsureLocalPoints();
        _localPoints.Add(transform.InverseTransformPoint(worldPosition));
        Invalidate();
    }

    public void InsertEditorWorldPoint(int index, Vector3 worldPosition)
    {
        EnsureLocalPoints();
        index = Mathf.Clamp(index, 0, _localPoints.Count);
        _localPoints.Insert(index, transform.InverseTransformPoint(worldPosition));
        Invalidate();
    }

    public void RemoveEditorPointAt(int index)
    {
        EnsureLocalPoints();

        if (index < 0 || index >= _localPoints.Count)
            return;

        _localPoints.RemoveAt(index);
        Invalidate();
    }

    public void ReverseEditorPoints()
    {
        EnsureLocalPoints();
        _localPoints.Reverse();
        Invalidate();
    }

    public void ClearEditorPoints()
    {
        EnsureLocalPoints();
        _localPoints.Clear();
        Invalidate();
    }

    public void FlattenEditorLocalZ()
    {
        EnsureLocalPoints();

        for (int i = 0; i < _localPoints.Count; i++)
        {
            Vector3 point = _localPoints[i];
            point.z = 0f;
            _localPoints[i] = point;
        }

        Invalidate();
    }

    public int ImportLegacyWaypointsToLocalPoints()
    {
        int count = CountValidLegacyWaypoints();
        if (count < 2)
            return 0;

        EnsureLocalPoints();
        _localPoints.Clear();

        for (int i = 0; i < _waypoints.Length; i++)
        {
            Transform waypoint = _waypoints[i];
            if (waypoint != null)
                _localPoints.Add(transform.InverseTransformPoint(waypoint.position));
        }

        Invalidate();
        return _localPoints.Count;
    }

    public int ImportChildTransformsToLocalPoints()
    {
        int childCount = transform.childCount;
        if (childCount < 2)
            return 0;

        EnsureLocalPoints();
        _localPoints.Clear();

        for (int i = 0; i < childCount; i++)
            _localPoints.Add(transform.InverseTransformPoint(transform.GetChild(i).position));

        Invalidate();
        return _localPoints.Count;
    }

    public void ClearLegacyWaypoints()
    {
        _waypoints = null;
        Invalidate();
    }

    private static void DrawPathGizmos(IReadOnlyList<Vector3> points)
    {
        Vector3 previous = points[0];
        Gizmos.DrawSphere(previous, GizmoPointRadius);

        for (int i = 1; i < points.Count; i++)
        {
            Vector3 current = points[i];
            Gizmos.DrawLine(previous, current);
            previous = current;
        }
    }

    private void DrawLegacyWaypointGizmos()
    {
        if (_waypoints == null || _waypoints.Length < 2)
            return;

        Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.9f);

        Transform previous = null;
        for (int i = 0; i < _waypoints.Length; i++)
        {
            Transform current = _waypoints[i];
            if (current == null)
                continue;

            Gizmos.DrawSphere(current.position, GizmoPointRadius);

            if (previous != null)
                Gizmos.DrawLine(previous.position, current.position);

            previous = current;
        }
    }

    private Vector3[] GetPreviewWorldPoints()
    {
        if (!TryBuildWorldPoints())
            return null;

        return BuildPathPoints();
    }

    private void EnsureLocalPoints()
    {
        if (_localPoints == null)
            _localPoints = new List<Vector3>();
    }
}
#endif
