using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LastSeed.Infrastructure.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BootstrapLoadingView : MonoBehaviour, ISceneTransition
{
    [Header("Progress")]
    [SerializeField] private Image _loadingProgressImage;
    [SerializeField, Min(0.1f)] private float _loadingDuration = 4f;
    [SerializeField] private bool _useProgressSteps = true;
    [SerializeField, Range(0f, 1f)] private float _firstProgressStep = 0.17f;
    [SerializeField, Range(0f, 1f)] private float _secondProgressStep = 0.45f;
    [SerializeField, Range(0f, 1f)] private float _thirdProgressStep = 0.83f;
    [SerializeField, Min(0f)] private float _progressStepMoveDuration = 0.18f;
    [SerializeField, Min(0f)] private float _progressStepPauseDuration = 0.5f;

    [Header("Status Text")]
    [SerializeField] private TMP_Text _loadingStatusText;
    [SerializeField, Min(0.1f)] private float _loadingStatusChangeIntervalMin = 1f;
    [SerializeField, Min(0.1f)] private float _loadingStatusChangeIntervalMax = 2f;
    [SerializeField] private string[] _loadingStatusPhrases =
    {
        "Waking the forest...",
        "Opening the gate...",
        "Growing the last seed...",
        "Preparing the run...",
        "Gathering sunlight...",
        "Calling tiny roots...",
        "Stirring old magic...",
        "Packing acorns...",
        "Sharpening thorns...",
        "Listening to leaves...",
        "Lighting the path...",
        "Planting hope..."
    };

    [Header("Idle Eyes")]
    [SerializeField] private Image[] _idleEyes;
    [SerializeField, Min(0f)] private float _idleOpenStartTime = 1.5f;
    [SerializeField, Min(0f)] private float _idleOpenEndTime = 1.85f;
    [SerializeField, Min(0f)] private float _idleCloseStartTime = 2.15f;
    [SerializeField, Min(0f)] private float _idleCloseEndTime = 2.45f;

    [Header("Angry Eyes")]
    [SerializeField] private Image[] _angryEyes;
    [SerializeField, Min(0f)] private float _angryOpenStartTime = 2.45f;
    [SerializeField, Min(0f)] private float _angryOpenEndTime = 2.85f;

    private Sequence _sequence;
    private UniTaskCompletionSource _completionSource;
    private CancellationTokenRegistration _cancellationRegistration;
    private int _lastStatusPhraseIndex = -1;

    public bool IsComplete { get; private set; }

    public UniTask PlayAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancelAnimation();
        ApplyInitialState();

        IsComplete = false;
        _completionSource = new UniTaskCompletionSource();
        _cancellationRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
            static state => ((BootstrapLoadingView)state).CancelAnimation(),
            this);
        _sequence = DOTween.Sequence()
            .SetTarget(this)
            .SetUpdate(true);

        SetRandomLoadingStatus();

        InsertProgressTween();
        InsertStatusTextCallbacks();
        InsertEyeFillTweens(_idleEyes, _idleOpenStartTime, _idleOpenEndTime, 1f);
        InsertEyeFillTweens(_idleEyes, _idleCloseStartTime, _idleCloseEndTime, 0f);
        InsertEyeFillTweens(_angryEyes, _angryOpenStartTime, _angryOpenEndTime, 1f);

        _sequence.InsertCallback(_loadingDuration, DoNothing);
        _sequence.OnComplete(Complete);
        return _completionSource.Task;
    }

    private void OnDestroy()
    {
        CancelAnimation();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _loadingDuration = Mathf.Max(0.1f, _loadingDuration);
        _firstProgressStep = Mathf.Clamp01(_firstProgressStep);
        _secondProgressStep = Mathf.Clamp(_secondProgressStep, _firstProgressStep, 1f);
        _thirdProgressStep = Mathf.Clamp(_thirdProgressStep, _secondProgressStep, 1f);
        _progressStepMoveDuration = Mathf.Max(0f, _progressStepMoveDuration);
        _progressStepPauseDuration = Mathf.Max(0f, _progressStepPauseDuration);
        _loadingStatusChangeIntervalMin = Mathf.Max(0.1f, _loadingStatusChangeIntervalMin);
        _loadingStatusChangeIntervalMax = Mathf.Max(
            _loadingStatusChangeIntervalMin,
            _loadingStatusChangeIntervalMax);

        ClampTimeline(ref _idleOpenStartTime, ref _idleOpenEndTime);
        ClampTimeline(ref _idleCloseStartTime, ref _idleCloseEndTime);
        ClampTimeline(ref _angryOpenStartTime, ref _angryOpenEndTime);
    }
#endif

    private void ApplyInitialState()
    {
        SetImageFill(_loadingProgressImage, 0f);
        SetImagesFill(_idleEyes, 0f);
        SetImagesFill(_angryEyes, 0f);
    }

    private void InsertProgressTween()
    {
        if (_loadingProgressImage == null)
            return;

        if (_useProgressSteps)
        {
            Sequence progressSequence = DOTween.Sequence()
                .SetUpdate(true);

            AppendProgressStep(progressSequence, _firstProgressStep, _progressStepMoveDuration, _progressStepPauseDuration);
            AppendProgressStep(progressSequence, _secondProgressStep, _progressStepMoveDuration, _progressStepPauseDuration);
            AppendProgressStep(progressSequence, _thirdProgressStep, _progressStepMoveDuration, _progressStepPauseDuration);

            float finalDuration = Mathf.Max(0f, _loadingDuration - progressSequence.Duration());
            progressSequence.Append(_loadingProgressImage
                .DOFillAmount(1f, finalDuration)
                .SetEase(Ease.OutQuad));

            _sequence.Insert(0f, progressSequence);
            return;
        }

        _sequence.Insert(0f, _loadingProgressImage
            .DOFillAmount(1f, _loadingDuration)
            .SetEase(Ease.Linear));
    }

    private void AppendProgressStep(Sequence progressSequence, float fillAmount, float moveDuration, float pauseDuration)
    {
        progressSequence.Append(_loadingProgressImage
            .DOFillAmount(fillAmount, moveDuration)
            .SetEase(Ease.OutQuad));

        if (pauseDuration > 0f)
            progressSequence.AppendInterval(pauseDuration);
    }

    private void InsertStatusTextCallbacks()
    {
        if (_loadingStatusText == null ||
            _loadingStatusPhrases == null ||
            _loadingStatusPhrases.Length == 0)
        {
            return;
        }

        float time = Random.Range(
            _loadingStatusChangeIntervalMin,
            _loadingStatusChangeIntervalMax);

        int maximumCallbackCount = Mathf.CeilToInt(
            _loadingDuration / _loadingStatusChangeIntervalMin);

        for (int callbackIndex = 0;
             callbackIndex < maximumCallbackCount && time < _loadingDuration;
             callbackIndex++)
        {
            _sequence.InsertCallback(time, SetRandomLoadingStatus);
            time += Random.Range(
                _loadingStatusChangeIntervalMin,
                _loadingStatusChangeIntervalMax);
        }
    }

    private void SetRandomLoadingStatus()
    {
        if (_loadingStatusText == null ||
            _loadingStatusPhrases == null ||
            _loadingStatusPhrases.Length == 0)
        {
            return;
        }

        int phraseIndex = GetRandomStatusPhraseIndex();
        string phrase = _loadingStatusPhrases[phraseIndex];

        if (string.IsNullOrEmpty(phrase))
            return;

        _lastStatusPhraseIndex = phraseIndex;
        _loadingStatusText.text = phrase;
    }

    private int GetRandomStatusPhraseIndex()
    {
        int phraseCount = _loadingStatusPhrases.Length;

        if (phraseCount <= 1)
            return 0;

        int phraseIndex = Random.Range(0, phraseCount);

        if (phraseIndex == _lastStatusPhraseIndex)
            phraseIndex = (phraseIndex + 1) % phraseCount;

        return phraseIndex;
    }

    private void InsertEyeFillTweens(Image[] eyes, float startTime, float endTime, float targetFill)
    {
        if (eyes == null)
            return;

        float duration = Mathf.Max(0f, endTime - startTime);

        for (int i = 0; i < eyes.Length; i++)
        {
            Image eye = eyes[i];

            if (eye == null)
                continue;

            if (duration <= 0f)
            {
                _sequence.InsertCallback(startTime, () => SetImageFill(eye, targetFill));
                continue;
            }

            _sequence.Insert(startTime, eye
                .DOFillAmount(targetFill, duration)
                .SetEase(Ease.OutQuad));
        }
    }

    private void CancelAnimation()
    {
        _sequence?.Kill();
        _sequence = null;

        UniTaskCompletionSource completionSource = ReleaseCompletionSource();
        completionSource?.TrySetCanceled();
    }

    private void Complete()
    {
        IsComplete = true;
        _sequence = null;

        UniTaskCompletionSource completionSource = ReleaseCompletionSource();
        completionSource?.TrySetResult();
    }

    private UniTaskCompletionSource ReleaseCompletionSource()
    {
        _cancellationRegistration.Dispose();
        _cancellationRegistration = default;

        UniTaskCompletionSource completionSource = _completionSource;
        _completionSource = null;
        return completionSource;
    }

    private static void SetImagesFill(Image[] images, float fillAmount)
    {
        if (images == null)
            return;

        for (int i = 0; i < images.Length; i++)
            SetImageFill(images[i], fillAmount);
    }

    private static void SetImageFill(Image image, float fillAmount)
    {
        if (image == null)
            return;

        image.fillAmount = fillAmount;
    }

    private static void DoNothing()
    {
    }

#if UNITY_EDITOR
    private static void ClampTimeline(ref float startTime, ref float endTime)
    {
        startTime = Mathf.Max(0f, startTime);
        endTime = Mathf.Max(startTime, endTime);
    }
#endif
}
