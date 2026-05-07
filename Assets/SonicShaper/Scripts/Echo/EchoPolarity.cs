using UnityEngine;

public enum EchoPolarity
{
    Normal = 0,
    Inverted = 1,
    Any = 2
}

public static class EchoPolarityUtility
{
    public static EchoPolarity Invert(EchoPolarity polarity)
    {
        if (polarity == EchoPolarity.Normal) return EchoPolarity.Inverted;
        if (polarity == EchoPolarity.Inverted) return EchoPolarity.Normal;
        return EchoPolarity.Any;
    }
}

public class EchoPolarityObject : MonoBehaviour
{
    [SerializeField] private EchoPolarity polarity = EchoPolarity.Normal;
    [SerializeField] private bool isEchoCopy = false;

    public EchoPolarity Polarity => polarity;
    public bool IsEchoCopy => isEchoCopy;

    public void SetPolarity(EchoPolarity newPolarity, bool markAsEchoCopy)
    {
        polarity = newPolarity;
        isEchoCopy = markAsEchoCopy;
    }
}
