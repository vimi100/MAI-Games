using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class EchoPolarityActivator : MonoBehaviour
{
    [Header("Detection")]
    public LayerMask triggerLayers = ~0;
    public EchoPolarity acceptedPolarity = EchoPolarity.Inverted;
    public bool onlyEchoCopies = false;

    [Header("Events")]
    public UnityEvent OnActivated;
    public UnityEvent OnDeactivated;

    private readonly HashSet<Collider> _inside = new HashSet<Collider>();

    void OnTriggerEnter(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, triggerLayers)) return;
        if (!IsAllowed(other)) return;
        if (!_inside.Add(other)) return;

        if (_inside.Count == 1)
            OnActivated?.Invoke();
    }

    void OnTriggerExit(Collider other)
    {
        if (_inside.Remove(other) && _inside.Count == 0)
            OnDeactivated?.Invoke();
    }

    void Update()
    {
        if (_inside.Count == 0) return;

        bool removedAny = false;
        List<Collider> stale = null;
        foreach (Collider col in _inside)
        {
            if (col != null) continue;
            if (stale == null) stale = new List<Collider>();
            stale.Add(col);
        }

        if (stale != null)
        {
            for (int i = 0; i < stale.Count; i++)
            {
                _inside.Remove(stale[i]);
                removedAny = true;
            }
        }

        if (removedAny && _inside.Count == 0)
            OnDeactivated?.Invoke();
    }

    bool IsAllowed(Collider other)
    {
        EchoPolarityObject polarityObject = other.GetComponentInParent<EchoPolarityObject>();
        EchoPolarity polarity = polarityObject != null ? polarityObject.Polarity : EchoPolarity.Normal;
        bool isEchoCopy = polarityObject != null && polarityObject.IsEchoCopy;

        if (onlyEchoCopies && !isEchoCopy)
            return false;

        if (acceptedPolarity == EchoPolarity.Any)
            return true;

        return polarity == acceptedPolarity;
    }

    static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
}
