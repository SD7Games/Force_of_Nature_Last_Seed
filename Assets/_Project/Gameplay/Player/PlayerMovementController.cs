using System;
using LastSeed.Infrastructure.Input;

public sealed class PlayerMovementController
{
    private readonly PlayerMover _view;
    private PlayerMovementModel _model;

    public PlayerMovementController(PlayerMover view)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
    }

    public float MovementInput => _model != null ? _model.MovementInput : 0f;

    public void Initialize(IScreenBounds screenBounds)
    {
        _model = new PlayerMovementModel(
            _view.PositionX,
            _view.Speed,
            _view.Smooth,
            _view.EdgePadding,
            screenBounds);

        ApplyModelPosition();
    }

    public void Tick(PlayerInputSnapshot inputSnapshot, float deltaTime)
    {
        EnsureInitialized();

        if (inputSnapshot.IsTouchActive)
            _model.MoveByNormalizedScreenDeltaX(inputSnapshot.NormalizedTouchDeltaX);
        else
            _model.Move(inputSnapshot.HorizontalMovement, deltaTime);

        ApplyModelPosition();
    }

    public void StopMovement()
    {
        if (_model == null)
            return;

        _model.Stop();
    }

    public void ResetForNewRun()
    {
        EnsureInitialized();
        _model.Reset();
        ApplyModelPosition();
    }

    private void ApplyModelPosition()
    {
        _view.SetPositionX(_model.PositionX);
    }

    private void EnsureInitialized()
    {
        if (_model == null)
            throw new InvalidOperationException("Player movement is not initialized.");
    }
}
