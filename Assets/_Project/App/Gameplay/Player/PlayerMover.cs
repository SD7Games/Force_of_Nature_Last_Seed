using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerMover : MonoBehaviour
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _minX = -3.5f;
    [SerializeField] private float _maxX = 3.5f;

    private void Awake()
    {
        Debug.Log($"PlayerMover Awake on: {gameObject.name}", this);
    }

    public void Move(float inputX)
    {
        Debug.Log($"input={inputX}, speed={_speed}, delta={inputX * _speed * Time.deltaTime}");
        Vector3 pos = transform.position;
        pos.x += inputX * _speed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, _minX, _maxX);
        transform.position = pos;
    }
}