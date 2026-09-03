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
    [SerializeField, Range(0f, 1f)] private float _projectileReleaseNormalizedTime = 0.58f;
    [SerializeField] private bool _resetPoseWhenStopped = true;

    private int _attackStateHash;
    private float _animationTimer;
    private float _currentAnimationDuration;
    private bool _isPlaying;
    private bool _projectileReleased;

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
        _projectileReleased = false;
        _currentAnimationDuration = 0f;

        if (_animator != null)
        {
            _animator.speed = 1f;
            _animator.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (!_isPlaying)
            return;

        TryReleaseProjectileFromAnimationProgress();

        _animationTimer -= Time.deltaTime;

        if (_animationTimer <= 0f)
        {
            if (!_projectileReleased && CombatState.CanShoot)
                ReleaseProjectileAtNormalizedTime(1f);

            StopAnimation();
        }
    }

    private void HandleAttackCycleStarted(float currentCooldown, float baseCooldown)
    {
        if (!CombatState.CanShoot)
            return;

        PlayAnimation(GetScaledAnimationDuration(currentCooldown, baseCooldown));
    }

    public void ReleasePreparedAttack()
    {
        // Kept intentionally so stale Animation Events do not release the shot early.
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
        _currentAnimationDuration = duration;
        _projectileReleased = false;
        _isPlaying = true;
    }

    private void TryReleaseProjectileFromAnimationProgress()
    {
        if (_projectileReleased || !CombatState.CanShoot)
            return;

        float normalizedTime = GetCurrentAnimationNormalizedTime();

        if (normalizedTime < _projectileReleaseNormalizedTime)
            return;

        ReleaseProjectileAtNormalizedTime(normalizedTime);
    }

    private void ReleaseProjectileAtNormalizedTime(float normalizedTime)
    {
        _projectileReleased = true;

        if (_weapon == null)
            return;

        float preparedAttackElapsed = Mathf.Clamp01(normalizedTime) * _currentAnimationDuration;
        _weapon.ReleasePreparedAttack(preparedAttackElapsed);
    }

    private float GetCurrentAnimationNormalizedTime()
    {
        if (_animator == null || !_animator.enabled || _currentAnimationDuration <= 0f)
            return 1f;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(BaseLayerIndex);

        if (_attackStateHash != 0 && stateInfo.shortNameHash == _attackStateHash)
            return Mathf.Clamp01(stateInfo.normalizedTime);

        return Mathf.Clamp01(1f - _animationTimer / _currentAnimationDuration);
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
        _projectileReleased = false;
        _animationTimer = 0f;
        _currentAnimationDuration = 0f;

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
