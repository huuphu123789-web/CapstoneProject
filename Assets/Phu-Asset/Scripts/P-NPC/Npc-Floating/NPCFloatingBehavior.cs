using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Hành vi NPC Lơ Lửng (Floating Mutant):
/// Kế thừa từ NPCBaseBehavior. Khi đến bốt gác kiểm tra, cơ thể sẽ bay lơ lửng nhấp nhô trên không.
/// Khi bị Deny: bay lơ lửng vòng vòng một chút rồi bay vút thẳng lên trời biến mất.
/// </summary>
public class NPCFloatingBehavior : NPCBaseBehavior
{
    [Header("=== Cấu Hình Lơ Lửng ===")]
    public float floatHeight = 0.6f;  // Chiều cao bay lên
    public float floatSpeed = 2.5f;   // Tốc độ nhấp nhô lơ lửng
    public AudioClip floatHumSound;   // Tiếng vi vu/u u lơ lửng

    [Header("=== Hiệu Ứng Bay Khi Bị Deny ===")]
    [Tooltip("Thời gian bay lơ lửng vòng vòng trước khi bay lên trời (giây)")]
    public float circleDuration = 3.0f;

    [Tooltip("Bán kính vòng bay (mét)")]
    public float circleRadius = 2.6f;

    [Tooltip("Số vòng bay trong khoảng thời gian bay vòng")]
    public float circleTurns = 1.25f;

    [Tooltip("Thời gian bay vút thẳng lên trời (giây)")]
    public float ascendDuration = 2.5f;

    [Tooltip("Độ cao bay lên trời trước khi biến mất (mét)")]
    public float ascendHeight = 35f;

    private Vector3 originalLocalPos;
    private float soundTimer;
    private bool isFlyingAway = false;

    protected override void Start()
    {
        base.Start();
        originalLocalPos = transform.localPosition;
    }

    protected override void OnInspectionStarted()
    {
        base.OnInspectionStarted();
        originalLocalPos = transform.localPosition;
    }

    protected override void Update()
    {
        base.Update();

        // KHI ĐANG KIỂM DUYỆT -> PHÁT TIẾNG LƠ LỬNG
        if ((isInspecting || isFlyingAway) && floatHumSound != null)
        {
            soundTimer -= Time.deltaTime;
            if (soundTimer <= 0)
            {
                PlaySound(floatHumSound);
                soundTimer = 1.5f;
            }
        }
    }

    void LateUpdate()
    {
        // KHI ĐANG KIỂM DUYỆT (và chưa bay đi) -> BAY LƠ LỬNG NHẤP NHÔ
        if (isInspecting && !isFlyingAway)
        {
            float newY = Mathf.Sin(Time.time * floatSpeed) * 0.25f + floatHeight;
            transform.localPosition = originalLocalPos + new Vector3(0f, newY, 0f);
        }
    }

    /// <summary>
    /// Kích hoạt hiệu ứng bay vòng vòng một chút rồi bay thẳng lên trời biến mất khi bị Deny
    /// </summary>
    public void TriggerDenyFlyAway(Action onFinished)
    {
        if (isFlyingAway) return;
        isFlyingAway = true;
        isInspecting = false;

        StartCoroutine(DenyFlyAwayRoutine(onFinished));
    }

    private IEnumerator DenyFlyAwayRoutine(Action onFinished)
    {
        Transform moveRoot = transform.root != null ? transform.root : transform;

        // Tắt NavMeshAgent để NPC bay tự do trên không không bị kéo dính mặt đất
        NavMeshAgent[] agents = moveRoot.GetComponentsInChildren<NavMeshAgent>(true);
        for (int i = 0; i < agents.Length; i++)
        {
            if (agents[i] != null) agents[i].enabled = false;
        }

        Animator anim = moveRoot.GetComponentInChildren<Animator>();
        if (anim != null) anim.SetBool("isWalk", false);

        Vector3 startPos = moveRoot.position;
        Vector3 orbitCenter = startPos - moveRoot.forward * (circleRadius * 0.6f);
        Vector3 initialOffset = startPos - orbitCenter;
        float startAngle = Mathf.Atan2(initialOffset.z, initialOffset.x);
        float startRadius = new Vector2(initialOffset.x, initialOffset.z).magnitude;

        // Giai đoạn 1: Bay lơ lửng vòng vòng và nâng dần độ cao
        float elapsed = 0f;
        while (elapsed < circleDuration)
        {
            float t = elapsed / Mathf.Max(0.1f, circleDuration);
            float currentRadius = Mathf.Lerp(startRadius, circleRadius, Mathf.SmoothStep(0f, 1f, t * 2f));
            float angle = startAngle + t * circleTurns * Mathf.PI * 2f;

            float x = orbitCenter.x + Mathf.Cos(angle) * currentRadius;
            float z = orbitCenter.z + Mathf.Sin(angle) * currentRadius;
            float hoverY = startPos.y + Mathf.Lerp(0.4f, 2.2f, t) + Mathf.Sin(elapsed * floatSpeed * 1.4f) * 0.35f;

            Vector3 nextPos = new Vector3(x, hoverY, z);
            Vector3 moveDir = nextPos - moveRoot.position;
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized);
                moveRoot.rotation = Quaternion.Slerp(moveRoot.rotation, targetRot, Time.deltaTime * 6f);
            }

            moveRoot.position = nextPos;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Giai đoạn 2: Bay vút thẳng lên trời rồi biến mất
        Vector3 ascendStartPos = moveRoot.position;
        Vector3 initialScale = moveRoot.localScale;
        elapsed = 0f;

        while (elapsed < ascendDuration)
        {
            float t = elapsed / Mathf.Max(0.1f, ascendDuration);
            float easeIn = t * t; // Tăng tốc dần khi lao lên trời

            Vector3 pos = ascendStartPos + Vector3.up * (ascendHeight * easeIn);
            moveRoot.position = pos;
            moveRoot.Rotate(Vector3.up, 240f * Time.deltaTime, Space.World);

            // Thu nhỏ dần ở 30% cuối để tan biến vào bầu trời đêm
            if (t > 0.7f)
            {
                float fadeT = (t - 0.7f) / 0.3f;
                moveRoot.localScale = Vector3.Lerp(initialScale, Vector3.zero, fadeT);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        onFinished?.Invoke();
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX(clip);
        else if (audioSource != null) audioSource.PlayOneShot(clip, volume);
    }
}