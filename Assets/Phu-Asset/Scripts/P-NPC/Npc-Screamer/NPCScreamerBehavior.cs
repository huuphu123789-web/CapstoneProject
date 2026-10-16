using System.Collections;
using UnityEngine;

/// <summary>
/// Hành vi NPC La Hét (Screamer Mutant):
/// Kế thừa từ NPCBaseBehavior. Phát ra sóng âm la hét chói tai, làm chập chờn đèn bốt gác, rung lắc camera và co giật thân thể.
/// </summary>
public class NPCScreamerBehavior : NPCBaseBehavior
{
    [Header("=== SCREAM SETTINGS ===")]
    public float initialScreamDelay = 1.8f;
    public float screamDuration = 3.2f;
    public float screamInterval = 5.5f;
    public bool loopScreams = true;

    [Header("=== BONE & BODY DISTORTION ===")]
    [Tooltip("Head or jaw bone that vibrates violently during screaming")]
    public Transform headBone;
    public Transform neckBone;
    public Transform spineBone;
    public float headShakeFrequency = 42f;
    public float headShakeMagnitude = 22f;
    public float bodyJitterMagnitude = 0.035f;

    [Header("=== ENVIRONMENTAL DISRUPTION ===")]
    [Tooltip("Guard booth light to flicker (auto-detected if empty)")]
    public Light boothLight;
    public float flickerSpeed = 35f;
    public float minFlickerIntensity = 0.05f;
    public float maxFlickerIntensity = 1.6f;

    [Header("=== CAMERA SHAKE ===")]
    public float cameraShakeIntensity = 0.25f;

    [Header("=== AUDIO EFFECTS ===")]
    public AudioClip screamShriekSound;
    public AudioClip radioStaticSound;
    public AudioClip earRingingSound;

    [Header("=== STATE ===")]
    public bool isScreaming = false;

    private Quaternion originalHeadRot;
    private Quaternion originalNeckRot;
    private Quaternion originalSpineRot;
    private Vector3 originalLocalScale;
    private Vector3 baseInspectionPos;
    private float defaultLightIntensity = 1f;
    private Coroutine screamLoopCoroutine;

    protected override void Awake()
    {
        base.Awake();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 35f;
        }

        originalLocalScale = transform.localScale;

        AutoFindBones();
        FindBoothLight();

#if UNITY_EDITOR
        if (screamShriekSound == null)
            screamShriekSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Dark/Drn_Autonomy_01.wav");
        if (radioStaticSound == null)
            radioStaticSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Dark/Drn_Abandoned_Lab_01.wav");
        if (earRingingSound == null)
            earRingingSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Dark/Drn_Alien_01.wav");
#endif
    }

    private void AutoFindBones()
    {
        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        foreach (Transform t in allTransforms)
        {
            string n = t.name.ToLower();
            if (headBone == null && n.Contains("head"))
            {
                headBone = t;
                originalHeadRot = t.localRotation;
            }
            else if (neckBone == null && n.Contains("neck"))
            {
                neckBone = t;
                originalNeckRot = t.localRotation;
            }
            else if (spineBone == null && (n.Equals("spine") || n.Contains("spine1") || n.Contains("chest")))
            {
                spineBone = t;
                originalSpineRot = t.localRotation;
            }
        }

        if (headBone != null && originalHeadRot == default) originalHeadRot = headBone.localRotation;
        if (neckBone != null && originalNeckRot == default) originalNeckRot = neckBone.localRotation;
        if (spineBone != null && originalSpineRot == default) originalSpineRot = spineBone.localRotation;
    }

    private void FindBoothLight()
    {
        if (boothLight != null)
        {
            defaultLightIntensity = boothLight.intensity;
            return;
        }

        Vector3 boothCenter = new Vector3(-51.17f, 0f, -50.18f);
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        Light closestLight = null;
        float closestDist = float.MaxValue;

        foreach (var l in allLights)
        {
            if (l.type == LightType.Directional) continue;

            float d = Vector3.Distance(l.transform.position, boothCenter);
            if (d < closestDist && d < 20f)
            {
                closestDist = d;
                closestLight = l;
            }
        }

        if (closestLight != null)
        {
            boothLight = closestLight;
            defaultLightIntensity = boothLight.intensity;
        }
        else if (allLights.Length > 0)
        {
            foreach (var l in allLights)
            {
                if (l.type != LightType.Directional)
                {
                    boothLight = l;
                    defaultLightIntensity = boothLight.intensity;
                    break;
                }
            }
        }
    }

    public override void MoveToInspectionPoint()
    {
        base.MoveToInspectionPoint();
        isScreaming = false;
    }

    protected override void OnInspectionStarted()
    {
        base.OnInspectionStarted();
        baseInspectionPos = transform.position;

        if (screamLoopCoroutine != null) StopCoroutine(screamLoopCoroutine);
        screamLoopCoroutine = StartCoroutine(ScreamLoopSequence());
    }

    private IEnumerator ScreamLoopSequence()
    {
        yield return new WaitForSeconds(initialScreamDelay);

        while (isInspecting)
        {
            yield return StartCoroutine(PerformScream());

            if (!loopScreams) break;

            yield return new WaitForSeconds(screamInterval);
        }
    }

    private IEnumerator PerformScream()
    {
        isScreaming = true;
        baseInspectionPos = transform.position;

        // Play screech sound and static noise
        PlaySound(screamShriekSound, 1.0f);
        if (radioStaticSound != null) PlaySound(radioStaticSound, 0.75f);

        CameraShake shaker = CameraShake.instance ?? FindFirstObjectByType<CameraShake>();
        if (shaker == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) shaker = player.AddComponent<CameraShake>();
            else if (Camera.main != null) shaker = Camera.main.gameObject.AddComponent<CameraShake>();
        }

        float elapsed = 0f;
        while (elapsed < screamDuration)
        {
            elapsed += Time.deltaTime;

            // Vibrate camera violently
            if (shaker != null)
            {
                shaker.Shake(0.12f, cameraShakeIntensity);
            }

            // Strobe flicker booth light
            if (boothLight != null)
            {
                float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
                boothLight.intensity = Mathf.Lerp(minFlickerIntensity, maxFlickerIntensity, noise);
            }

            yield return null;
        }

        // Restore light to normal
        if (boothLight != null)
        {
            boothLight.intensity = defaultLightIntensity;
        }

        // Post-scream high-pitch ringing
        if (earRingingSound != null)
        {
            PlaySound(earRingingSound, 0.6f);
        }

        transform.position = baseInspectionPos;
        transform.localScale = originalLocalScale;
        isScreaming = false;
    }

    void LateUpdate()
    {
        if (isScreaming)
        {
            // Violent head vibrating & spasming
            float shakeX = Mathf.Sin(Time.time * headShakeFrequency) * headShakeMagnitude;
            float shakeY = Mathf.Cos(Time.time * (headShakeFrequency * 1.35f)) * headShakeMagnitude;
            float shakeZ = Mathf.Sin(Time.time * (headShakeFrequency * 0.8f)) * (headShakeMagnitude * 0.6f);

            if (headBone != null)
            {
                headBone.localRotation = originalHeadRot * Quaternion.Euler(shakeX, shakeY, shakeZ);
            }

            if (neckBone != null)
            {
                neckBone.localRotation = originalNeckRot * Quaternion.Euler(shakeX * 0.4f, shakeY * 0.4f, 0f);
            }

            if (spineBone != null)
            {
                spineBone.localRotation = originalSpineRot * Quaternion.Euler(shakeX * 0.2f, 0f, shakeY * 0.2f);
            }

            // Violent full-body jitter
            if (bodyJitterMagnitude > 0f)
            {
                transform.position = baseInspectionPos + Random.insideUnitSphere * bodyJitterMagnitude;
                transform.localScale = originalLocalScale + Random.insideUnitSphere * 0.04f;
            }
        }
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX(clip);
        else if (audioSource != null) audioSource.PlayOneShot(clip, volume);
    }

    void OnDisable()
    {
        if (screamLoopCoroutine != null)
        {
            StopCoroutine(screamLoopCoroutine);
            screamLoopCoroutine = null;
        }

        if (boothLight != null)
        {
            boothLight.intensity = defaultLightIntensity;
        }

        transform.localScale = originalLocalScale;
        isScreaming = false;
    }
}
