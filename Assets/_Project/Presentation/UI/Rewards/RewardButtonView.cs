using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RewardButtonView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _targetIcon;
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _description;
    [SerializeField] private TMP_Text _value;

    [Header("Rarity Popup Visuals")]
    [SerializeField] private Image _commonVisual;
    [SerializeField] private Image _rareVisual;
    [SerializeField] private Image _legendaryVisual;
    [SerializeField] private Image _weaponUnlockVisual;

    [Header("Text Highlighting")]
    [SerializeField] private Color32 _numberColor = new(105, 255, 120, 255);

    [Header("Text Colors")]
    [SerializeField] private Color32 _titleColor = new(255, 221, 75, 255);
    [SerializeField] private Color32 _descriptionColor = new(255, 255, 255, 255);
    [SerializeField] private Color32 _valueColor = new(255, 255, 255, 255);
    [SerializeField] private Color32 _weaponUnlockTitleColor = new(255, 232, 120, 255);
    [SerializeField] private Color32 _weaponUnlockDescriptionColor = new(238, 226, 255, 255);
    [SerializeField] private Color32 _weaponUnlockValueColor = new(105, 255, 120, 255);
    [SerializeField] private string _weaponUnlockValueFallback = "NEW";

    private RewardChoiceData _data;
    private RewardButtonContentPresenter _contentPresenter;
    private RewardButtonAnimator _animator;

    private event Action<RewardChoiceData> _onClick;

    public RectTransform RectTransform
    {
        get
        {
            EnsureControllers();
            return _animator.Root;
        }
    }

    private void Awake()
    {
        EnsureControllers();
    }

    public void Bind(
        RewardChoiceData data,
        RewardPresentationData presentation,
        Action<RewardChoiceData> onClick,
        bool interactable = true)
    {
        EnsureControllers();

        _data = data;
        _onClick = onClick;

        _contentPresenter.Apply(data, presentation);
        _animator.CaptureIconScale();

        if (_button == null)
            return;

        _button.interactable = interactable;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClick);
    }

    public void SetInteractable(bool interactable)
    {
        if (_button != null)
            _button.interactable = interactable;
    }

    public bool IsBoundTo(RewardChoiceData data)
    {
        return data != null && ReferenceEquals(_data, data);
    }

    public void KillAnimations()
    {
        EnsureControllers();
        _animator.Kill();
    }

    public void ResetAnimatedState()
    {
        EnsureControllers();
        _animator.Reset();
    }

    public void PrepareEnter(float yOffset, float startScaleMultiplier)
    {
        EnsureControllers();
        _animator.PrepareEnter(yOffset, startScaleMultiplier);
    }

    public Tween CreateEnterTween(float duration, Ease moveEase, Ease scaleEase)
    {
        EnsureControllers();
        return _animator.CreateEnter(duration, moveEase, scaleEase);
    }

    public Tween CreateRefreshTween(
        RewardChoiceData data,
        RewardPresentationData presentation,
        Action<RewardChoiceData> onClick,
        float delay,
        float outDuration,
        float inDuration,
        Ease outEase,
        Ease inEase)
    {
        EnsureControllers();
        SetInteractable(false);
        return _animator.CreateRefresh(
            () => Bind(data, presentation, onClick, false),
            delay,
            outDuration,
            inDuration,
            outEase,
            inEase);
    }

    public Tween CreateSelectedDismissTween(
        float focusDuration,
        float growDuration,
        float exitDuration,
        float exitYOffset,
        float focusScaleMultiplier,
        float exitScaleMultiplier,
        Ease focusEase,
        Ease exitEase)
    {
        EnsureControllers();
        SetInteractable(false);
        return _animator.CreateSelectedDismiss(
            focusDuration,
            growDuration,
            exitDuration,
            exitYOffset,
            focusScaleMultiplier,
            exitScaleMultiplier,
            focusEase,
            exitEase);
    }

    public Tween CreateUnselectedDismissTween(
        float duration,
        float exitYOffset,
        float exitScaleMultiplier,
        Ease exitEase)
    {
        EnsureControllers();
        SetInteractable(false);
        return _animator.CreateUnselectedDismiss(
            duration,
            exitYOffset,
            exitScaleMultiplier,
            exitEase);
    }

    private void EnsureControllers()
    {
        if (_contentPresenter != null)
            return;

        RewardButtonContentStyle style = new(
            _numberColor,
            _titleColor,
            _descriptionColor,
            _valueColor,
            _weaponUnlockTitleColor,
            _weaponUnlockDescriptionColor,
            _weaponUnlockValueColor,
            _weaponUnlockValueFallback);
        _contentPresenter = new RewardButtonContentPresenter(
            _targetIcon,
            _title,
            _description,
            _value,
            _commonVisual,
            _rareVisual,
            _legendaryVisual,
            _weaponUnlockVisual,
            style);
        _animator = new RewardButtonAnimator(
            transform as RectTransform,
            _canvasGroup,
            _targetIcon != null ? _targetIcon.rectTransform : null);
    }

    private void OnClick()
    {
        _onClick?.Invoke(_data);
    }
}
