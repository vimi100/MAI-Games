using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class EchoKeycardReader : MonoBehaviour
{
    [Header("Access")]
    public string requiredCode = "A1";
    public bool acceptOnlyEchoCopies = false;
    public bool consumeCardOnUse = false;

    [Header("Events")]
    public UnityEvent OnAccessGranted;
    public UnityEvent OnAccessDenied;

    void OnTriggerEnter(Collider other)
    {
        EchoKeycard card = other.GetComponentInParent<EchoKeycard>();
        if (card == null)
            return;

        EchoPolarityObject polarityObject = other.GetComponentInParent<EchoPolarityObject>();
        bool isEchoCopy = polarityObject != null && polarityObject.IsEchoCopy;

        if (acceptOnlyEchoCopies && !isEchoCopy)
        {
            OnAccessDenied?.Invoke();
            return;
        }

        if (!string.Equals(card.accessCode, requiredCode))
        {
            OnAccessDenied?.Invoke();
            return;
        }

        OnAccessGranted?.Invoke();

        if (consumeCardOnUse)
            Destroy(card.gameObject);
    }
}
