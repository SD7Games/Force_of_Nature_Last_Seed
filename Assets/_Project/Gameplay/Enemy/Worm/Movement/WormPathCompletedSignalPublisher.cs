using System;
using LastSeed.Gameplay.Signals;
using Zenject;

public sealed class WormPathCompletedSignalPublisher : IInitializable, IDisposable
{
    private readonly WormController _wormController;
    private readonly SignalBus _signalBus;

    public WormPathCompletedSignalPublisher(
        WormController wormController,
        SignalBus signalBus)
    {
        _wormController = wormController;
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        _wormController.PathCompleted += PublishPathCompleted;
    }

    public void Dispose()
    {
        _wormController.PathCompleted -= PublishPathCompleted;
    }

    private void PublishPathCompleted()
    {
        _signalBus.Fire(new WormPathCompletedSignal(
            _wormController.HeadPathProgressNormalized));
    }
}
