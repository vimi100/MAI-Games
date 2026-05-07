using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class PressurePlate : MonoBehaviour
{
    [Header("Detection")]
    public float requiredMass = 0f;
    public LayerMask triggerLayers;
    public EchoPolarity acceptedPolarity = EchoPolarity.Any;
    public bool onlyEchoCopies = false;
    public float staticObjectMass = 1f;

    [Header("Visual")]
    public Renderer plateRenderer;
    public Color inactiveColor = Color.gray;
    public Color activeColor = Color.green;

    [Header("Events")]
    public UnityEvent OnActivated;
    public UnityEvent OnDeactivated;

    private readonly HashSet<Collider> _trackedColliders = new HashSet<Collider>();
    private bool _isActive = false;

    void Awake()
    {
        if (plateRenderer != null)
            plateRenderer.material.color = inactiveColor;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, triggerLayers)) return;
        if (!IsPolarityAccepted(other)) return;
        if (!_trackedColliders.Add(other)) return;
        EvaluateState();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, triggerLayers)) return;
        if (_trackedColliders.Remove(other))
            EvaluateState();
    }

    void Update()
    {
        if (_trackedColliders.Count == 0) return;
        EvaluateState();
    }

    bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    bool IsPolarityAccepted(Collider other)
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

    void EvaluateState()
    {
        float totalMass = 0f;
        int trackedObjects = 0;
        HashSet<Rigidbody> countedRigidbodies = new HashSet<Rigidbody>();
        List<Collider> staleColliders = null;

        foreach (Collider col in _trackedColliders)
        {
            if (col == null)
            {
                if (staleColliders == null) staleColliders = new List<Collider>();
                staleColliders.Add(col);
                continue;
            }

            Rigidbody rb = col.attachedRigidbody;
            if (rb != null)
            {
                if (countedRigidbodies.Add(rb))
                {
                    totalMass += rb.mass;
                    trackedObjects++;
                }
            }
            else
            {
                totalMass += Mathf.Max(0f, staticObjectMass);
                trackedObjects++;
            }
        }

        if (staleColliders != null)
        {
            for (int i = 0; i < staleColliders.Count; i++)
                _trackedColliders.Remove(staleColliders[i]);
        }

        bool hasAnyObject = trackedObjects > 0;
        bool hasEnoughMass = requiredMass <= 0f || totalMass >= requiredMass;
        bool shouldBeActive = hasAnyObject && hasEnoughMass;

        if (shouldBeActive == _isActive)
            return;

        _isActive = shouldBeActive;
        if (plateRenderer != null)
            plateRenderer.material.color = _isActive ? activeColor : inactiveColor;

        if (_isActive) OnActivated?.Invoke();
        else OnDeactivated?.Invoke();
    }
}
