using System;
using System.Collections.Generic;
using DG.Tweening;

internal sealed class RewardPopupRefreshAnimationBuilder
{
    private const float RevealPaddingSeconds = 0.02f;

    private readonly RewardPopupChoiceBinder _choiceBinder;
    private readonly RewardPopupAnimationSettings _settings;
    private readonly RewardPopupAudioPlayer _audioPlayer;

    public RewardPopupRefreshAnimationBuilder(
        RewardPopupChoiceBinder choiceBinder,
        RewardPopupAnimationSettings settings,
        RewardPopupAudioPlayer audioPlayer)
    {
        _choiceBinder = choiceBinder;
        _settings = settings;
        _audioPlayer = audioPlayer;
    }

    public void Populate(
        Sequence sequence,
        IReadOnlyList<RewardChoiceData> choices,
        RewardPopupState state,
        Action<RewardPopupState> applyRefreshedState)
    {
        sequence.InsertCallback(0f, _audioPlayer.PlayRefresh);

        int choiceCount = choices?.Count ?? 0;
        float lastDelay = 0f;

        for (int i = 0; i < _choiceBinder.ButtonCount; i++)
        {
            RewardButtonView button = _choiceBinder.GetButton(i);

            if (button == null)
                continue;

            if (i >= choiceCount)
            {
                button.gameObject.SetActive(false);
                continue;
            }

            RewardChoiceData choice = choices[i];
            button.gameObject.SetActive(true);

            float delay = i * _settings.RefreshCardStagger;
            lastDelay = delay;
            Tween tween = button.CreateRefreshTween(
                choice,
                _choiceBinder.GetPresentation(choice),
                _choiceBinder.OnClicked,
                delay,
                _settings.RefreshOutDuration,
                _settings.RefreshInDuration,
                _settings.RefreshOutEase,
                _settings.RefreshInEase);
            sequence.Join(tween);
        }

        sequence.InsertCallback(
            lastDelay + _settings.RefreshOutDuration + RevealPaddingSeconds,
            _audioPlayer.PlayCardReveal);
        sequence.AppendCallback(() => applyRefreshedState?.Invoke(state));
    }
}
