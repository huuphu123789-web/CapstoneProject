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

    [Header("=== TELEKINETIC REACH & THROW PLAYER ===")]
    [Tooltip("Bật để khi GIRAR tới điểm kiểm tra sẽ đưa 2 tay về phía Player rồi ném Player bay ra xa")]
    public bool enableThrowPlayer = true;

    [Tooltip("Điểm đáp khi GIRAR ném Player (tự động tìm 'throw point' / 'ThrowPoint' trong Scene nếu để trống)")]
    public Transform throwPoint;

    [Tooltip("Điểm trong bốt gác để phát hiện Player vừa quay lại lần 2 (tự động tìm 'booth point' / 'BoothPoint' nếu để trống)")]
    public Transform boothPoint;

    [Tooltip("Số lần ném tối đa (mặc định 2: ném lần 1 lúc tới, ném lần 2 khi Player vừa chạy quay lại booth point!)")]
    public int maxThrowCount = 2;

    [Tooltip("Bán kính quanh booth point kích hoạt cú ném lần 2 khi Player quay lại (mét)")]
    public float boothReturnTriggerRadius = 2.8f;

    [Tooltip("Thời gian chờ sau khi tới vạch kiểm tra trước khi đưa tay ra (giây)")]
    public float reachDelay = 1.6f;

    [Tooltip("Thời gian vươn 2 tay về phía Player trước khi hất văng (giây)")]
    public float reachDuration = 1.0f;

    [Tooltip("Khoảng cách ném Player bay ra xa nếu chưa đặt Throw Point (mét)")]
    public float throwDistance = 18f;

    [Tooltip("Độ cao vòng cung khi Player bị hất tung lên không (mét)")]
    public float throwArcHeight = 5.5f;

    [Tooltip("Thời gian Player bay trên không trước khi tiếp đất (giây)")]
    public float throwFlightDuration = 1.15f;

    [Header("=== BONE & BODY DISTORTION ===")]
    [Tooltip("Head or jaw bone that vibrates violently during screaming")]
    public Transform headBone;
    public Transform neckBone;
    public Transform spineBone;
    public Transform leftArmBone;
    public Transform rightArmBone;
    public Transform leftForeArmBone;
    public Transform rightForeArmBone;
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
    [Tooltip("Độ giật xoay/lệch góc nhìn Cinemachine khi bị hét hoặc bị ném (độ)")]
    public float cameraJoltDegrees = 18f;
    [Tooltip("Góc nghiêng chéo tối đa của màn hình (Dutch Angle) sau khi bị rung giật/ném (độ)")]
    public float dutchTiltDegrees = 28f;
    [Tooltip("Thời gian gây choáng váng đảo nhiều góc nghiêng + giật camera khi chạy về bốt gác (giây)")]
    public float dutchHoldDuration = 9.0f;
    [Tooltip("Thời gian từ từ trả màn hình về thẳng (giây)")]
    public float dutchRecoverDuration = 2.0f;
    [Tooltip("Hệ số tăng độ rung, độ giật và độ nghiêng xéo ở lần bị quăng thứ 2 (mặc định 1.65 = mạnh hơn 65%)")]
    public float secondThrowIntensityMultiplier = 1.65f;

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

    // Trạng thái đưa tay & ném Player
    private float armReachWeight = 0f;
    private float armThrowSwing = 0f;
    private int throwCount = 0;
    private bool isThrowingNow = false;
    private Transform cachedPlayerTarget;
    private Unity.Cinemachine.CinemachinePanTilt cachedPanTilt;
    private Unity.Cinemachine.CinemachineCamera cachedCinemachineCam;
    private Coroutine dutchRecoverCoroutine;

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
        FindThrowAndBoothPoints();

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
            else if (leftArmBone == null && (n.Contains("leftarm") || n.Contains("upperarm_l") || n.Contains("leftupperarm") || n.EndsWith("arm_l")))
            {
                leftArmBone = t;
            }
            else if (rightArmBone == null && (n.Contains("rightarm") || n.Contains("upperarm_r") || n.Contains("rightupperarm") || n.EndsWith("arm_r")))
            {
                rightArmBone = t;
            }
            else if (leftForeArmBone == null && (n.Contains("leftforearm") || n.Contains("lowerarm_l") || n.Contains("leftlowerarm")))
            {
                leftForeArmBone = t;
            }
            else if (rightForeArmBone == null && (n.Contains("rightforearm") || n.Contains("lowerarm_r") || n.Contains("rightlowerarm")))
            {
                rightForeArmBone = t;
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

        Vector3 boothCenter = transform.position;
        if (Camera.main != null) boothCenter = Camera.main.transform.position;
        else
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) boothCenter = playerObj.transform.position;
        }
        Light[] allLights = FindObjectsByType<Light>();
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

    private static string NormalizeName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        return raw.Trim().Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
    }

    private void FindThrowAndBoothPoints()
    {
        if (throwPoint != null && boothPoint != null) return;

        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null) continue;
            string n = NormalizeName(t.name);

            if (throwPoint == null && (n == "throwpoint" || n == "npcthrowpoint" || n == "playerthrowpoint"))
            {
                throwPoint = t;
            }
            else if (boothPoint == null && (n == "boothpoint" || n == "playerboothteleportpoint" || n == "guardboothpoint"))
            {
                boothPoint = t;
            }
        }
    }

    private IEnumerator ScreamLoopSequence()
    {
        FindThrowAndBoothPoints();

        // Lần ném 1: Ngay khi GIRAR tới điểm kiểm tra -> đưa tay quăng Player ra Throw Point!
        if (enableThrowPlayer && throwCount < maxThrowCount)
        {
            yield return new WaitForSeconds(reachDelay);
            if (isInspecting)
            {
                yield return StartCoroutine(ExecuteThrowPlayerOnce(reachDuration));
            }
        }
        else
        {
            yield return new WaitForSeconds(initialScreamDelay);
        }

        // Theo dõi Player chạy quay lại Booth Point -> Quăng ra Throw Point thêm 1 lần nữa!
        while (isInspecting && enableThrowPlayer && throwCount < maxThrowCount)
        {
            Transform playerRoot = GetPlayerRootTransform();
            Vector3 boothPos = boothPoint != null ? boothPoint.position : (transform.position + transform.forward * 2f);

            if (playerRoot != null)
            {
                float flatDistToBooth = Vector2.Distance(
                    new Vector2(playerRoot.position.x, playerRoot.position.z),
                    new Vector2(boothPos.x, boothPos.z)
                );

                if (flatDistToBooth <= boothReturnTriggerRadius)
                {
                    // Vừa lết về tới booth point -> GIRAR vươn tay quăng tiếp ra throw point lần 2!
                    yield return StartCoroutine(ExecuteThrowPlayerOnce(Mathf.Min(0.55f, reachDuration)));
                    continue;
                }
            }

            yield return new WaitForSeconds(0.15f);
        }

        while (isInspecting)
        {
            yield return StartCoroutine(PerformScream());

            if (!loopScreams) break;

            yield return new WaitForSeconds(screamInterval);
        }
    }

    private Transform GetPlayerRootTransform()
    {
        // Trả về đúng Transform chứa PlayerController / CharacterController (PlayerModel - object thực sự di chuyển!)
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) return pc.transform;

        GameObject playerObj = GameObject.FindWithTag("Player");
        return playerObj != null ? playerObj.transform : (Camera.main != null ? Camera.main.transform : null);
    }

    private IEnumerator ExecuteThrowPlayerOnce(float currentReachDuration)
    {
        if (isThrowingNow) yield break;
        isThrowingNow = true;
        throwCount++;
        if (!isInspecting) { isThrowingNow = false; yield break; }

        FindThrowAndBoothPoints();

        GameObject playerObj = GameObject.FindWithTag("Player");
        PlayerController pc = playerObj != null ? playerObj.GetComponentInChildren<PlayerController>() : FindAnyObjectByType<PlayerController>();
        Transform playerRoot = GetPlayerRootTransform();
        cachedPlayerTarget = Camera.main != null ? Camera.main.transform : playerRoot;

        if (playerRoot == null) { isThrowingNow = false; yield break; }

        // 1. Đưa 2 tay vươn thẳng về phía Player kèm tiếng rít cảnh báo
        if (radioStaticSound != null) PlaySound(radioStaticSound, 0.85f);

        CameraShake shaker = CameraShake.instance ?? FindAnyObjectByType<CameraShake>();
        if (shaker == null && Camera.main != null) shaker = Camera.main.gameObject.AddComponent<CameraShake>();

        // Hệ số cường độ: lần quăng thứ 2 mạnh và điên cuồng hơn hẳn lần 1!
        float mult = (throwCount >= 2) ? Mathf.Max(1.0f, secondThrowIntensityMultiplier) : 1.0f;
        float curShake = cameraShakeIntensity * mult;
        float curJolt = cameraJoltDegrees * mult;
        float curDutch = Mathf.Clamp(dutchTiltDegrees * mult, 10f, 48f);

        float elapsed = 0f;
        while (elapsed < currentReachDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, currentReachDuration));
            armReachWeight = Mathf.SmoothStep(0f, 1f, t);

            if (shaker != null) shaker.Shake(0.08f, curShake * 0.45f * t);
            JoltCinemachineCamera(
                Mathf.Sin(Time.time * 48f) * curJolt * 0.14f * t,
                Mathf.Cos(Time.time * 55f) * curJolt * 0.12f * t
            );
            yield return null;
        }
        armReachWeight = 1f;

        // 2. Vung tay hất mạnh + phát tiếng thét -> Ném Player bay vòng cung thẳng tới Throw Point!
        isScreaming = true;
        PlaySound(screamShriekSound, 1.0f);
        if (shaker != null) shaker.Shake(0.45f, curShake * 1.9f);

        float spinDir = (Random.value > 0.5f) ? 1f : -1f;
        JoltCinemachineCamera(spinDir * curJolt * 1.5f, -curJolt * 0.9f);

        Vector3 startPos = playerRoot.position;
        Vector3 landingPos;
        if (throwPoint != null)
        {
            landingPos = FindSafeGroundPosition(throwPoint.position, throwPoint.position.y);
        }
        else
        {
            Vector3 awayDir = startPos - transform.position;
            awayDir.y = 0f;
            if (awayDir.sqrMagnitude < 0.01f) awayDir = transform.forward;
            awayDir.Normalize();
            landingPos = FindSafeGroundPosition(startPos + awayDir * throwDistance, startPos.y);
        }

        // Tạm tắt PlayerController và CharacterController khi đang bay vòng cung để không bị kẹt tường bốt gác
        CharacterController cc = pc != null ? pc.GetComponent<CharacterController>() : playerRoot.GetComponentInChildren<CharacterController>();
        if (pc != null) pc.enabled = false;
        if (cc != null) cc.enabled = false;

        elapsed = 0f;
        float flightTime = Mathf.Max(0.4f, throwFlightDuration);
        while (elapsed < flightTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightTime);

            armThrowSwing = (t < 0.35f) ? Mathf.Sin((t / 0.35f) * Mathf.PI * 0.5f) : Mathf.Lerp(1f, 0f, (t - 0.35f) / 0.65f);
            if (t > 0.6f) armReachWeight = Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);

            Vector3 flatPos = Vector3.Lerp(startPos, landingPos, t);
            float arcY = Mathf.Sin(t * Mathf.PI) * throwArcHeight;
            playerRoot.position = new Vector3(flatPos.x, Mathf.Lerp(startPos.y, landingPos.y, t) + arcY, flatPos.z);

            float flightIntensity = 1f - (t * 0.35f);
            JoltCinemachineCamera(
                (spinDir * 105f * mult + Mathf.Sin(Time.time * 42f * mult) * curJolt * 6.5f) * Time.deltaTime * flightIntensity,
                (Mathf.Cos(Time.time * 38f * mult) * curJolt * 5.5f) * Time.deltaTime * flightIntensity
            );

            if (shaker != null && t < 0.65f) shaker.Shake(0.08f, curShake * 1.0f);
            yield return null;
        }

        // Tiếp đất chuẩn tại Throw Point + choáng váng đảo nhiều góc nghiêng & giật camera (lần 2 mạnh hơn hẳn!)
        playerRoot.position = landingPos;
        JoltCinemachineCamera(Random.Range(-curJolt * 1.6f, curJolt * 1.6f), Random.Range(14f, 26f) * mult);
        TriggerDutchTilt(spinDir * curDutch);
        if (cc != null) cc.enabled = true;
        if (pc != null) pc.enabled = true;

        if (earRingingSound != null) PlaySound(earRingingSound, 0.7f);
        if (shaker != null) shaker.Shake(0.3f, curShake * 1.25f);

        armReachWeight = 0f;
        armThrowSwing = 0f;
        isScreaming = false;
        isThrowingNow = false;

        Debug.Log($"🖐️💥 [NPCScreamerBehavior] GIRAR đã quăng Player lần {throwCount}/{maxThrowCount} tới {landingPos}!");
    }

    /// <summary>
    /// Tác động trực tiếp lên CinemachinePanTilt đang hoạt động trên Player để rung lắc dữ dội và hất lệch hướng nhìn
    /// </summary>
    private void JoltCinemachineCamera(float panDelta, float tiltDelta)
    {
        if (cachedPanTilt == null || !cachedPanTilt.isActiveAndEnabled)
        {
            cachedPanTilt = null;
            Unity.Cinemachine.CinemachinePanTilt[] allPT = FindObjectsByType<Unity.Cinemachine.CinemachinePanTilt>(FindObjectsInactive.Exclude);
            for (int i = 0; i < allPT.Length; i++)
            {
                if (allPT[i] != null && allPT[i].isActiveAndEnabled)
                {
                    cachedPanTilt = allPT[i];
                    break;
                }
            }
        }

        if (cachedPanTilt != null)
        {
            cachedPanTilt.PanAxis.Value += panDelta;
            cachedPanTilt.TiltAxis.Value = Mathf.Clamp(cachedPanTilt.TiltAxis.Value + tiltDelta, -70f, 70f);
        }
        else if (Camera.main != null)
        {
            Camera.main.transform.Rotate(tiltDelta, panDelta, 0f);
        }
    }

    private void SetCinemachineDutch(float dutchAngle)
    {
        bool applied = false;

        // Ưu tiên đúng Virtual Camera đang Live trên CinemachineBrain
        if (Camera.main != null)
        {
            var brain = Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            if (brain != null && brain.ActiveVirtualCamera is Unity.Cinemachine.CinemachineCamera liveCam)
            {
                var lens = liveCam.Lens;
                lens.Dutch = dutchAngle;
                liveCam.Lens = lens;
                cachedCinemachineCam = liveCam;
                applied = true;
            }
        }

        if (!applied)
        {
            Unity.Cinemachine.CinemachineCamera[] activeCams = FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsInactive.Exclude);
            for (int i = 0; i < activeCams.Length; i++)
            {
                if (activeCams[i] != null && activeCams[i].isActiveAndEnabled)
                {
                    var lens = activeCams[i].Lens;
                    lens.Dutch = dutchAngle;
                    activeCams[i].Lens = lens;
                    cachedCinemachineCam = activeCams[i];
                }
            }
        }
    }

    private void TriggerDutchTilt(float targetAngle)
    {
        if (dutchRecoverCoroutine != null) StopCoroutine(dutchRecoverCoroutine);
        dutchRecoverCoroutine = StartCoroutine(DutchTiltRoutine(targetAngle));
    }

    private IEnumerator DutchTiltRoutine(float initialAngle)
    {
        float currentDutch = cachedCinemachineCam != null ? cachedCinemachineCam.Lens.Dutch : 0f;
        float targetDutch = initialAngle;
        float nextAngleSwapTimer = 0.65f;
        float nextJoltTimer = 0.4f;
        float sideSign = (initialAngle >= 0f) ? 1f : -1f;

        // Giữ hiệu ứng choáng váng (đảo nhiều góc nghiêng + giật camera liên tục) SUỐT QUÁ TRÌNH chạy về bốt gác
        // cho đến khi người chơi bấm được Deny (hoặc hết GIRAR)
        while (isInspecting && (NPCInspectionManager.instance == null || !NPCInspectionManager.instance.hasDecisionBeenMade))
        {
            // Tạm nhường nếu GIRAR đang thực hiện cú quăng lần 2
            if (!isThrowingNow)
            {
                float mult = (throwCount >= 2) ? Mathf.Max(1.0f, secondThrowIntensityMultiplier) : 1.0f;
                float curDutchMax = Mathf.Clamp(dutchTiltDegrees * mult, 10f, 48f);
                float curJolt = cameraJoltDegrees * mult;

                nextAngleSwapTimer -= Time.deltaTime;
                nextJoltTimer -= Time.deltaTime;

                if (nextAngleSwapTimer <= 0f)
                {
                    sideSign = -sideSign;
                    float magnitude = Random.Range(curDutchMax * 0.65f, curDutchMax * 1.05f);
                    targetDutch = sideSign * magnitude;
                    nextAngleSwapTimer = (throwCount >= 2) ? Random.Range(0.32f, 0.72f) : Random.Range(0.55f, 1.2f);
                }

                if (nextJoltTimer <= 0f)
                {
                    float panKick = Random.Range(-curJolt * 1.45f, curJolt * 1.45f);
                    float tiltKick = Random.Range(-curJolt * 0.9f, curJolt * 0.9f);
                    JoltCinemachineCamera(panKick, tiltKick);
                    nextJoltTimer = (throwCount >= 2) ? Random.Range(0.22f, 0.55f) : Random.Range(0.35f, 0.85f);
                }

                float microSway = Mathf.Sin(Time.time * (16f * mult)) * (3.8f * mult);
                currentDutch = Mathf.Lerp(currentDutch, targetDutch, Time.deltaTime * (6.5f * mult));
                SetCinemachineDutch(currentDutch + microSway);
            }

            yield return null;
        }

        // Từ từ trả màn hình về thẳng (0 độ) sau khi đã bấm Deny / xử lý xong GIRAR
        float startRecover = cachedCinemachineCam != null ? cachedCinemachineCam.Lens.Dutch : 0f;
        float t = 0f;
        float recoverTime = Mathf.Max(0.2f, dutchRecoverDuration);
        while (t < recoverTime)
        {
            t += Time.deltaTime;
            float smooth = Mathf.SmoothStep(0f, 1f, t / recoverTime);
            SetCinemachineDutch(Mathf.Lerp(startRecover, 0f, smooth));
            yield return null;
        }

        SetCinemachineDutch(0f);
        dutchRecoverCoroutine = null;
    }

    private Vector3 FindSafeGroundPosition(Vector3 targetPos, float fallbackY)
    {
        Ray ray = new Ray(new Vector3(targetPos.x, fallbackY + 15f, targetPos.z), Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, 40f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float bestDist = float.MaxValue;
        float bestY = fallbackY;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i].collider;
            if (c == null) continue;
            if (c.CompareTag("Player") || c.CompareTag("NPC")) continue;
            if (hits[i].distance < bestDist)
            {
                bestDist = hits[i].distance;
                bestY = hits[i].point.y;
            }
        }
        return new Vector3(targetPos.x, bestY + 0.1f, targetPos.z);
    }

    private IEnumerator PerformScream()
    {
        isScreaming = true;
        baseInspectionPos = transform.position;

        // Play screech sound and static noise
        PlaySound(screamShriekSound, 1.0f);
        if (radioStaticSound != null) PlaySound(radioStaticSound, 0.75f);

        CameraShake shaker = CameraShake.instance ?? FindAnyObjectByType<CameraShake>();
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

            // Vibrate & knock Cinemachine camera angle off-center violently
            if (shaker != null)
            {
                shaker.Shake(0.12f, cameraShakeIntensity);
            }
            JoltCinemachineCamera(
                Mathf.Sin(Time.time * 46f) * cameraJoltDegrees * 4.5f * Time.deltaTime + Random.Range(-18f, 18f) * Time.deltaTime,
                Mathf.Cos(Time.time * 52f) * cameraJoltDegrees * 3.5f * Time.deltaTime + Random.Range(-14f, 14f) * Time.deltaTime
            );

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

        // Post-scream high-pitch ringing + giữ góc nhìn nghiêng xéo sau cú hét
        if (earRingingSound != null)
        {
            PlaySound(earRingingSound, 0.6f);
        }
        TriggerDutchTilt((Random.value > 0.5f ? 1f : -1f) * dutchTiltDegrees * 0.85f);

        transform.position = baseInspectionPos;
        transform.localScale = originalLocalScale;
        armReachWeight = 0f;
        armThrowSwing = 0f;
        isScreaming = false;

        // Đảm bảo trả lại quyền điều khiển cho Player nếu GIRAR bị tắt giữa lúc đang ném
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            if (!pc.enabled) pc.enabled = true;
            CharacterController cc = pc.GetComponent<CharacterController>();
            if (cc != null && !cc.enabled) cc.enabled = true;
        }
    }

    void LateUpdate()
    {
        // Hiệu ứng giơ 2 tay vươn thẳng về phía Player rồi vung tay hất ném ra xa
        if (armReachWeight > 0.001f)
        {
            Vector3 targetPos = cachedPlayerTarget != null
                ? cachedPlayerTarget.position
                : (transform.position + transform.forward * 2f + Vector3.up * 1.4f);

            // Khi vung tay ném (armThrowSwing > 0), hất 2 tay lên cao và sang ngang mạnh
            Vector3 swingOffset = Vector3.up * (armThrowSwing * 2.2f) + transform.right * (armThrowSwing * 0.9f);
            AimArmBoneAt(leftArmBone, targetPos + swingOffset, armReachWeight);
            AimArmBoneAt(rightArmBone, targetPos + swingOffset, armReachWeight);

            if (leftForeArmBone != null)
            {
                leftForeArmBone.localRotation = Quaternion.Slerp(leftForeArmBone.localRotation, Quaternion.identity, armReachWeight * 0.7f);
            }
            if (rightForeArmBone != null)
            {
                rightForeArmBone.localRotation = Quaternion.Slerp(rightForeArmBone.localRotation, Quaternion.identity, armReachWeight * 0.7f);
            }
        }

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

    private void AimArmBoneAt(Transform armBone, Vector3 worldTarget, float weight)
    {
        if (armBone == null) return;
        Vector3 toTarget = worldTarget - armBone.position;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        // Dùng hướng từ vai tới khuỷu tay con (hoặc armBone.up) để xoay xương cánh tay chỉ thẳng vào mục tiêu
        Vector3 boneAxis = armBone.childCount > 0
            ? (armBone.GetChild(0).position - armBone.position).normalized
            : armBone.up;

        if (boneAxis.sqrMagnitude < 0.0001f) return;

        Quaternion delta = Quaternion.FromToRotation(boneAxis, toTarget.normalized);
        Quaternion targetWorldRot = delta * armBone.rotation;
        armBone.rotation = Quaternion.Slerp(armBone.rotation, targetWorldRot, weight);
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

        if (dutchRecoverCoroutine != null)
        {
            StopCoroutine(dutchRecoverCoroutine);
            dutchRecoverCoroutine = null;
        }
        SetCinemachineDutch(0f);

        if (boothLight != null)
        {
            boothLight.intensity = defaultLightIntensity;
        }

        transform.localScale = originalLocalScale;
        armReachWeight = 0f;
        armThrowSwing = 0f;
        isScreaming = false;

        // Đảm bảo trả lại quyền điều khiển cho Player nếu GIRAR bị tắt giữa lúc đang ném
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            if (!pc.enabled) pc.enabled = true;
            CharacterController cc = pc.GetComponent<CharacterController>();
            if (cc != null && !cc.enabled) cc.enabled = true;
        }
    }
}
