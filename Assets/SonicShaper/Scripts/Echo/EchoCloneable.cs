using UnityEngine;

public class EchoCloneable : MonoBehaviour
{
    [Header("Clone Rules")]
    public bool allowEchoCopy = true;
    public Vector3 phaseOffsetLocal = new Vector3(0.8f, 0.12f, 0f);

    [Header("Echo Behavior")]
    public bool enableChaosMotion = true;
    public float chaosForce = 1.8f;
    public float chaosInterval = 0.45f;
    public bool forceOppositeLeverState = true;

    [Header("Echo Visual")]
    public bool overrideEmitterVisualSettings = false;
    public Color cloneTintColor = new Color(0.35f, 0.95f, 1f);
    [Range(0f, 1f)] public float cloneTintStrength = 0.55f;
    [Range(0.5f, 2f)] public float cloneBrightnessMultiplier = 1.18f;
}
