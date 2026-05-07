using UnityEngine;
using System.Collections.Generic;

public class EchoDistractionSource : MonoBehaviour
{
    public float sourceRadius = 12f;

    private static readonly List<EchoDistractionSource> ActiveSources = new List<EchoDistractionSource>();

    public static EchoDistractionSource GetNearest(Vector3 position, float maxDistance)
    {
        EchoDistractionSource nearest = null;
        float bestSq = maxDistance * maxDistance;

        for (int i = ActiveSources.Count - 1; i >= 0; i--)
        {
            EchoDistractionSource src = ActiveSources[i];
            if (src == null)
            {
                ActiveSources.RemoveAt(i);
                continue;
            }

            float allowedRange = Mathf.Min(maxDistance, src.sourceRadius);
            float sq = (src.transform.position - position).sqrMagnitude;
            if (sq <= allowedRange * allowedRange && sq < bestSq)
            {
                bestSq = sq;
                nearest = src;
            }
        }

        return nearest;
    }

    void OnEnable()
    {
        if (!ActiveSources.Contains(this))
            ActiveSources.Add(this);
    }

    void OnDisable()
    {
        ActiveSources.Remove(this);
    }
}
