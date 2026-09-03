using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public sealed class PlayerVisualController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Min(0.01f)] private float _flipThreshold = 0.05f;

    private SpriteRenderer _spriteRenderer;
    private Transform _visualRoot;
    private Vector3 _baseLocalScale;
    private MirroredChild[] _mirroredChildren = new MirroredChild[0];
    private bool _isFacingLeft;
    private PlayerMovementController _movementController;

    [Zenject.Inject]
    public void Construct(PlayerMovementController movementController)
    {
        _movementController = movementController;
    }

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _visualRoot = transform;
        _baseLocalScale = _visualRoot.localScale;
        CacheMirroredChildren();

        ApplyFacing();
    }

    private void LateUpdate()
    {
        if (_movementController == null)
            return;

        float input = _movementController.MovementInput;

        if (Mathf.Abs(input) > _flipThreshold)
        {
            bool shouldFaceLeft = input < 0f;

            if (_isFacingLeft != shouldFaceLeft)
            {
                _isFacingLeft = shouldFaceLeft;
                ApplyFacing();
            }
        }

        ApplyMirroredChildren();
    }

    private void ApplyFacing()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.flipX = _isFacingLeft;

        if (_visualRoot == null)
            return;

        if (_visualRoot.localScale != _baseLocalScale)
            _visualRoot.localScale = _baseLocalScale;
    }

    private void CacheMirroredChildren()
    {
        int childCount = transform.childCount;

        if (childCount <= 0)
        {
            _mirroredChildren = new MirroredChild[0];
            return;
        }

        _mirroredChildren = new MirroredChild[childCount];

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            child.TryGetComponent(out SpriteRenderer renderer);
            child.TryGetComponent(out Animator animator);

            _mirroredChildren[i] = new MirroredChild(
                child,
                renderer,
                animator,
                renderer != null && renderer.flipX);
        }
    }

    private void ApplyMirroredChildren()
    {
        for (int i = 0; i < _mirroredChildren.Length; i++)
        {
            MirroredChild child = _mirroredChildren[i];
            child.Apply(_isFacingLeft);
            _mirroredChildren[i] = child;
        }
    }

    private struct MirroredChild
    {
        private readonly Transform _transform;
        private readonly SpriteRenderer _renderer;
        private readonly Animator _animator;
        private readonly bool _baseFlipX;
        private readonly Vector3 _baseLocalPosition;
        private readonly float _baseLocalRotationZ;

        public MirroredChild(
            Transform transform,
            SpriteRenderer renderer,
            Animator animator,
            bool baseFlipX)
        {
            _transform = transform;
            _renderer = renderer;
            _animator = animator;
            _baseFlipX = baseFlipX;
            _baseLocalPosition = transform != null ? transform.localPosition : Vector3.zero;
            _baseLocalRotationZ = transform != null ? Mathf.DeltaAngle(0f, transform.localEulerAngles.z) : 0f;
        }

        public void Apply(bool isFacingLeft)
        {
            if (_renderer != null)
                _renderer.flipX = _baseFlipX != isFacingLeft;

            if (_transform == null)
                return;

            bool animatorControlsTransform = _animator != null && _animator.enabled;
            Vector3 localPosition = animatorControlsTransform
                ? _transform.localPosition
                : _baseLocalPosition;

            localPosition.x = isFacingLeft
                ? -Mathf.Abs(localPosition.x)
                : Mathf.Abs(localPosition.x);

            _transform.localPosition = localPosition;

            float localRotationZ = animatorControlsTransform
                ? Mathf.DeltaAngle(0f, _transform.localEulerAngles.z)
                : _baseLocalRotationZ;

            SetLocalRotationZ(isFacingLeft ? -localRotationZ : localRotationZ);
        }

        private void SetLocalRotationZ(float rotationZ)
        {
            Vector3 eulerAngles = _transform.localEulerAngles;
            eulerAngles.z = rotationZ;
            _transform.localEulerAngles = eulerAngles;
        }
    }
}
