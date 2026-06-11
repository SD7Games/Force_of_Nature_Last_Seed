using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class WeaponAutoAttackAnimator : MonoBehaviour
{
    private const int BaseLayerIndex = 0;

    [Header("References")]
    [SerializeField] private ProjectileWeapon _weapon;
    [SerializeField] private Animator _animator;

    [Header("Settings")]
    [SerializeField] private string _attackStateName = "Weapon";
    [SerializeField, Min(0.01f)] private float _baseAnimationDuration = 1.1f;
    [SerializeField, Min(0.01f)] private float _minAnimationDuration = 0.25f;
    [SerializeField] private bool _resetPoseWhenStopped = true;

    private int _attackStateHash;
    private float _animationTimer;
    private bool _isPlaying;

    private void Reset()
    {
        TryCacheAnimator();
    }

    private void OnValidate()
    {
        TryCacheAnimator();
    }

    private void Awake()
    {
        TryCacheAnimator();
        _attackStateHash = string.IsNullOrEmpty(_attackStateName)
            ? 0
            : Animator.StringToHash(_attackStateName);
    }

    private void OnEnable()
    {
        if (_weapon != null)
            _weapon.AttackCycleStarted += HandleAttackCycleStarted;
        else
            Debug.LogWarning("WeaponAutoAttackAnimator: weapon reference is missing.", this);

        CombatState.OnShootStateChanged += HandleShootStateChanged;

        if (!CombatState.CanShoot)
            StopAnimation();
    }

    private void OnDisable()
    {
        if (_weapon != null)
            _weapon.AttackCycleStarted -= HandleAttackCycleStarted;

        CombatState.OnShootStateChanged -= HandleShootStateChanged;
        _isPlaying = false;

        if (_animator != null)
        {
            _animator.speed = 1f;
            _animator.enabled = false;
        }
    }

    private void Update()
    {
        if (!_isPlaying)
            return;

        _animationTimer -= Time.deltaTime;

        if (_animationTimer <= 0f)
            StopAnimation();
    }

    private void HandleAttackCycleStarted(float currentCooldown, float baseCooldown)
    {
        if (!CombatState.CanShoot)
            return;

        PlayAnimation(GetScaledAnimationDuration(currentCooldown, baseCooldown));
    }

    private void HandleShootStateChanged(bool canShoot)
    {
        if (!canShoot)
            StopAnimation();
    }

    private void PlayAnimation(float duration)
    {
        if (_animator == null)
            return;

        _animator.enabled = true;
        _animator.speed = Mathf.Max(0.01f, _baseAnimationDuration / duration);

        if (_attackStateHash != 0)
            _animator.Play(_attackStateHash, BaseLayerIndex, 0f);

        _animationTimer = duration;
        _isPlaying = true;
    }

    private float GetScaledAnimationDuration(float currentCooldown, float baseCooldown)
    {
        float safeBaseCooldown = Mathf.Max(0.01f, baseCooldown);
        float cooldownRatio = Mathf.Clamp01(Mathf.Max(0.01f, currentCooldown) / safeBaseCooldown);

        return Mathf.Clamp(
            _baseAnimationDuration * cooldownRatio,
            _minAnimationDuration,
            _baseAnimationDuration);
    }

    private void StopAnimation()
    {
        if (_animator == null)
            return;

        _isPlaying = false;
        _animationTimer = 0f;

        if (_resetPoseWhenStopped)
        {
            _animator.enabled = true;
            _animator.speed = 1f;
            _animator.Rebind();
            _animator.Update(0f);
        }

        _animator.speed = 1f;
        _animator.enabled = false;
    }

    private void TryCacheAnimator()
    {
        if (_animator != null)
            return;

        TryGetComponent(out _animator);
    }
}
