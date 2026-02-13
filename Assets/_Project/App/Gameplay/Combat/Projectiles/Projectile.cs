using UnityEngine;

[DisallowMultipleComponent]
public sealed class Projectile : MonoBehaviour
{
    [SerializeField] private LayerMask _hitMask;
    [SerializeField] private float _lifeTime = 2f;

    private float _timer;
    private ProjectilePool _pool;
    private bool _active;

    private ProjectileMovement _movement;
    private ProjectileBounce _bounce;
    private ProjectileConfig _config;

    public void Init(ProjectilePool pool, ProjectileMovement movement, ProjectileBounce bounce)
    {
        _pool = pool;
        _movement = movement;
        _bounce = bounce;
    }

    public void ApplyConfig(ProjectileConfig config)
    {
        _config = config;
        _movement.SetSpeed(config.Speed);
        _bounce.SetBounces(config.BounceCount, config.BounceX, config.BounceY);
    }

    public void Activate(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        _timer = _lifeTime;
        _active = true;

        _movement.SetDirection(Vector2.up);
        _bounce?.ResetBounces();

        gameObject.SetActive(true);
    }

    private void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer <= 0)
            ReleaseSelf();

        _bounce?.Tick();
        _movement.Tick();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & _hitMask) == 0) return;

        ReleaseSelf();
    }

    private void ReleaseSelf()
    {
        if (!_active) return;
        _active = false;
        _pool.Release(this);
    }
}