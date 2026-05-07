using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerRigidbodyPusher : MonoBehaviour
{
    public float pushPower = 0.9f;
    public float maxPushMass = 20f;

    private CharacterController _controller;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;
        if (rb.mass > maxPushMass) return;

        Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z).normalized;
        if (pushDirection.sqrMagnitude < 0.01f) return;

        rb.AddForce(pushDirection * pushPower, ForceMode.Impulse);
    }
}
