using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum FrequencyMode { Low, High }
public enum AbilityMode { EchoCopy, Grab }

public class SoundEmitter : MonoBehaviour
{
    [Header("Ability Switch")]
    public KeyCode switchAbilityKey = KeyCode.F;

    [Header("Echo Copy")]
    public KeyCode castKey = KeyCode.E;
    public LayerMask cloneTargetMask = ~0;
    public float maxCopyDistance = 5f;
    public float copyLifetime = 8f;
    public float cooldown = 12f;
    public int maxSimultaneousCopies = 1;
    public bool secondSlotUnlocked = false;
    public Vector3 defaultPhaseOffset = new Vector3(0.8f, 0.12f, 0f);
    public float placementPadding = 0.08f;

    [Header("Echo Visuals")]
    public Color cloneTintColor = new Color(0.35f, 0.95f, 1f);
    [Range(0f, 1f)] public float cloneTintStrength = 0.55f;
    [Range(0.5f, 2f)] public float cloneBrightnessMultiplier = 1.18f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip createCopyClip;
    public AudioClip failCopyClip;
    public AudioClip grabStartClip;
    public AudioClip grabDropClip;

    [Header("Grab Ability")]
    public LayerMask grabTargetMask = ~0;
    public float maxGrabDistance = 5f;
    public float holdDistance = 2.6f;
    public float holdMoveSpeed = 16f;
    public float holdMaxSpeed = 11f;
    public float maxHeldMass = 25f;
    public float dropDistance = 8f;
    public bool showAbilityOverlay = true;

    // Kept for compatibility with existing UI and scripts.
    public FrequencyMode CurrentMode { get; private set; } = FrequencyMode.Low;
    public AbilityMode CurrentAbility { get; private set; } = AbilityMode.EchoCopy;
    public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);
    public int ActiveCopyCount => _activeCopies.Count;
    public int MaxCopyCount => GetMaxCopies();
    public bool IsReady => _cooldownTimer <= 0f;
    public bool IsHoldingObject => _heldRigidbody != null;
    public string HeldObjectName => _heldRigidbody != null ? _heldRigidbody.gameObject.name : string.Empty;

    private float _cooldownTimer = 0f;
    private AudioClip _generatedSuccessTone;
    private AudioClip _generatedErrorTone;
    private readonly List<EchoCopyMarker> _activeCopies = new List<EchoCopyMarker>();
    private PlayerController _playerController;
    private Camera _mainCamera;
    private MaterialPropertyBlock _propBlock;
    private Rigidbody _heldRigidbody;
    private float _heldOriginalLinearDamping;
    private float _heldOriginalAngularDamping;
    private bool _heldOriginalUseGravity;

    void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _mainCamera = Camera.main;
        _propBlock = new MaterialPropertyBlock();
        _generatedSuccessTone = GenerateTone(560f, 0.16f, decay: 12f);
        _generatedErrorTone = GenerateTone(140f, 0.2f, decay: 6f);
    }

    void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(switchAbilityKey))
            SwitchAbility();

        if (Input.GetKeyDown(castKey))
            UseCurrentAbility();
    }

    void FixedUpdate()
    {
        UpdateHeldObjectMotion();
    }

    void OnDisable()
    {
        ReleaseHeldObject(false);
    }

    void SwitchAbility()
    {
        if (CurrentAbility == AbilityMode.Grab && _heldRigidbody != null)
            ReleaseHeldObject(true);

        CurrentAbility = CurrentAbility == AbilityMode.EchoCopy
            ? AbilityMode.Grab
            : AbilityMode.EchoCopy;
    }

    void UseCurrentAbility()
    {
        if (CurrentAbility == AbilityMode.EchoCopy)
            TryCastEchoCopy();
        else
            ToggleGrabObject();
    }

    void ToggleGrabObject()
    {
        if (_heldRigidbody != null)
        {
            ReleaseHeldObject(true);
            return;
        }

        if (!TryGetGrabTarget(out Rigidbody target))
        {
            PlayClip(failCopyClip != null ? failCopyClip : _generatedErrorTone);
            return;
        }

        GrabObject(target);
        PlayClip(grabStartClip != null ? grabStartClip : _generatedSuccessTone);
    }

    bool TryGetGrabTarget(out Rigidbody target)
    {
        target = null;
        Ray ray = BuildAimRay();
        if (!Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, grabTargetMask, QueryTriggerInteraction.Ignore))
            return false;

        Rigidbody rb = hit.rigidbody != null ? hit.rigidbody : hit.collider.attachedRigidbody;
        if (rb == null || rb.isKinematic)
            return false;

        if (rb.mass > maxHeldMass)
            return false;

        if (rb.CompareTag("Player"))
            return false;

        target = rb;
        return true;
    }

    void GrabObject(Rigidbody target)
    {
        _heldRigidbody = target;
        _heldOriginalLinearDamping = _heldRigidbody.linearDamping;
        _heldOriginalAngularDamping = _heldRigidbody.angularDamping;
        _heldOriginalUseGravity = _heldRigidbody.useGravity;

        _heldRigidbody.useGravity = false;
        _heldRigidbody.linearDamping = Mathf.Max(_heldRigidbody.linearDamping, 2f);
        _heldRigidbody.angularDamping = Mathf.Max(_heldRigidbody.angularDamping, 8f);
        _heldRigidbody.linearVelocity = Vector3.zero;
    }

    void ReleaseHeldObject(bool playAudio)
    {
        if (_heldRigidbody == null)
            return;

        _heldRigidbody.useGravity = _heldOriginalUseGravity;
        _heldRigidbody.linearDamping = _heldOriginalLinearDamping;
        _heldRigidbody.angularDamping = _heldOriginalAngularDamping;
        _heldRigidbody = null;

        if (playAudio)
            PlayClip(grabDropClip != null ? grabDropClip : _generatedErrorTone);
    }

    void UpdateHeldObjectMotion()
    {
        if (_heldRigidbody == null)
            return;

        Transform aimTransform = _playerController != null && _playerController.cameraTransform != null
            ? _playerController.cameraTransform
            : (_mainCamera != null ? _mainCamera.transform : transform);

        Vector3 targetPos = aimTransform.position + aimTransform.forward * holdDistance;
        Vector3 toTarget = targetPos - _heldRigidbody.worldCenterOfMass;
        if (toTarget.magnitude > dropDistance)
        {
            ReleaseHeldObject(false);
            return;
        }

        Vector3 desiredVelocity = Vector3.ClampMagnitude(toTarget * holdMoveSpeed, holdMaxSpeed);
        _heldRigidbody.linearVelocity = Vector3.Lerp(_heldRigidbody.linearVelocity, desiredVelocity, 0.6f);
        _heldRigidbody.angularVelocity *= 0.9f;
    }

    public void UnlockSecondCopySlot()
    {
        secondSlotUnlocked = true;
    }

    void TryCastEchoCopy()
    {
        CleanupDeadClones();

        if (_cooldownTimer > 0f)
        {
            PlayClip(failCopyClip != null ? failCopyClip : _generatedErrorTone);
            return;
        }

        if (!TryGetCloneTarget(out GameObject sourceObject, out EchoCloneable cloneableConfig, out RaycastHit hit))
        {
            PlayClip(failCopyClip != null ? failCopyClip : _generatedErrorTone);
            return;
        }

        CreateEchoCopy(sourceObject, cloneableConfig, hit);
        _cooldownTimer = cooldown;
        PlayClip(createCopyClip != null ? createCopyClip : _generatedSuccessTone);
    }

    bool TryGetCloneTarget(out GameObject sourceObject, out EchoCloneable cloneableConfig, out RaycastHit hit)
    {
        sourceObject = null;
        cloneableConfig = null;
        hit = default;

        Ray ray = BuildAimRay();
        if (!Physics.Raycast(ray, out hit, maxCopyDistance, cloneTargetMask, QueryTriggerInteraction.Ignore))
            return false;

        Rigidbody rb = hit.rigidbody != null ? hit.rigidbody : hit.collider.attachedRigidbody;
        if (rb == null)
            return false;

        sourceObject = rb.gameObject;
#if UNITY_EDITOR
        RemoveMissingScriptsInEditor(sourceObject);
#endif
        cloneableConfig = sourceObject.GetComponent<EchoCloneable>();

        if (cloneableConfig != null && !cloneableConfig.allowEchoCopy)
            return false;

        if (sourceObject.CompareTag("Player"))
            return false;

        if (sourceObject.GetComponent<EchoCopyMarker>() != null)
            return false;

        return true;
    }

    Ray BuildAimRay()
    {
        Transform aimTransform = _playerController != null && _playerController.cameraTransform != null
            ? _playerController.cameraTransform
            : (_mainCamera != null ? _mainCamera.transform : transform);
        return new Ray(aimTransform.position, aimTransform.forward);
    }

    void CreateEchoCopy(GameObject sourceObject, EchoCloneable config, RaycastHit hit)
    {
        int maxCopies = GetMaxCopies();
        while (_activeCopies.Count >= maxCopies)
            RemoveOldestClone();

        Vector3 spawnPosition = CalculateSpawnPosition(sourceObject, config, hit);
        GameObject clone = Instantiate(sourceObject, spawnPosition, sourceObject.transform.rotation);

        if (clone.CompareTag("Player"))
            clone.tag = "Untagged";

        StripUnsupportedComponents(clone);
        ApplyEchoPolarity(sourceObject, clone);
        ApplyEchoVisual(clone, config);
        ApplyCopyChaos(clone, config);
        EnsureDistractionSource(clone, config);
        ConfigureMirrorLever(sourceObject, clone, config);

        EchoCopyMarker marker = clone.GetComponent<EchoCopyMarker>();
        if (marker == null)
            marker = clone.AddComponent<EchoCopyMarker>();

        marker.Initialize(sourceObject, copyLifetime, HandleCopyExpired);
        _activeCopies.Add(marker);
    }

    Vector3 CalculateSpawnPosition(GameObject sourceObject, EchoCloneable config, RaycastHit hit)
    {
        Vector3 phaseOffset = config != null ? config.phaseOffsetLocal : defaultPhaseOffset;
        Vector3 worldOffset = sourceObject.transform.TransformDirection(phaseOffset);
        Vector3 byOffset = sourceObject.transform.position + worldOffset;

        Collider sourceCollider = sourceObject.GetComponentInChildren<Collider>();
        if (sourceCollider == null)
            return byOffset;

        Vector3 pushDirection = hit.normal.sqrMagnitude > 0.01f ? hit.normal.normalized : sourceObject.transform.right;
        float sourceExtent = sourceCollider.bounds.extents.magnitude;
        float pushDistance = Mathf.Max(0.1f, sourceExtent * 0.35f + placementPadding);
        Vector3 byHit = hit.point + pushDirection * pushDistance;

        return Vector3.Lerp(byOffset, byHit, 0.75f);
    }

    void StripUnsupportedComponents(GameObject clone)
    {
        CharacterController controller = clone.GetComponent<CharacterController>();
        if (controller != null) Destroy(controller);

        Camera[] cameras = clone.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
            Destroy(cameras[i]);

        AudioListener[] listeners = clone.GetComponentsInChildren<AudioListener>(true);
        for (int i = 0; i < listeners.Length; i++)
            Destroy(listeners[i]);

        MonoBehaviour[] scripts = clone.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < scripts.Length; i++)
        {
            MonoBehaviour script = scripts[i];
            if (script == null) continue;

            if (script is PlayerController ||
                script is PlayerDash ||
                script is SoundEmitter ||
                script is HUD)
            {
                Destroy(script);
            }
        }
    }

    void ApplyEchoPolarity(GameObject sourceObject, GameObject clone)
    {
        EchoPolarityObject sourcePolarity = sourceObject.GetComponent<EchoPolarityObject>();
        EchoPolarity sourceValue = sourcePolarity != null ? sourcePolarity.Polarity : EchoPolarity.Normal;
        EchoPolarity cloneValue = EchoPolarityUtility.Invert(sourceValue);

        EchoPolarityObject clonePolarity = clone.GetComponent<EchoPolarityObject>();
        if (clonePolarity == null)
            clonePolarity = clone.AddComponent<EchoPolarityObject>();

        clonePolarity.SetPolarity(cloneValue, true);
    }

    void ApplyEchoVisual(GameObject clone, EchoCloneable config)
    {
        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return;
        if (_propBlock == null)
            _propBlock = new MaterialPropertyBlock();

        bool useCloneableOverride = config != null && config.overrideEmitterVisualSettings;
        Color tintColor = useCloneableOverride ? config.cloneTintColor : cloneTintColor;
        float tintStrength = useCloneableOverride ? config.cloneTintStrength : cloneTintStrength;
        float brightnessMultiplier = useCloneableOverride ? config.cloneBrightnessMultiplier : cloneBrightnessMultiplier;
        tintStrength = Mathf.Clamp01(tintStrength);
        brightnessMultiplier = Mathf.Clamp(brightnessMultiplier, 0.5f, 2f);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;

            renderer.GetPropertyBlock(_propBlock);
            Material sharedMat = renderer.sharedMaterial;
            Color baseColor = Color.white;
            if (sharedMat != null)
            {
                if (sharedMat.HasProperty("_BaseColor"))
                    baseColor = sharedMat.GetColor("_BaseColor");
                else if (sharedMat.HasProperty("_Color"))
                    baseColor = sharedMat.color;
            }
            Color finalColor = Color.Lerp(baseColor, tintColor, tintStrength) * brightnessMultiplier;
            finalColor.r = Mathf.Clamp01(finalColor.r);
            finalColor.g = Mathf.Clamp01(finalColor.g);
            finalColor.b = Mathf.Clamp01(finalColor.b);
            finalColor.a = 1f;
            if (sharedMat != null && sharedMat.HasProperty("_BaseColor"))
                _propBlock.SetColor("_BaseColor", finalColor);
            if (sharedMat == null || sharedMat.HasProperty("_Color"))
                _propBlock.SetColor("_Color", finalColor);
            renderer.SetPropertyBlock(_propBlock);
        }
    }

    void ApplyCopyChaos(GameObject clone, EchoCloneable config)
    {
        bool enableChaos = config == null || config.enableChaosMotion;
        Rigidbody rb = clone.GetComponent<Rigidbody>();
        if (!enableChaos || rb == null)
            return;

        EchoCopyChaos chaos = clone.GetComponent<EchoCopyChaos>();
        if (chaos == null)
            chaos = clone.AddComponent<EchoCopyChaos>();

        float force = config != null ? config.chaosForce : 1.8f;
        float interval = config != null ? config.chaosInterval : 0.45f;
        chaos.Configure(force, interval);
    }

    void ConfigureMirrorLever(GameObject sourceObject, GameObject clone, EchoCloneable config)
    {
        bool shouldInvertLever = config == null || config.forceOppositeLeverState;
        if (!shouldInvertLever)
            return;

        EchoMirrorLever sourceLever = sourceObject.GetComponent<EchoMirrorLever>();
        EchoMirrorLever cloneLever = clone.GetComponent<EchoMirrorLever>();
        if (sourceLever != null && cloneLever != null)
            cloneLever.ConfigureAsEchoCopyOf(sourceLever);
    }

    void EnsureDistractionSource(GameObject clone, EchoCloneable config)
    {
        bool enableChaos = config == null || config.enableChaosMotion;
        if (!enableChaos) return;

        EchoDistractionSource distraction = clone.GetComponent<EchoDistractionSource>();
        if (distraction == null)
            distraction = clone.AddComponent<EchoDistractionSource>();
        distraction.sourceRadius = 12f;
    }

    void HandleCopyExpired(EchoCopyMarker marker)
    {
        if (marker != null)
            _activeCopies.Remove(marker);
    }

    void CleanupDeadClones()
    {
        for (int i = _activeCopies.Count - 1; i >= 0; i--)
        {
            if (_activeCopies[i] == null)
                _activeCopies.RemoveAt(i);
        }
    }

    void RemoveOldestClone()
    {
        if (_activeCopies.Count == 0)
            return;

        EchoCopyMarker oldest = _activeCopies[0];
        _activeCopies.RemoveAt(0);
        if (oldest != null)
            Destroy(oldest.gameObject);
    }

    int GetMaxCopies()
    {
        return secondSlotUnlocked ? Mathf.Max(2, maxSimultaneousCopies) : Mathf.Max(1, maxSimultaneousCopies);
    }

#if UNITY_EDITOR
    void RemoveMissingScriptsInEditor(GameObject target)
    {
        if (target == null) return;
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);

        Transform[] children = target.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(children[i].gameObject);
    }
#endif

    static AudioClip GenerateTone(float frequency, float duration, float decay = 5f, int sampleRate = 44100)
    {
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * decay);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
        }
        var clip = AudioClip.Create("gen_tone", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    public float GetCooldownRatio()
    {
        if (cooldown <= 0f) return 0f;
        return Mathf.Clamp01(_cooldownTimer / cooldown);
    }

    void OnGUI()
    {
        if (!showAbilityOverlay) return;

        string line1 = $"Ability: {(CurrentAbility == AbilityMode.EchoCopy ? "Echo Copy" : "Grab")}";
        string line2;

        if (CurrentAbility == AbilityMode.EchoCopy)
            line2 = IsReady ? $"E: Clone ({ActiveCopyCount}/{MaxCopyCount})" : $"Cooldown: {CooldownRemaining:0.0}s";
        else
            line2 = IsHoldingObject ? $"Holding: {HeldObjectName}" : "E: Pick/Drop object";

        GUI.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        Rect bg = new Rect(Screen.width - 320f, 16f, 300f, 58f);
        GUI.Box(bg, GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(Screen.width - 308f, 22f, 286f, 20f), line1 + " (F to switch)");
        GUI.Label(new Rect(Screen.width - 308f, 40f, 286f, 20f), line2);
    }
}
