using System;
using LastSeed.Gameplay.Signals;
using Zenject;

public sealed class WormCombatBurstSignalPublisher : IInitializable, IDisposable
{
    private readonly WormCombatBurstController _burstController;
    private readonly SignalBus _signalBus;

    public WormCombatBurstSignalPublisher(
        WormCombatBurstController burstController,
        SignalBus signalBus)
    {
        _burstController = burstController;
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        _burstController.ActiveStateChanged += PublishStateChanged;
    }

    public void Dispose()
    {
        _burstController.ActiveStateChanged -= PublishStateChanged;
    }

    private void PublishStateChanged(bool isActive)
    {
        _signalBus.Fire(new WormCombatBurstStateChangedSignal(isActive));
    }
}
