using UnityEngine;

public sealed class PlayerShooter : MonoBehaviour
{
    [SerializeField] private Shoot _shootPrefab;
    [SerializeField] private float _fireRate = 0.4f;

    private float _timer;

    private void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            Instantiate(_shootPrefab, transform.position, Quaternion.identity);
            _timer = _fireRate;
        }
    }
}