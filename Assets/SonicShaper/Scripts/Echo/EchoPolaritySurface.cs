using UnityEngine;

[DisallowMultipleComponent]
public class EchoPolaritySurface : MonoBehaviour
{
    [Header("Filter")]
    public EchoPolarity acceptedPolarity = EchoPolarity.Inverted;
    public bool onlyEchoCopies = false;

    private Collider[] _surfaceColliders;

    void Awake()
    {
        _surfaceColliders = GetComponentsInChildren<Collider>(true);
    }

    void OnCollisionEnter(Collision collision)
    {
        SetCollisionIgnored(collision.collider, !IsAllowed(collision.collider));
    }

    void OnCollisionExit(Collision collision)
    {
        SetCollisionIgnored(collision.collider, false);
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

    void SetCollisionIgnored(Collider other, bool ignore)
    {
        if (other == null || _surfaceColliders == null) return;

        for (int i = 0; i < _surfaceColliders.Length; i++)
        {
            Collider surfaceCollider = _surfaceColliders[i];
            if (surfaceCollider == null) continue;
            Physics.IgnoreCollision(surfaceCollider, other, ignore);
        }
    }
}
