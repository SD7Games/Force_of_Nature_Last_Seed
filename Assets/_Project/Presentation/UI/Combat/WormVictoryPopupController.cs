using LastSeed.Gameplay.Signals;
using LastSeed.Presentation.UI.Popups;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public sealed class WormVictoryPopupController : MonoBehaviour
{
    [SerializeField] private string _victoryPopupId = "WinPopup";
    private SignalBus _signalBus;
    private bool _isSubscribedToSignals;

    [Inject]
    public void Construct(SignalBus signalBus)
    {
        _signalBus = signalBus;
        SubscribeToSignals();
    }

    private void OnEnable()
    {
        SubscribeToSignals();
    }

    private void OnDisable()
    {
        UnsubscribeFromSignals();
    }

    private void HandleWormDied(WormDiedSignal signal)
    {
        if (string.IsNullOrEmpty(_victoryPopupId))
        {
            Debug.LogWarning("WormVictoryPopupController: victory popup id is empty.", this);
            return;
        }

        _signalBus.Fire(new ShowPopupRequestedSignal(_victoryPopupId));
    }

    private void SubscribeToSignals()
    {
        if (_signalBus == null || _isSubscribedToSignals || !isActiveAndEnabled)
            return;

        _signalBus.Subscribe<WormDiedSignal>(HandleWormDied);
        _isSubscribedToSignals = true;
    }

    private void UnsubscribeFromSignals()
    {
        if (_signalBus == null || !_isSubscribedToSignals)
            return;

        _signalBus.Unsubscribe<WormDiedSignal>(HandleWormDied);
        _isSubscribedToSignals = false;
    }
}
