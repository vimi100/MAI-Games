using UnityEngine;

public class SimpleStateIndicator : MonoBehaviour
{
    public Renderer indicatorRenderer;
    public Color offColor = new Color(0.9f, 0.15f, 0.15f);
    public Color onColor = new Color(0.15f, 1f, 0.2f);

    void Awake()
    {
        SetOff();
    }

    public void SetOn()
    {
        SetColor(onColor);
    }

    public void SetOff()
    {
        SetColor(offColor);
    }

    private void SetColor(Color color)
    {
        if (indicatorRenderer == null) return;
        indicatorRenderer.material.color = color;
    }
}
