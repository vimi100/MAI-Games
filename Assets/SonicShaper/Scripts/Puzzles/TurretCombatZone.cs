using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TurretCombatZone : MonoBehaviour
{
    public EchoTurretTargeting turret;
    public Transform roomRespawnPoint;

    void Awake()
    {
        Collider c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        health.SetHudVisible(true);
        if (roomRespawnPoint != null)
            health.SetRespawnPoint(roomRespawnPoint);

        if (turret != null)
            turret.SetCombatActive(true, other.transform);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
            health.SetHudVisible(false);

        if (turret != null)
            turret.SetCombatActive(false, null);
    }
}
