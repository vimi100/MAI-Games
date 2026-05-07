using UnityEngine;

public class Room3Goal : MonoBehaviour
{
    public Renderer goalRenderer;
    public Color idleColor = new Color(0.2f, 0.5f, 1f);
    public Color completedColor = new Color(0.2f, 1f, 0.3f);

    private bool _completed;

    void Awake()
    {
        SetColor(idleColor);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_completed) return;
        if (!other.CompareTag("Player")) return;

        _completed = true;
        SetColor(completedColor);
        Debug.Log("Room 3 completed: you passed the turret corridor.");
    }

    void SetColor(Color color)
    {
        if (goalRenderer != null)
            goalRenderer.material.color = color;
    }
}
