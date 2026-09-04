using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Projectile : MonoBehaviour
{
    private const int HitSectionsInitialCapacity = 8;

    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private LayerMask _hitMask;
    [SerializeField] private float _minHitDistance = 1.5f;
    [SerializeField, Min(0f)] private float _releaseBoundsPadding = 2f;
    [SerializeField, Min(0f)] private float _spawnHitDelay = 0.03f;
    [SerializeField, Min(0f)] private float _minHitTravelDistance = 0.25f;
    [SerializeField, Min(0f)] private float _damageBoundsPadding = 0f;

    private Vector3 _lastHitPosition;
    private Vector3 _spawnPosition;
    private bool _hasLastHit;
    private float _hitDelayTimer;

    private readonly List<WormSection> _hitSections = new(HitSectionsInitialCapacity);

    private float _lifeTime;
    private float _timer;

    private int _damage;
    private int _hitsLeft;
    private float _criticalChance;
    private float _criticalDamageMultiplier = 1f;

    private ProjectilePool _pool;
    private IScreenBounds _screenBounds;
    private bool _active;
    private Quaternion _visualRotationOffset = Quaternion.identity;

    private ProjectileMovement _movement;
    private ProjectileBounce _bounce;

    private void Awake()
    {
        _movement = GetComponent<ProjectileMovement>();
        _bounce = GetComponent<ProjectileBounce>();

        if (_renderer == null)
            Debug.LogError("Projectile: SpriteRenderer reference is not set.", this);
        else
            _visualRotationOffset = _renderer.transform.localRotation;
    }

    private void Update()
    {
        if (!_active)
            return;

        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            ReleaseSelf();
            return;
        }

        _bounce?.Tick();
        _movement.Tick();

        if (_hitDelayTimer > 0f)
            _hitDelayTimer -= Time.deltaTime;

        if (IsOutsideReleaseBounds())
        {
            ReleaseSelf();
            return;
        }

        UpdateVisualRotation();
    }

    public void Init(ProjectilePool pool, IScreenBounds screenBounds)
    {
        _pool = pool;
        _screenBounds = screenBounds;
        _bounce?.Init(screenBounds);
    }

    public void ApplyConfig(ProjectileConfig config, ProjectileRuntimeStats stats)
    {
        _lifeTime = Mathf.Max(0.05f, config.LifeTime);
        _damage = stats.Damage;
        _hitsLeft = Mathf.Max(1, 1 + config.Penetration + stats.ExtraPenetration);
        _criticalChance = stats.CriticalChance;
        _criticalDamageMultiplier = stats.CriticalDamageMultiplier;

        _movement.SetSpeed(config.Speed * stats.ProjectileSpeedMultiplier);

        if (_bounce != null)
        {
            _bounce.SetBounces(
                config.BounceCount,
                config.BounceX,
                config.BounceY
            );
        }
    }

    public void Activate(Vector3 position, Quaternion shotRotation)
    {
        _hasLastHit = false;

        _hitSections.Clear();

        _spawnPosition = position;
        transform.position = position;
        transform.rotation = Quaternion.identity;

        _timer = _lifeTime;
        _hitDelayTimer = _spawnHitDelay;
        _active = true;

        Vector2 direction = shotRotation * Vector2.up;
        _movement.SetDirection(direction);

        _bounce?.ResetBounces();

        UpdateVisualRotation();
        gameObject.SetActive(true);
    }

    public void ForceRelease()
    {
        ReleaseSelf();
    }

    private void UpdateVisualRotation()
    {
        if (_movement == null || _renderer == null) return;

        Vector2 dir = _movement.Direction;
        if (dir.sqrMagnitude < 0.001f) return;

        float angle = -Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        _renderer.transform.localRotation =
            Quaternion.Euler(0f, 0f, angle) * _visualRotationOffset;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_active)
            return;

        if (!CanHitNow())
            return;

        if (((1 << collision.gameObject.layer) & _hitMask) == 0)
            return;

        if (!collision.TryGetComponent<WormSegmentDamageReceiver>(out var receiver))
            return;

        var segment = receiver.GetSegment();
        if (segment == null || !segment.IsAlive)
            return;

        var section = receiver.GetDamageSection();
        if (section == null || section.IsDestroyed)
            return;

        if (_hitSections.Contains(section))
            return;

        Vector3 hitPosition = collision.ClosestPoint(transform.position);

        if (!IsInsideDamageBounds(hitPosition))
            return;

        if (_hasLastHit)
        {
            float dist = Vector3.SqrMagnitude(hitPosition - _lastHitPosition);
            if (dist < _minHitDistance * _minHitDistance)
                return;
        }

        _hitSections.Add(section);

        int damage = RollDamage(out DamageKind damageKind, out bool isCritical);

        var damageInfo = new DamageInfo(
            damage,
            hitPosition,
            damageKind,
            this,
            isCritical
        );

        receiver.TakeDamage(damageInfo);

        _lastHitPosition = hitPosition;
        _hasLastHit = true;

        _hitsLeft--;

        if (_hitsLeft > 0)
            return;

        ReleaseSelf();
    }

    private bool CanHitNow()
    {
        if (_hitDelayTimer > 0f)
            return false;

        if (_minHitTravelDistance <= 0f)
            return true;

        float sqrDistance = Vector3.SqrMagnitude(transform.position - _spawnPosition);
        return sqrDistance >= _minHitTravelDistance * _minHitTravelDistance;
    }

    private bool IsOutsideReleaseBounds()
    {
        if (_screenBounds == null)
            return false;

        Vector3 position = transform.position;
        float padding = _releaseBoundsPadding;

        return position.x < _screenBounds.Left - padding ||
               position.x > _screenBounds.Right + padding ||
               position.y < _screenBounds.Bottom - padding ||
               position.y > _screenBounds.Top + padding;
    }

    private bool IsInsideDamageBounds(Vector3 position)
    {
        if (_screenBounds == null)
            return true;

        float padding = _damageBoundsPadding;

        return position.x >= _screenBounds.Left - padding &&
               position.x <= _screenBounds.Right + padding &&
               position.y >= _screenBounds.Bottom - padding &&
               position.y <= _screenBounds.Top + padding;
    }

    private int RollDamage(out DamageKind damageKind, out bool isCritical)
    {
        isCritical = _criticalChance > 0f && Random.value < _criticalChance;
        damageKind = isCritical ? DamageKind.Critical : DamageKind.Normal;

        if (!isCritical)
            return _damage;

        return WeaponRuntimeState.ClampDamage(_damage * (double)_criticalDamageMultiplier);
    }

    private void ReleaseSelf()
    {
        if (!_active)
            return;

        _active = false;
        _pool.Release(this);
    }
}
