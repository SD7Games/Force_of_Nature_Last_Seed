using UnityEngine;

[DisallowMultipleComponent]
public sealed class Projectile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private LayerMask _hitMask;
    private float _lifeTime;

    private float _timer;
    private ProjectilePool _pool;
    private bool _active;
    private float _baseVisualRotation;

    private ProjectileMovement _movement;
    private ProjectileBounce _bounce;

    private void Awake()
    {
        _movement = GetComponent<ProjectileMovement>();
        _bounce = GetComponent<ProjectileBounce>();
        if (_renderer == null)
            Debug.LogError("Projectile: SpriteRenderer reference is not set.", this);
    }

    private void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            ReleaseSelf();
            return;
        }

        _bounce?.Tick();
        _movement.Tick();

        UpdateVisualRotation();
    }

    public void Init(ProjectilePool pool)
    {
        _pool = pool;
    }

    public void ApplyConfig(ProjectileConfig config)
    {
        _renderer.sprite = config.Sprite;
        _baseVisualRotation = config.RotateSprite;

        _lifeTime = Mathf.Max(0.05f, config.LifeTime);

        _movement.SetSpeed(config.Speed);
        _bounce.SetBounces(
            config.BounceCount,
            config.BounceX,
            config.BounceY
        );
    }

    public void Activate(Vector3 position, Quaternion shotRotation)
    {
        transform.position = position;
        transform.rotation = Quaternion.identity;

        _timer = _lifeTime;
        _active = true;

        Vector2 direction = shotRotation * Vector2.up;
        _movement.SetDirection(direction);

        _bounce?.ResetBounces();
        UpdateVisualRotation();

        gameObject.SetActive(true);
    }

    private void UpdateVisualRotation()
    {
        if (_movement == null) return;

        Vector2 dir = _movement.Direction;
        if (dir.sqrMagnitude < 0.001f) return;

        float angle = -Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;

        _renderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle + _baseVisualRotation);
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