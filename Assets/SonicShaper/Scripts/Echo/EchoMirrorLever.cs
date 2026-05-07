using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class EchoMirrorLever : MonoBehaviour
{
    [Header("State")]
    public bool startsOn = false;
    public bool invokeEventsOnStart = false;

    [Header("Events")]
    public UnityEvent OnTurnedOn;
    public UnityEvent OnTurnedOff;

    public bool IsOn => _isOn;

    private bool _isOn;

    void Awake()
    {
        SetState(startsOn, invokeEventsOnStart);
    }

    void OnMouseDown()
    {
        Toggle();
    }

    public void Toggle()
    {
        SetState(!_isOn, true);
    }

    public void SetState(bool value, bool invokeEvents)
    {
        _isOn = value;
        if (!invokeEvents) return;

        if (_isOn) OnTurnedOn?.Invoke();
        else OnTurnedOff?.Invoke();
    }

    public void ConfigureAsEchoCopyOf(EchoMirrorLever sourceLever)
    {
        if (sourceLever == null) return;
        SetState(!sourceLever.IsOn, true);
    }
}
