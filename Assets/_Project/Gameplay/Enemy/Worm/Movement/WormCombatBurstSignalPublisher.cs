using System;
using LastSeed.Gameplay.Signals;
using Zenject;

public sealed class WormCombatBurstSignalPublisher
{
    private readonly SignalBus _signalBus;
    private bool _hasPublishedState;
    private bool _lastPublishedState;

    public WormCombatBurstSignalPublisher(SignalBus signalBus)
    {
        _signalBus = signalBus ?? throw new ArgumentNullException(nameof(signalBus));
    }

    public void PublishIfChanged(bool isActive)
    {
        if (_hasPublishedState && _lastPublishedState == isActive)
            return;

        _hasPublishedState = true;
        _lastPublishedState = isActive;
        _signalBus.Fire(new WormCombatBurstStateChangedSignal(isActive));
    }
}
