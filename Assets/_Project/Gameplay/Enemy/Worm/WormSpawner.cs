using System;
using System.Collections.Generic;
using LastSeed.Gameplay.Signals;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public sealed class WormSpawner : MonoBehaviour
{
    [Header("Rewards")]
    [SerializeField] private RewardDatabase _rewardDatabase;

    private WormSpawnLifecycle _spawnLifecycle;
    private WormAdaptiveHpController _adaptiveHpController;
    private SignalBus _signalBus;
    private bool _isSubscribedToSignals;

    [Inject]
    public void Construct(
        SignalBus signalBus,
        WormAdaptiveHpController adaptiveHpController,
        WormSpawnLifecycle spawnLifecycle)
    {
        _signalBus = signalBus;
        _adaptiveHpController = adaptiveHpController;
        _spawnLifecycle = spawnLifecycle;
        SubscribeToSignals();
    }

    private void OnEnable()
    {
        SubscribeToSignals();

        _spawnLifecycle?.RebindFacePresentation();
    }

    private void OnDisable()
    {
        UnsubscribeFromSignals();

        _spawnLifecycle?.UnbindFacePresentation();
    }

    private async void Start()
    {
        try
        {
            await _spawnLifecycle.PrewarmAsync(destroyCancellationToken);
            SpawnWorm();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            enabled = false;
        }
    }

    public void SpawnWorm()
    {
        _spawnLifecycle.Spawn(GetCocoonProfiles(), Time.time);
    }

    public void RestartWorm()
    {
        DespawnWorm();
        SpawnWorm();
    }

    public void DespawnWorm()
    {
        _spawnLifecycle.Despawn(Time.time);
    }

    public void SetRuntimePressureMultiplier(float multiplier)
    {
        _adaptiveHpController.SetRuntimePressureMultiplier(multiplier);
    }

    private void OnWeaponRuntimeStatsChanged(WeaponRuntimeStatsChangedSignal signal)
    {
        _adaptiveHpController.NotifyWeaponRuntimeStatsChanged(signal.OccurredAt);
    }

    private IReadOnlyList<CocoonRewardProfile> GetCocoonProfiles()
    {
        return _rewardDatabase != null
            ? _rewardDatabase.CocoonProfiles
            : CocoonRewardProfile.Defaults;
    }

    private void HandleReviveGranted(WormReviveGrantedSignal signal)
    {
        _adaptiveHpController.NotifyReviveGranted();
    }

    private void SubscribeToSignals()
    {
        if (_signalBus == null || _isSubscribedToSignals || !isActiveAndEnabled)
            return;

        _signalBus.Subscribe<WormReviveGrantedSignal>(HandleReviveGranted);
        _signalBus.Subscribe<WeaponRuntimeStatsChangedSignal>(OnWeaponRuntimeStatsChanged);
        _isSubscribedToSignals = true;
    }

    private void UnsubscribeFromSignals()
    {
        if (_signalBus == null || !_isSubscribedToSignals)
            return;

        _signalBus.Unsubscribe<WormReviveGrantedSignal>(HandleReviveGranted);
        _signalBus.Unsubscribe<WeaponRuntimeStatsChangedSignal>(OnWeaponRuntimeStatsChanged);
        _isSubscribedToSignals = false;
    }
}
