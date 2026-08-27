using System;
using UnityEngine;
using UnityEngine.VFX;

public class VFXEvent : MonoBehaviour
{
    public static VFXEvent Instance { get; private set; }

    [Header("VFX Settings")]
    [SerializeField] private VFXData[] effects;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void Play(string effectName, Vector3 position)
    {
        VFXData effect = FindEffect(effectName);

        if (effect == null)
        {
            Debug.LogWarning(
                "Không tìm thấy VFX: " + effectName
            );

            return;
        }

        if (effect.prefab == null)
        {
            Debug.LogWarning(
                "VFX Prefab chưa được gán: " +
                effectName
            );

            return;
        }

        GameObject vfx = Instantiate(
            effect.prefab,
            position,
            Quaternion.identity
        );

        AutoDestroy(vfx, effect.duration);
    }


    public void Play(
        string effectName,
        Vector3 position,
        Quaternion rotation)
    {
        VFXData effect = FindEffect(effectName);

        if (effect == null)
        {
            Debug.LogWarning(
                "Không tìm thấy VFX: " + effectName
            );

            return;
        }

        if (effect.prefab == null)
        {
            return;
        }

        GameObject vfx = Instantiate(
            effect.prefab,
            position,
            rotation
        );

        AutoDestroy(vfx, effect.duration);
    }


    public void Play(
        string effectName,
        Transform target)
    {
        if (target == null)
            return;

        Play(
            effectName,
            target.position,
            target.rotation
        );
    }


    private VFXData FindEffect(string effectName)
    {
        foreach (VFXData effect in effects)
        {
            if (effect.effectName == effectName)
            {
                return effect;
            }
        }

        return null;
    }


    private void AutoDestroy(
        GameObject vfx,
        float duration)
    {
        if (duration <= 0)
        {
            ParticleSystem particle =
                vfx.GetComponentInChildren<ParticleSystem>();

            if (particle != null)
            {
                duration = particle.main.duration;
            }
            else
            {
                VisualEffect visualEffect =
                    vfx.GetComponentInChildren<VisualEffect>();

                if (visualEffect != null)
                {
                    duration = 2f;
                }
                else
                {
                    duration = 2f;
                }
            }
        }

        Destroy(vfx, duration);
    }
}


[Serializable]
public class VFXData
{
    public string effectName;

    public GameObject prefab;

    [Min(0f)]
    public float duration = 2f;
}