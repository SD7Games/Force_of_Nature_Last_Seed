using LastSeed.Infrastructure.Input;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public sealed class PlayerController : MonoBehaviour
{
    private PlayerMover _playerMover;

    [Inject]
    public void Construct(PlayerMover playerMover)
    {
        _playerMover = playerMover;
    }

    public void Tick(PlayerInputSnapshot inputSnapshot, float deltaTime)
    {
        if (inputSnapshot.IsTouchActive)
        {
            _playerMover.MoveByNormalizedScreenDeltaX(inputSnapshot.NormalizedTouchDeltaX);
            return;
        }

        _playerMover.Move(inputSnapshot.HorizontalMovement, deltaTime);
    }

    public void StopMovement()
    {
        _playerMover.StopMovement();
    }
}
