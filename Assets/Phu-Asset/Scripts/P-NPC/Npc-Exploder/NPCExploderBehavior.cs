using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCExploderBehavior : MonoBehaviour
{
    [Header("=== Cấu Hình Tự Đi Bộ ===")]
    public Transform inspectionPoint;
    public float walkSpeed = 2.5f;

    [Header("=== Cấu Hình Nổ Tung ===")]
    public float swellDuration = 2.5f;          // Thời gian phồng to
    public float maxSwellScale = 1.8f;           // Mức độ phồng to
    public GameObject explosionParticlePrefab;   // Particle Nổ

    [Header("=== Âm Thanh & Footstep ===")]
    public AudioClip[] footstepSounds;
    public AudioClip swellSound;                 // Tiếng phồng to/rít
    public AudioClip explosionSound;             // Tiếng NỔ TUNG
    public float footstepInterval = 0.45f;

    [Header("=== Trạng Thái ===")]
    public bool isInspecting = false;
    public bool hasExploded = false;

    [Header("=== References ===")]
    public Animator animator;
    public AudioSource audioSource;

    private NavMeshAgent agent;
    private Vector3 originalScale;
    private float footstepTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        originalScale = transform.localScale;

        if (inspectionPoint == null)
        {
            GameObject zoneObj = GameObject.Find("InspectionZone");
            if (zoneObj != null) inspectionPoint = zoneObj.transform;
        }

        if (inspectionPoint != null) MoveToInspectionPoint();
    }

    public void MoveToInspectionPoint()
    {
        isInspecting = false;
        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.SetDestination(inspectionPoint.position);
        if (animator != null) animator.SetBool("isWalk", true);
    }

    public void StartInspection()
    {
        if (hasExploded) return;
        isInspecting = true;
        agent.isStopped = true;
        if (animator != null) animator.SetBool("isWalk", false);

        StartCoroutine(ExplodeRoutine());
    }

    private IEnumerator ExplodeRoutine()
    {
        PlaySound(swellSound);

        float elapsed = 0f;
        while (elapsed < swellDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / swellDuration;

            // Phồng to dần
            transform.localScale = Vector3.Lerp(originalScale, originalScale * maxSwellScale, progress);
            transform.localPosition += Random.insideUnitSphere * (progress * 0.04f);
            yield return null;
        }

        // --- NỔ TUNG! ---
        hasExploded = true;

        // PHÁT TIẾNG NỔ AN TOÀN (Không bị tắt khi GameObject Ẩn)
        if (explosionSound != null)
        {
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(explosionSound);
            }
            else
            {
                AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1.0f);
            }
        }

        // Tạo Particle
        if (explosionParticlePrefab != null)
        {
            Instantiate(explosionParticlePrefab, transform.position + Vector3.up, Quaternion.identity);
        }

        Debug.Log("💥 NPC ĐÃ NỔ TUNG!");

        // Ẩn NPC
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isInspecting && agent.velocity.magnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0)
            {
                PlayFootstepSound();
                footstepTimer = footstepInterval;
            }
        }

        if (!isInspecting && agent.enabled && agent.hasPath)
        {
            if (agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                StartInspection();
            }
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;
        int randomIndex = Random.Range(0, footstepSounds.Length);
        PlaySound(footstepSounds[randomIndex], 0.6f);
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX(clip);
        else if (audioSource != null) audioSource.PlayOneShot(clip, volume);
    }
}