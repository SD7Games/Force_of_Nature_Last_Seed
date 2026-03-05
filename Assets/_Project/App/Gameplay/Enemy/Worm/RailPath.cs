using UnityEngine;

public sealed class RailPath : MonoBehaviour
{
    [SerializeField] private Transform[] _waypoints;

    private float[] _distances;
    private float _totalLength;

    public float TotalLength => _totalLength;

    private void Awake()
    {
        if (_waypoints == null || _waypoints.Length < 2)
        {
            Debug.LogError("RailPath requires at least 2 waypoints", this);
            return;
        }

        CalculateDistances();
    }

    private void CalculateDistances()
    {
        _distances = new float[_waypoints.Length];
        _distances[0] = 0;

        for (int i = 1; i < _waypoints.Length; i++)
        {
            float d = Vector3.Distance(
                _waypoints[i - 1].position,
                _waypoints[i].position
            );

            _distances[i] = _distances[i - 1] + d;
        }

        _totalLength = _distances[^1];
    }

    public Vector3 GetPoint(float distance)
    {
        if (_waypoints == null || _waypoints.Length < 2)
            return Vector3.zero;

        distance = Mathf.Clamp(distance, 0, _totalLength);

        for (int i = 1; i < _distances.Length; i++)
        {
            if (distance <= _distances[i])
            {
                float segLength = _distances[i] - _distances[i - 1];

                float t = (distance - _distances[i - 1]) / segLength;

                return Vector3.Lerp(
                    _waypoints[i - 1].position,
                    _waypoints[i].position,
                    t
                );
            }
        }

        return _waypoints[^1].position;
    }
}