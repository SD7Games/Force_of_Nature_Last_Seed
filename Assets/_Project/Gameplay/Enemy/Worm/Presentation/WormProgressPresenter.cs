using LastSeed.Gameplay.Signals;
using TMPro;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public sealed class WormProgressPresenter : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private string _format = "Progress: {0}%";
    private SignalBus _signalBus;
    private bool _isSubscribedToSignals;

    [Inject]
    public void Construct(SignalBus signalBus)
    {
        _signalBus = signalBus;
        SubscribeToSignals();
    }

    private void Awake()
    {
        if (_text == null)
            _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        SubscribeToSignals();
        UpdateProgress(0, 0);
    }

    private void OnDisable()
    {
        UnsubscribeFromSignals();
    }

    private void UpdateProgress(WormDestructionProgressChangedSignal signal)
    {
        UpdateProgress(signal.DestroyedSegments, signal.TotalSegments);
    }

    private void SubscribeToSignals()
    {
        if (_signalBus == null || _isSubscribedToSignals || !isActiveAndEnabled)
            return;

        _signalBus.Subscribe<WormDestructionProgressChangedSignal>(UpdateProgress);
        _isSubscribedToSignals = true;
    }

    private void UnsubscribeFromSignals()
    {
        if (_signalBus == null || !_isSubscribedToSignals)
            return;

        _signalBus.Unsubscribe<WormDestructionProgressChangedSignal>(UpdateProgress);
        _isSubscribedToSignals = false;
    }

    private void UpdateProgress(int destroyedSegments, int totalSegments)
    {
        if (_text == null)
            return;

        int progress = 0;

        if (totalSegments > 0)
        {
            float normalized = Mathf.Clamp01(destroyedSegments / (float)totalSegments);
            progress = Mathf.RoundToInt(normalized * 100f);
        }

        _text.SetText(_format, progress);
    }
}
