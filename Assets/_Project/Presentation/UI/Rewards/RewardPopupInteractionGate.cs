using System;
using System.Collections;
using UnityEngine;

public sealed class RewardPopupInteractionGate
{
    private readonly MonoBehaviour _owner;
    private readonly Func<bool> _canOpen;
    private readonly Action<bool> _setInteractionEnabled;
    private Coroutine _coroutine;

    public RewardPopupInteractionGate(
        MonoBehaviour owner,
        Func<bool> canOpen,
        Action<bool> setInteractionEnabled)
    {
        _owner = owner;
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
        Stop();

        if (_owner == null || !_owner.isActiveAndEnabled)
            return;

        _coroutine = _owner.StartCoroutine(WaitForPointerReleaseThenEnable());
    }

    public void Stop()
    {
        if (_coroutine == null)
            return;

        if (_owner != null)
            _owner.StopCoroutine(_coroutine);

        _coroutine = null;
    }

    private IEnumerator WaitForPointerReleaseThenEnable()
    {
        yield return null;

        while (RewardPopupPointerState.IsAnyPressed())
            yield return null;

        yield return null;

        _coroutine = null;

        if (_canOpen != null && !_canOpen())
            yield break;

        IsOpen = true;
        _setInteractionEnabled?.Invoke(true);
    }
}
