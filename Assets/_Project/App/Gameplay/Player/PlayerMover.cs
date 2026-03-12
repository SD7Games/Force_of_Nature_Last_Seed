using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerMover : MonoBehaviour
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _smooth = 10f;
    [SerializeField] private float _edgePadding = 0.5f;

    private float _currentInput;

    public void Move(float inputX)
    {
        _currentInput = Mathf.Lerp(_currentInput, inputX, _smooth * Time.deltaTime);

        Vector3 pos = transform.position;

        pos.x += _currentInput * _speed * Time.deltaTime;

        float minX = ScreenBounds.Left + _edgePadding;
        float maxX = ScreenBounds.Right - _edgePadding;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);

        transform.position = pos;
    }
}