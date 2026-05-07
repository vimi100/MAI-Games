using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EchoCopyChaos : MonoBehaviour
{
    public float impulseForce = 1.8f;
    public float impulseInterval = 0.45f;

    private Rigidbody _rb;
    private float _nextImpulseTime = 0f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _nextImpulseTime = Time.time + impulseInterval;
    }

    public void Configure(float force, float interval)
    {
        impulseForce = Mathf.Max(0f, force);
        impulseInterval = Mathf.Max(0.05f, interval);
    }

    void FixedUpdate()
    {
        if (_rb == null) return;
        if (Time.time < _nextImpulseTime) return;

        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(0f, 0.35f),
            Random.Range(-1f, 1f)
        ).normalized;

        if (randomDirection.sqrMagnitude < 0.01f)
            randomDirection = transform.forward;

        _rb.AddForce(randomDirection * impulseForce, ForceMode.Impulse);
        _nextImpulseTime = Time.time + impulseInterval;
    }
}
