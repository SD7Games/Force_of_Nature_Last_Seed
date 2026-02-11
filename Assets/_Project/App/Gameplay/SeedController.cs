using UnityEngine;

public sealed class SeedController : MonoBehaviour
{

    [SerializeField] private SeedMover _mover;

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
    }
}