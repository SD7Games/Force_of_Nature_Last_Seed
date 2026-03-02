using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyRailMover : MonoBehaviour
{
    private Transform[] _waypoints;
    private float _speed;
    private int _currentIndex;
    private bool _isInitialized;

    public event Action Completed;

    private void Update()
    {
        if (!_isInitialized)
            return;

        Transform target = _waypoints[_currentIndex];

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            _speed * Time.deltaTime
        );

        if (Vector3.SqrMagnitude(transform.position - target.position) <= 0.0025f)
        {
            _currentIndex++;

            if (_currentIndex >= _waypoints.Length)
            {
                _isInitialized = false;
                enabled = false;
                Completed?.Invoke();
            }
        }
    }

    public void Initialize(Transform[] waypoints, float speed)
    {
        _waypoints = waypoints;
        _speed = speed;
        _currentIndex = 0;

        _isInitialized = _waypoints != null && _waypoints.Length > 0;
        enabled = _isInitialized;

        if (_isInitialized)
            transform.position = _waypoints[0].position;
    }

    public void ResetState()
    {
        _currentIndex = 0;
        _isInitialized = false;
        enabled = false;
    }
}