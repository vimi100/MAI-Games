using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TurretRocket : MonoBehaviour
{
    public float speed = 22f;
    public float damage = 14f;
    public float lifetime = 3f;
    public float homingStrength = 8f;
    public float explosionRadius = 1.35f;
    public float proximityFuseDistance = 0.95f;

    private Rigidbody _rb;
    private Transform _target;
    private float _spawnTime;
    private bool _detonated;

    public void Initialize(Transform target, float moveSpeed, float hitDamage, float life, float blastRadius)
    {
        _target = target;
        speed = moveSpeed;
        damage = hitDamage;
        lifetime = life;
        explosionRadius = Mathf.Max(0.2f, blastRadius);
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _spawnTime = Time.time;
        _rb.useGravity = false;
    }

    void FixedUpdate()
    {
        if (_detonated) return;

        if (Time.time - _spawnTime >= lifetime)
        {
            Detonate();
            return;
        }

        Vector3 desiredForward = transform.forward;
        if (_target != null)
        {
            Vector3 toTarget = (_target.position + Vector3.up * 0.8f) - transform.position;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                desiredForward = Vector3.Slerp(transform.forward, toTarget.normalized, homingStrength * Time.fixedDeltaTime);
                if (toTarget.magnitude <= proximityFuseDistance)
                {
                    Detonate();
                    return;
                }
            }
        }

        transform.rotation = Quaternion.LookRotation(desiredForward, Vector3.up);
        _rb.linearVelocity = transform.forward * speed;
    }

    void OnCollisionEnter(Collision collision)
    {
        Detonate();
    }

    void Detonate()
    {
        if (_detonated) return;
        _detonated = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            PlayerHealth health = hits[i].GetComponentInParent<PlayerHealth>();
            if (health != null)
                health.TakeDamage(damage);
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.45f, 0.2f, 0.35f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
#endif
}
