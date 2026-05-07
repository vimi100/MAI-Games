using UnityEngine;

public class EchoTurretTargeting : MonoBehaviour
{
    [Header("Targeting")]
    public Transform defaultTarget;
    public float detectionRadius = 14f;
    public float turnSpeed = 7f;
    public bool lockPitch = true;
    public float attackRange = 16f;
    public float attackInterval = 0.32f;
    [Range(0.5f, 1f)] public float aimDotThreshold = 0.93f;
    public Transform muzzlePoint;
    public float rocketSpeed = 24f;
    public float rocketDamage = 18f;
    public float rocketLifetime = 3f;
    public float rocketBlastRadius = 1.35f;

    [Header("Visual")]
    public Transform rotatingPart;
    public Renderer stateRenderer;
    public Color idleColor = new Color(0.9f, 0.2f, 0.2f);
    public Color distractedColor = new Color(0.2f, 1f, 0.3f);
    public Color firingColor = new Color(1f, 0.9f, 0.1f);

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shotClip;

    private Transform _currentTarget;
    private bool _isDistracted;
    private bool _combatActive;
    private Transform _playerTarget;
    private float _nextAttackTime;
    private AudioClip _generatedShotClip;

    void Update()
    {
        SelectTarget();
        RotateToTarget();
        HandleAttack();
        UpdateStateVisual();
    }

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        _generatedShotClip = GenerateLaserTone(1180f, 0.08f);
    }

    public void SetCombatActive(bool active, Transform playerTarget)
    {
        _combatActive = active;
        _playerTarget = playerTarget;
    }

    void SelectTarget()
    {
        EchoDistractionSource echoTarget = EchoDistractionSource.GetNearest(transform.position, detectionRadius);
        _isDistracted = echoTarget != null;

        if (!_isDistracted && defaultTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                defaultTarget = player.transform;
        }

        if (_combatActive && !_isDistracted && _playerTarget != null)
            _currentTarget = _playerTarget;
        else
            _currentTarget = _isDistracted ? echoTarget.transform : defaultTarget;
    }

    void RotateToTarget()
    {
        if (_currentTarget == null) return;

        Transform pivot = rotatingPart != null ? rotatingPart : transform;
        Vector3 direction = (_currentTarget.position - pivot.position).normalized;
        if (direction.sqrMagnitude < 0.001f) return;

        if (lockPitch)
            direction = new Vector3(direction.x, 0f, direction.z).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        pivot.rotation = Quaternion.Slerp(pivot.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    void HandleAttack()
    {
        if (!_combatActive || _isDistracted || _playerTarget == null)
            return;

        if (Time.time < _nextAttackTime)
            return;

        Transform pivot = rotatingPart != null ? rotatingPart : transform;
        Vector3 toPlayer = (_playerTarget.position - pivot.position);
        float sqDist = toPlayer.sqrMagnitude;
        if (sqDist > attackRange * attackRange)
            return;

        Vector3 forward = pivot.forward.normalized;
        if (Vector3.Dot(forward, toPlayer.normalized) < aimDotThreshold)
            return;

        Vector3 eyePos = (muzzlePoint != null ? muzzlePoint.position : pivot.position) + Vector3.up * 0.05f;
        if (Physics.Raycast(eyePos, toPlayer.normalized, out RaycastHit hit, attackRange, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == null || !hit.collider.CompareTag("Player"))
                return;
        }

        FireRocket();
        _nextAttackTime = Time.time + attackInterval;
    }

    void FireRocket()
    {
        Transform muzzle = muzzlePoint != null ? muzzlePoint : (rotatingPart != null ? rotatingPart : transform);
        Vector3 spawnPos = muzzle.position + muzzle.forward * 0.9f + Vector3.up * 0.05f;

        GameObject rocket = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rocket.name = "TurretRocket";
        rocket.transform.position = spawnPos;
        rocket.transform.localScale = Vector3.one * 0.18f;
        Renderer r = rocket.GetComponent<Renderer>();
        if (r != null) r.material.color = new Color(1f, 0.25f, 0.1f);

        Rigidbody rb = rocket.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        TurretRocket projectile = rocket.AddComponent<TurretRocket>();
        projectile.Initialize(_playerTarget, rocketSpeed, rocketDamage, rocketLifetime, rocketBlastRadius);

        if (audioSource != null)
            audioSource.PlayOneShot(shotClip != null ? shotClip : _generatedShotClip, 0.75f);
    }

    void UpdateStateVisual()
    {
        if (stateRenderer == null) return;
        if (_combatActive && !_isDistracted)
            stateRenderer.material.color = firingColor;
        else
            stateRenderer.material.color = _isDistracted ? distractedColor : idleColor;
    }

    static AudioClip GenerateLaserTone(float frequency, float duration, int sampleRate = 44100)
    {
        int samples = Mathf.Max(1, (int)(sampleRate * duration));
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-20f * t);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
        }

        AudioClip clip = AudioClip.Create("turret_laser", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
