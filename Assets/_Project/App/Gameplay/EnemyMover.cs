using UnityEngine;

public sealed class EnemyMover : MonoBehaviour
{
    [SerializeField] private float _speed = 2f;
    [SerializeField] private Transform[] _waypoints;

    private int _currentIndex;

    private void Update()
    {
        if (_waypoints == null || _waypoints.Length == 0) return;

        Transform target = _waypoints[_currentIndex];

        transform.position = Vector3.MoveTowards(transform.position, target.position, _speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            _currentIndex++;

            if (_currentIndex >= _waypoints.Length)
            {
                enabled = false;
            }
        }
    }
}