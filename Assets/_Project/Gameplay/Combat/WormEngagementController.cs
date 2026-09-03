using LastSeed.Gameplay.Combat;
using LastSeed.Gameplay.Signals;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public sealed class WormEngagementController : MonoBehaviour
{
    private int _wormsInside;
    private ICombatSessionState _combatSessionState;
    private SignalBus _signalBus;
    private bool _isSubscribedToSignals;

    [Inject]
    public void Construct(ICombatSessionState combatSessionState, SignalBus signalBus)
    {
        _combatSessionState = combatSessionState;
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

    private void Start()
    {
        _wormsInside = 0;
        _combatSessionState.SetShootingEnabled(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out WormSegment segment))
            return;

        if (segment.Type != WormSegmentType.Head)
            return;

        _wormsInside++;

        if (_wormsInside == 1)
            _combatSessionState.SetShootingEnabled(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent(out WormSegment segment))
            return;

        if (segment.Type != WormSegmentType.Head)
            return;

        _wormsInside = Mathf.Max(0, _wormsInside - 1);

        if (_wormsInside == 0)
            _combatSessionState.SetShootingEnabled(false);
    }

    private void HandleWormDied(WormDiedSignal signal)
    {
        ResetState();
    }

    public void ResetState()
    {
        _wormsInside = 0;
        _combatSessionState.SetShootingEnabled(false);
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
