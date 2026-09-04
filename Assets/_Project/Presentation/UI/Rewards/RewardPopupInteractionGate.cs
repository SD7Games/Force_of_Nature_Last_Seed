using System;

public sealed class RewardPopupInteractionGate
{
    private readonly Func<bool> _canOpen;
    private readonly Action<bool> _setInteractionEnabled;
    private GateStage _stage;

    public RewardPopupInteractionGate(
        Func<bool> canOpen,
        Action<bool> setInteractionEnabled)
    {
        _canOpen = canOpen;
        _setInteractionEnabled = setInteractionEnabled;
    }

    public bool IsOpen { get; private set; }

    public void Close()
    {
        Stop();
        IsOpen = false;
        _setInteractionEnabled?.Invoke(false);
    }

    public void StartWhenSafe()
    {
        IsOpen = false;
        _setInteractionEnabled?.Invoke(false);
        _stage = GateStage.WaitInitialFrame;
    }

    public void Stop()
    {
        _stage = GateStage.Inactive;
    }

    public void Tick()
    {
        switch (_stage)
        {
            case GateStage.WaitInitialFrame:
                _stage = GateStage.WaitPointerRelease;
                return;
            case GateStage.WaitPointerRelease:
                if (!RewardPopupPointerState.IsAnyPressed())
                    _stage = GateStage.WaitReleaseFrame;
                return;
            case GateStage.WaitReleaseFrame:
                OpenIfAllowed();
                return;
        }
    }

    private void OpenIfAllowed()
    {
        _stage = GateStage.Inactive;

        if (_canOpen != null && !_canOpen())
            return;

        IsOpen = true;
        _setInteractionEnabled?.Invoke(true);
    }

    private enum GateStage
    {
        Inactive,
        WaitInitialFrame,
        WaitPointerRelease,
        WaitReleaseFrame
    }
}
