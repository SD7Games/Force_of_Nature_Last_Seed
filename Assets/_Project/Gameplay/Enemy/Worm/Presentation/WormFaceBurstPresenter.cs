using System;
using LastSeed.Gameplay.Signals;
using Zenject;

public sealed class WormFaceBurstPresenter : IInitializable, IDisposable
{
    private readonly SignalBus _signalBus;
    private WormFaceVisualController _faceVisual;
    private bool _isBurstActive;

    public WormFaceBurstPresenter(SignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        _signalBus.Subscribe<WormCombatBurstStateChangedSignal>(HandleStateChanged);
    }

    public void Dispose()
    {
        _signalBus.Unsubscribe<WormCombatBurstStateChangedSignal>(HandleStateChanged);
        Unbind();
    }

    public void Bind(WormFaceVisualController faceVisual)
    {
        _faceVisual = faceVisual;
        _faceVisual?.SetBoostActive(_isBurstActive);
    }

    public void Unbind()
    {
        _faceVisual?.SetBoostActive(false);
        _faceVisual = null;
    }

    private void HandleStateChanged(WormCombatBurstStateChangedSignal signal)
    {
        _isBurstActive = signal.IsActive;
        _faceVisual?.SetBoostActive(_isBurstActive);
    }
}
