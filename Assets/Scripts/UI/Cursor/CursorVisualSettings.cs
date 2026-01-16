using UnityEngine;

[CreateAssetMenu(menuName = "Game/UI/Cursor Visual Settings", fileName = "CursorVisualSettings")]
public sealed class CursorVisualSettings : ScriptableObject
{
    [field: SerializeField] public Color FillColor { get; private set; } = new Color(1f, 1f, 1f, 1f);
    [field: SerializeField, Min(0.1f)] public float Scale { get; private set; } = 1f;

    public void SetFillColor(Color color)
    {
        FillColor = color;
    }

    public void SetScale(float scale)
    {
        Scale = Mathf.Max(0.1f, scale);
    }
}
