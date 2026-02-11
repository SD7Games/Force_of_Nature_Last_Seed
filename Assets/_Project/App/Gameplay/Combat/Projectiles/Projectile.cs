using UnityEngine;

[DisallowMultipleComponent]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float _speed = 8f;
    [SerializeField] private float _lifeTime = 2f;

    private float _timer;
    private ProjectilePool _pool;
    private bool _active;

    public void Init(ProjectilePool pool)
    {
        _pool = pool;
    }

    public void Activate(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        _timer = _lifeTime;
        _active = true;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        transform.position += Vector3.up * (_speed * Time.deltaTime);

        _timer -= Time.deltaTime;

        if (_timer <= 0)
            ReleaseSelf();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ReleaseSelf();
    }

    private void ReleaseSelf()
    {
        if (!_active) return;
        _active = false;
        _pool.Release(this);
    }
}