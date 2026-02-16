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
    }

    public void Init(ProjectilePool pool)
    {
        _pool = pool;
    }

    public void ApplyConfig(ProjectileConfig config)
    {
        _renderer.sprite = config.Sprite;
        _renderer.transform.localRotation = Quaternion.Euler(0f, 0f, config.RotateSprite);

        _lifeTime = Mathf.Max(0.05f, config.LifeTime);

        _movement.SetSpeed(config.Speed);
        _bounce.SetBounces(
            config.BounceCount,
            config.BounceX,
            config.BounceY
        );
    }

    public void Activate(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);

        _timer = _lifeTime;
        _active = true;

        _movement.SetDirection(transform.up);
        _bounce?.ResetBounces();

        gameObject.SetActive(true);
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