using System;
using UnityEngine;

public static class WormReviveEvents
{
    public static event Action ReviveGranted;
    public static event Action ReviveRollbackCompleted;

    public static void PublishReviveGranted()
    {
        ReviveGranted?.Invoke();
    }

    public static void PublishReviveRollbackCompleted()
    {
        ReviveRollbackCompleted?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        ReviveGranted = null;
        ReviveRollbackCompleted = null;
    }
}
