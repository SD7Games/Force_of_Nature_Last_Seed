using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class YSorter : MonoBehaviour
{
    private SpriteRenderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        _renderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
    }
}