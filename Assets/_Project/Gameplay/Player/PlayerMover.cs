using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerMover : MonoBehaviour
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _smooth = 10f;
    [SerializeField] private float _edgePadding = 0.5f;

    public float PositionX => transform.position.x;
    public float Speed => _speed;
    public float Smooth => _smooth;
    public float EdgePadding => _edgePadding;

    public void SetPositionX(float positionX)
    {
        Vector3 position = transform.position;
        position.x = positionX;
        transform.position = position;
    }
}
