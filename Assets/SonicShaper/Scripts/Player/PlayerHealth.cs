using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public Transform respawnPoint;
    public float currentHealth = 100f;

    [Header("Death & Respawn")]
    public bool respawnWhenFalling = true;
    public float fallRespawnY = -12f;
    public AudioClip deathClip;
    public float deathVolume = 0.9f;

    private bool _hudVisible = false;
    private Vector3 _defaultRespawnPosition;
    private AudioSource _audioSource;
    private AudioClip _generatedDeathTone;

    void Awake()
    {
        currentHealth = maxHealth;
        _defaultRespawnPosition = transform.position;
        _audioSource = GetComponent<AudioSource>();
        _generatedDeathTone = GenerateDeathTone(180f, 0.28f);
    }

    void Update()
    {
        if (!respawnWhenFalling) return;
        if (transform.position.y < fallRespawnY)
            DieAndRespawn();
    }

    public void SetHudVisible(bool visible)
    {
        _hudVisible = visible;
    }

    public void SetRespawnPoint(Transform point)
    {
        respawnPoint = point;
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (currentHealth <= 0f)
            DieAndRespawn();
    }

    void DieAndRespawn()
    {
        PlayDeathSound();
        currentHealth = maxHealth;

        if (respawnPoint != null)
            transform.position = respawnPoint.position;
        else
            transform.position = _defaultRespawnPosition;
    }

    void OnGUI()
    {
        if (!_hudVisible) return;

        float width = 280f;
        float height = 22f;
        Rect bg = new Rect(20f, 20f, width, height);
        GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        GUI.Box(bg, GUIContent.none);

        float ratio = Mathf.Clamp01(currentHealth / maxHealth);
        Rect fill = new Rect(23f, 23f, (width - 6f) * ratio, height - 6f);
        GUI.color = Color.Lerp(new Color(0.95f, 0.2f, 0.2f), new Color(0.15f, 1f, 0.25f), ratio);
        GUI.Box(fill, GUIContent.none);

        GUI.color = Color.white;
        GUI.Label(new Rect(24f, 18f, width, 24f), $"HP: {Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}");
    }

    void PlayDeathSound()
    {
        if (_audioSource == null) return;
        AudioClip clip = deathClip != null ? deathClip : _generatedDeathTone;
        if (clip != null)
            _audioSource.PlayOneShot(clip, deathVolume);
    }

    static AudioClip GenerateDeathTone(float startFrequency, float duration, int sampleRate = 44100)
    {
        int samples = Mathf.Max(1, (int)(duration * sampleRate));
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(startFrequency, startFrequency * 0.45f, t / duration);
            float envelope = Mathf.Exp(-6.5f * t);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope;
        }

        AudioClip clip = AudioClip.Create("death_fallback_tone", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
