using UnityEngine;

public sealed class Shoot : MonoBehaviour
{
    [SerializeField] private float _speed = 8f;

    private void Update()
    {
        transform.position += Vector3.up * _speed * Time.deltaTime;

        if (transform.position.y > 6f)
            Destroy(gameObject);

    }
}