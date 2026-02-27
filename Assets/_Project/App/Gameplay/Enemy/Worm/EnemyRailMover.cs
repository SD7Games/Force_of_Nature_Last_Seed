using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyRailMover : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Transform _visualRoot;

    [SerializeField] private float _rotationOffset = 0f;

    private Transform[] _waypoints;
    private float _speed;
    private int _currentIndex;
    private bool _isInitialized;

    public event Action Completed;

    private void Update()
    {
        if (!_isInitialized) return;

        Move();
    }

    private void Move()
    {
        Transform target = _waypoints[_currentIndex];

        Vector3 direction = (target.position - transform.position).normalized;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            _speed * Time.deltaTime
        );

        RotateVisual(direction);

        if (Vector3.SqrMagnitude(transform.position - target.position) <= 0.05f * 0.05f)
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

    private void RotateVisual(Vector3 direction)
    {
        if (_visualRoot == null) return;

        if (direction == Vector3.zero) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        angle += _rotationOffset;

        _visualRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
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