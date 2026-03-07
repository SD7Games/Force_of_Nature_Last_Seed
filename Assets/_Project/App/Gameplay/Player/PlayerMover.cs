using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerMover : MonoBehaviour
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _smooth = 10f;
    [SerializeField] private float _minX = -3.5f;
    [SerializeField] private float _maxX = 3.5f;

    private float _currentInput;

    public void Move(float inputX)
    {
        _currentInput = Mathf.Lerp(_currentInput, inputX, _smooth * Time.deltaTime);

        Vector3 pos = transform.position;
        pos.x += _currentInput * _speed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, _minX, _maxX);
        transform.position = pos;
    }
}