using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormInnerSymbolAnimator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform _target;
    [SerializeField] private bool _playOnEnable = true;

    [Header("Breathing")]
    [SerializeField] private bool _animateScale = true;
    [SerializeField, Range(0f, 1f)] private float _breathScale = 0.3f;
    [SerializeField, Min(0.01f)] private float _breathDuration = 1.45f;

    [Header("Rotation")]
    [SerializeField] private bool _animateRotation = true;
    [SerializeField, Range(0f, 180f)] private float _rotationAngle = 32f;
    [SerializeField, Min(0.1f)] private float _rotationDuration = 2.8f;
    [SerializeField, Range(0f, 0.75f)] private float _randomDurationOffset = 0.35f;
    [SerializeField] private bool _randomizeDirection = true;

    private Vector3 _basePosition;
    private Vector3 _baseScale;
    private Vector3 _baseEuler;
    private bool _hasBaseState;
    private bool _isPlaying;
    private bool _isBreathPlaying;
    private bool _isRotationPlaying;
    private float _breathTime;
    private float _breathPeriod;
    private float _breathAngularSpeed;
    private float _activeBreathScale;
    private float _rotationTime;
    private float _rotationPeriod;
    private float _rotationAngularSpeed;
    private float _activeRotationAngle;
    private float _rotationDirection = 1f;

    private Transform Target => _target != null ? _target : transform;

    private void Reset()
    {
        _target = transform;
    }

    private void Awake()
    {
        CaptureBaseStateIfNeeded();
    }

    private void OnEnable()
    {
        CaptureBaseStateIfNeeded();

        if (_playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        Stop(true);
    }

    private void OnDestroy()
    {
        _isPlaying = false;
    }

    private void Update()
    {
        if (!_isPlaying)
            return;

        Transform target = Target;
        if (target == null)
        {
            _isPlaying = false;
            return;
        }

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        if (_isBreathPlaying)
            UpdateBreath(target, deltaTime);

        if (_isRotationPlaying)
            UpdateRotation(target, deltaTime);
    }

    public void Play()
    {
        CaptureBaseStateIfNeeded();
        Stop(true);

        Transform target = Target;
        if (target == null)
            return;

        _isBreathPlaying = _animateScale && _breathScale > 0f;
        _isRotationPlaying = _animateRotation && _rotationAngle > 0f;

        if (_isBreathPlaying)
            StartBreathLoop();

        if (_isRotationPlaying)
            StartRotationLoop();

        _isPlaying = _isBreathPlaying || _isRotationPlaying;
    }

    public void Stop(bool restoreBaseState = true)
    {
        _isPlaying = false;
        _isBreathPlaying = false;
        _isRotationPlaying = false;

        if (restoreBaseState)
            RestoreBaseState();
    }

    public void CaptureBaseState()
    {
        Transform target = Target;
        if (target == null)
            return;

        _basePosition = target.localPosition;
        _baseScale = target.localScale;
        _baseEuler = target.localEulerAngles;
        _hasBaseState = true;
    }

    private void StartBreathLoop()
    {
        float safeDuration = Mathf.Max(0.01f, _breathDuration);
        _activeBreathScale = _breathScale;
        _breathTime = 0f;
        _breathPeriod = safeDuration * 2f;
        _breathAngularSpeed = Mathf.PI / safeDuration;
    }

    private void StartRotationLoop()
    {
        _rotationDirection = _randomizeDirection && Random.value < 0.5f ? -1f : 1f;
        float durationOffset = _randomDurationOffset > 0f
            ? Random.Range(-_randomDurationOffset, _randomDurationOffset)
            : 0f;
        float rotationDuration = Mathf.Max(0.1f, _rotationDuration + durationOffset);

        _activeRotationAngle = _rotationAngle;
        _rotationTime = 0f;
        _rotationPeriod = rotationDuration * 4f;
        _rotationAngularSpeed = Mathf.PI * 0.5f / rotationDuration;
    }

    private void UpdateBreath(Transform target, float deltaTime)
    {
        _breathTime += deltaTime;
        if (_breathTime >= _breathPeriod)
            _breathTime %= _breathPeriod;

        float scaleOffset = _activeBreathScale * 0.5f * (1f - Mathf.Cos(_breathTime * _breathAngularSpeed));
        target.localScale = new Vector3(
            _baseScale.x * (1f + scaleOffset),
            _baseScale.y * (1f + scaleOffset),
            _baseScale.z);
    }

    private void UpdateRotation(Transform target, float deltaTime)
    {
        _rotationTime += deltaTime;
        if (_rotationTime >= _rotationPeriod)
            _rotationTime %= _rotationPeriod;

        Vector3 euler = _baseEuler;
        euler.z += Mathf.Sin(_rotationTime * _rotationAngularSpeed) * _activeRotationAngle * _rotationDirection;
        target.localEulerAngles = euler;
    }

    private void CaptureBaseStateIfNeeded()
    {
        if (!_hasBaseState)
            CaptureBaseState();
    }

    private void RestoreBaseState()
    {
        if (!_hasBaseState)
            return;

        Transform target = Target;
        if (target == null)
            return;

        target.localPosition = _basePosition;
        target.localScale = _baseScale;
        target.localEulerAngles = _baseEuler;
    }
}
