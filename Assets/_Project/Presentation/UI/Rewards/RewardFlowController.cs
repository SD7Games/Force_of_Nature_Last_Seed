using System;
using System.Collections.Generic;

public sealed class RewardFlowController : IDisposable
{
    private const int AdRerollGuaranteedSlots = 1;

    private readonly RewardRollService _rollService;
    private readonly RewardApplyService _applyService;
    private readonly RewardPopupView _popup;
    private readonly PopupRoot _popupRoot;
    private readonly RewardAdOperation _rewardAdOperation;
    private readonly IRandomSource _randomSource;
    private readonly RewardAttemptState _attempts;
    private readonly RewardRequestQueue _requestQueue;

    private List<RewardChoiceData> _currentChoices;
    private CocoonRewardProfile _currentCocoonProfile;
    private RewardRollContext _currentRollContext;
    private RewardRarity _currentGuaranteeRarity;
    private bool _isDisposed;
    private bool _isPopupRequestActive;
    private bool _shouldOpenNextPendingRequest;

    public RewardFlowController(
        RewardRollService rollService,
        RewardApplyService applyService,
        RewardPopupView popup,
        PopupRoot popupRoot,
        RewardAdOperation rewardAdOperation,
        IRandomSource randomSource,
        RewardAttemptState attempts,
        RewardRequestQueue requestQueue)
    {
        _rollService = rollService;
        _applyService = applyService;
        _popup = popup;
        _popupRoot = popupRoot;
        _rewardAdOperation = rewardAdOperation
            ?? throw new ArgumentNullException(nameof(rewardAdOperation));
        _randomSource = randomSource
            ?? throw new ArgumentNullException(nameof(randomSource));
        _attempts = attempts ?? throw new ArgumentNullException(nameof(attempts));
        _requestQueue = requestQueue ?? throw new ArgumentNullException(nameof(requestQueue));

        if (_popup == null)
        {
            UnityEngine.Debug.LogWarning("RewardFlowController: reward popup is not assigned.");
            return;
        }

        _popup.Selected += HandleSelected;
        _popup.RerollRequested += HandleRerollRequested;
        _popup.AdRerollRequested += HandleAdRerollRequested;
        _popup.TakeAllRequested += HandleTakeAllRequested;
        _popup.Hidden += HandlePopupHidden;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        if (_popup != null)
        {
            _popup.Selected -= HandleSelected;
            _popup.RerollRequested -= HandleRerollRequested;
            _popup.AdRerollRequested -= HandleAdRerollRequested;
            _popup.TakeAllRequested -= HandleTakeAllRequested;
            _popup.Hidden -= HandlePopupHidden;
        }

        _rewardAdOperation.Cancel();
        _requestQueue.Clear();
        _isDisposed = true;
    }

    public bool Open(
        CocoonRewardProfile cocoonProfile = null,
        RewardRollContext rollContext = default)
    {
        if (_isDisposed)
            return false;

        RewardOpenRequest request = new(cocoonProfile, rollContext);

        if (_isPopupRequestActive)
        {
            _requestQueue.Enqueue(request);
            return true;
        }

        return StartOpenRequest(request);
    }

    private bool StartOpenRequest(RewardOpenRequest request)
    {
        _isPopupRequestActive = true;
        _currentCocoonProfile = request.CocoonProfile;
        _currentRollContext = request.RollContext;
        _rewardAdOperation.Cancel();

        if (!RollCurrentChoices())
        {
            CompleteCurrentPopupRequest();
            return false;
        }

        if (ShowCurrentChoices(false))
        {
            _shouldOpenNextPendingRequest = false;
            return true;
        }

        CompleteCurrentPopupRequest();
        return false;
    }

    public void ResetSession()
    {
        _requestQueue.Clear();
        _currentChoices = null;
        _currentCocoonProfile = null;
        _currentRollContext = default;
        _currentGuaranteeRarity = default;
        _attempts.Reset();
        _rewardAdOperation.Cancel();
        _isPopupRequestActive = false;
        _shouldOpenNextPendingRequest = false;
    }

    private void HandleSelected(RewardChoiceData choice)
    {
        if (_rewardAdOperation.IsPending)
            return;

        _shouldOpenNextPendingRequest = true;
        _applyService.Apply(choice);
    }

    private void HandleRerollRequested()
    {
        if (!_attempts.HasFreeReroll || _rewardAdOperation.IsPending)
            return;

        if (!RollCurrentChoices())
        {
            _popup?.SetAllButtonsInteractable(true);
            return;
        }

        _attempts.ConsumeFreeReroll();
        ShowCurrentChoices(true);
    }

    private void HandleAdRerollRequested()
    {
        if (_attempts.HasFreeReroll || !_attempts.HasAdReroll)
            return;

        if (_rewardAdOperation.IsPending)
            return;

        _popup?.SetAllButtonsInteractable(false);
        if (!_rewardAdOperation.TryBegin(CompleteAdRerollReward))
            _popup?.SetAllButtonsInteractable(true);
    }

    private void HandleTakeAllRequested()
    {
        if (_currentChoices == null || _currentChoices.Count == 0)
            return;

        if (!RewardAdRerollPolicy.CanOfferTakeAll(_currentRollContext))
            return;

        if (!_attempts.HasTakeAll || _rewardAdOperation.IsPending)
            return;

        _popup?.SetAllButtonsInteractable(false);
        if (!_rewardAdOperation.TryBegin(CompleteTakeAllReward))
            _popup?.SetAllButtonsInteractable(true);
    }

    private void CompleteAdRerollReward(bool rewardGranted)
    {
        if (_isDisposed)
            return;

        if (!rewardGranted)
        {
            ShowCurrentChoices(false);
            return;
        }

        _attempts.ConsumeAdReroll();

        RewardRarity adGuaranteeRarity = RewardAdRerollPolicy.RollGuaranteedRarity(
            _applyService?.RuntimeContext,
            _currentCocoonProfile,
            _currentRollContext,
            _randomSource);

        if (!RollCurrentChoices(
                adGuaranteeRarity,
                AdRerollGuaranteedSlots,
                isPaidAssistRoll: true))
        {
            _popup?.SetAllButtonsInteractable(true);
            return;
        }

        ShowCurrentChoices(true);
    }

    private void CompleteTakeAllReward(bool rewardGranted)
    {
        if (_isDisposed)
            return;

        if (!rewardGranted)
        {
            ShowCurrentChoices(false);
            return;
        }

        _attempts.ConsumeTakeAll();
        _shouldOpenNextPendingRequest = true;

        for (int i = 0; i < _currentChoices.Count; i++)
        {
            _applyService.Apply(_currentChoices[i]);
        }

        _popup?.Close();
    }

    private void HandlePopupHidden(PopupView popup)
    {
        if (popup != _popup || _isDisposed)
            return;

        bool shouldOpenNext = _shouldOpenNextPendingRequest;

        CompleteCurrentPopupRequest();

        if (shouldOpenNext)
        {
            TryOpenNextPendingRequest();
            return;
        }

        _requestQueue.Clear();
    }

    private void CompleteCurrentPopupRequest()
    {
        _currentChoices = null;
        _currentCocoonProfile = null;
        _currentRollContext = default;
        _currentGuaranteeRarity = default;
        _rewardAdOperation.Cancel();
        _isPopupRequestActive = false;
        _shouldOpenNextPendingRequest = false;
    }

    private void TryOpenNextPendingRequest()
    {
        int pendingRequestCount = _requestQueue.Count;

        for (int index = 0; index < pendingRequestCount; index++)
        {
            if (_isDisposed || _isPopupRequestActive ||
                !_requestQueue.TryDequeue(out RewardOpenRequest request))
            {
                return;
            }

            if (StartOpenRequest(request))
                return;
        }
    }

    private bool RollCurrentChoices(
        RewardRarity? forcedGuaranteeRarity = null,
        int forcedGuaranteeSlotCount = 1,
        bool isPaidAssistRoll = false)
    {
        RewardRollContext rollContext = isPaidAssistRoll
            ? _currentRollContext.WithPaidAssistRoll()
            : _currentRollContext;

        _currentGuaranteeRarity = forcedGuaranteeRarity
            ?? _rollService.RollGuaranteeRarity(
                _applyService.RuntimeContext,
                _currentCocoonProfile,
                rollContext);

        _currentChoices = _rollService.Roll3(
            _applyService.RuntimeContext,
            _currentCocoonProfile,
            _currentGuaranteeRarity,
            forcedGuaranteeSlotCount,
            rollContext);

        return _currentChoices != null && _currentChoices.Count > 0;
    }

    private bool ShowCurrentChoices(bool animateChoiceChanges)
    {
        if (_popup == null || _popupRoot == null)
        {
            UnityEngine.Debug.LogWarning("RewardFlowController: reward popup or popup root is not assigned.");
            return false;
        }

        bool canTakeAll = _attempts.HasTakeAll
            && !_rewardAdOperation.IsPending
            && RewardAdRerollPolicy.CanOfferTakeAll(_currentRollContext);

        bool isBound = _popup.Bind(
            _currentChoices,
            new RewardPopupState(
                _attempts.FreeRerollsLeft,
                _attempts.AdRerollsLeft,
                _attempts.TakeAllLeft,
                _currentGuaranteeRarity,
                RewardAdRerollPolicy.GetDisplayedGuaranteeRarity(_currentCocoonProfile),
                _attempts.HasFreeReroll && !_rewardAdOperation.IsPending,
                !_attempts.HasFreeReroll
                    && _attempts.HasAdReroll
                    && !_rewardAdOperation.IsPending,
                canTakeAll),
            animateChoiceChanges);

        if (!isBound)
            return false;

        _popupRoot.Show(_popup);
        return true;
    }

}
