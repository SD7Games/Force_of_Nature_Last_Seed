using System;
using System.Collections.Generic;

public sealed class RewardPopupChoiceBinder
{
    private static readonly RewardPresentationData DefaultPresentation =
        new(null, RewardPresentationKind.StatUpgrade);

    private readonly List<RewardButtonView> _buttons;
    private readonly RewardVisualCatalog _visualCatalog;
    private readonly Action<RewardChoiceData> _onClicked;

    public RewardPopupChoiceBinder(
        List<RewardButtonView> buttons,
        RewardVisualCatalog visualCatalog,
        Action<RewardChoiceData> onClicked)
    {
        _buttons = buttons;
        _visualCatalog = visualCatalog;
        _onClicked = onClicked;
    }

    public bool HasBindableButtons => _buttons != null && _buttons.Count > 0;
    public int ButtonCount => _buttons != null ? _buttons.Count : 0;
    public Action<RewardChoiceData> OnClicked => _onClicked;

    public RewardButtonView GetButton(int index)
    {
        return _buttons[index];
    }

    public void ApplyChoices(List<RewardChoiceData> choices, bool interactable)
    {
        if (_buttons == null)
            return;

        int choiceCount = choices != null ? choices.Count : 0;

        for (int i = 0; i < _buttons.Count; i++)
        {
            RewardButtonView button = _buttons[i];

            if (button == null)
                continue;

            if (i >= choiceCount)
            {
                button.KillAnimations();
                button.gameObject.SetActive(false);
                continue;
            }

            RewardChoiceData choice = choices[i];
            button.gameObject.SetActive(true);
            button.Bind(
                choice,
                GetPresentation(choice),
                _onClicked,
                interactable);
            button.ResetAnimatedState();
        }
    }

    public RewardPresentationData GetPresentation(RewardChoiceData choice)
    {
        return _visualCatalog != null && choice != null
            ? _visualCatalog.GetPresentation(choice.Category)
            : DefaultPresentation;
    }

    public void SetInteractable(bool enabled)
    {
        if (_buttons == null)
            return;

        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] != null)
                _buttons[i].SetInteractable(enabled);
        }
    }

    public RewardButtonView FindBoundButton(RewardChoiceData choice)
    {
        if (_buttons == null)
            return null;

        for (int i = 0; i < _buttons.Count; i++)
        {
            RewardButtonView button = _buttons[i];

            if (button != null && button.IsBoundTo(choice))
                return button;
        }

        return null;
    }

    public void ResetAnimatedState()
    {
        if (_buttons == null)
            return;

        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] != null)
                _buttons[i].ResetAnimatedState();
        }
    }

    public void KillAnimations()
    {
        if (_buttons == null)
            return;

        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] != null)
                _buttons[i].KillAnimations();
        }
    }
}
