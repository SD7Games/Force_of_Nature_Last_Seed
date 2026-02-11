using UnityEngine;

public sealed class SeedMover : MonoBehaviour
{
    [SerializeField] private float _speed = 6f;
    [SerializeField] private float _minX = -3.5f;
    [SerializeField] private float _maxX = 3.5f;

    public void Move(float inputX)
    {
        Vector3 pos = transform.position;
        pos.x += inputX * _speed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, _minX, _maxX);
        transform.position = pos;
    }
}