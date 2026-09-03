using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.App.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SceneFadeOverlay : MonoBehaviour
    {
        [SerializeField] private Image _targetImage;
        [SerializeField, Min(0f)] private float _fadeDuration = 1.25f;
        [SerializeField] private Ease _fadeEase = Ease.OutSine;
        [SerializeField] private bool _playOnEnable = true;
        [SerializeField] private bool _useUnscaledTime = true;
        [SerializeField] private bool _blockRaycastsDuringFade = true;

        private Tween _fadeTween;

        private void Awake()
        {
            ResolveTargetImage();
        }

        private void OnEnable()
        {
            if (_playOnEnable)
                PlayFadeOut();
        }

        private void OnDisable()
        {
            KillFade();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _fadeDuration = Mathf.Max(0f, _fadeDuration);

            if (_targetImage == null)
                TryGetComponent(out _targetImage);
        }
#endif

        public void PlayFadeOut()
        {
            KillFade();

            if (!ResolveTargetImage())
                return;

            SetAlpha(1f);

            if (_blockRaycastsDuringFade)
                _targetImage.raycastTarget = true;

            if (_fadeDuration <= 0f)
            {
                CompleteFade();
                return;
            }

            _fadeTween = _targetImage
                .DOFade(0f, _fadeDuration)
                .SetEase(_fadeEase)
                .SetUpdate(_useUnscaledTime)
                .SetTarget(this)
                .OnComplete(CompleteFade);
        }

        private bool ResolveTargetImage()
        {
            if (_targetImage != null)
                return true;

            return TryGetComponent(out _targetImage);
        }

        private void CompleteFade()
        {
            SetAlpha(0f);

            if (_blockRaycastsDuringFade && _targetImage != null)
                _targetImage.raycastTarget = false;

            _fadeTween = null;
        }

        private void KillFade()
        {
            if (_fadeTween == null)
                return;

            _fadeTween.Kill();
            _fadeTween = null;
        }

        private void SetAlpha(float alpha)
        {
            if (_targetImage == null)
                return;

            Color color = _targetImage.color;
            color.a = alpha;
            _targetImage.color = color;
        }
    }
}
