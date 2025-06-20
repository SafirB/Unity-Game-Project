using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    public static event Action<Vector3, float> NoiseMade; // Guards listen to this event

    private List<NoiseDebug> noiseDebugList = new List<NoiseDebug>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void EmitNoise(Vector3 position, float noiseRadius)
    {
        if (Instance != null)
        {
            Instance.noiseDebugList.Add(new NoiseDebug(position, noiseRadius, Time.time));
            NoiseMade?.Invoke(position, noiseRadius);
        }
    }

    private void OnDrawGizmos()
    {
        if (noiseDebugList == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red

        float timeToDisplay = 1.5f;

        for (int i = noiseDebugList.Count - 1; i >= 0; i--)
        {
            if (Time.time - noiseDebugList[i].timestamp > timeToDisplay)
            {
                noiseDebugList.RemoveAt(i);
            }
            else
            {
                Gizmos.DrawWireSphere(noiseDebugList[i].position, noiseDebugList[i].radius);
            }
        }
    }


    private class NoiseDebug
    {
        public Vector3 position;
        public float radius;
        public float timestamp;

        public NoiseDebug(Vector3 pos, float rad, float time)
        {
            position = pos;
            radius = rad;
            timestamp = time;
        }
    }
}
