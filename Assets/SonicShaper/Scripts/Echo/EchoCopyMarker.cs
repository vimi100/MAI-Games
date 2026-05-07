using UnityEngine;
using System;
using System.Collections;

public class EchoCopyMarker : MonoBehaviour
{
    public GameObject SourceObject { get; private set; }
    public float Lifetime { get; private set; }

    private Action<EchoCopyMarker> _onExpired;
    private bool _expiredCallbackInvoked = false;

    public void Initialize(GameObject sourceObject, float lifetime, Action<EchoCopyMarker> onExpired)
    {
        SourceObject = sourceObject;
        Lifetime = Mathf.Max(0.1f, lifetime);
        _onExpired = onExpired;
        StartCoroutine(LifetimeCoroutine());
    }

    IEnumerator LifetimeCoroutine()
    {
        yield return new WaitForSeconds(Lifetime);
        NotifyExpired();
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        NotifyExpired();
    }

    void NotifyExpired()
    {
        if (_expiredCallbackInvoked) return;
        _expiredCallbackInvoked = true;
        _onExpired?.Invoke(this);
    }
}
