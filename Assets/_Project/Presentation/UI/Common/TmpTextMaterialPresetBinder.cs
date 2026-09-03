using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class TmpTextMaterialPresetBinder : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private TMP_FontAsset _fontAsset;
    [SerializeField] private Material _materialPreset;

    private void Awake()
    {
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_text == null)
            TryGetComponent(out _text);
    }
#endif

    public void Apply()
    {
        if (_text == null && !TryGetComponent(out _text))
            return;

        if (_fontAsset != null && _text.font != _fontAsset)
            _text.font = _fontAsset;

        if (_materialPreset != null && _text.fontSharedMaterial != _materialPreset)
            _text.fontSharedMaterial = _materialPreset;

        _text.SetMaterialDirty();
    }
}
