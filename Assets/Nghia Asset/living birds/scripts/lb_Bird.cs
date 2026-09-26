using System.Collections;
using UnityEngine;

public class lb_Bird : MonoBehaviour
{
    private enum BirdBehavior
    {
        Sing,
        Preen,
        Ruffle,
        Peck,
        HopForward,
        HopBackward,
        HopLeft,
        HopRight
    }

    [Header("Audio")]
    public AudioClip song1;
    public AudioClip song2;
    public AudioClip flyAway1;
    public AudioClip flyAway2;

    [Header("Bird Settings")]
    public bool fleeCrows = true;

    private Animator anim;
    private lb_BirdController controller;

    private Rigidbody rb;
    private Collider mainCollider;
    private BoxCollider birdCollider;
    private SphereCollider solidCollider;
    private AudioSource audioSource;

    private Vector3 bColCenter;
    private Vector3 bColSize;

    private bool paused;
    private bool idle = true;
    private bool flying;
    private bool landing;
    private bool perched;
    private bool onGround = true;
    private bool dead;

    private float distanceToTarget;
    private float agitationLevel = 0.5f;
    private float originalAnimSpeed = 1f;

    private Vector3 originalVelocity = Vector3.zero;

    private int idleAnimationHash;
    private int flyAnimationHash;

    private int hopIntHash;
    private int flyingBoolHash;
    private int peckBoolHash;
    private int ruffleBoolHash;
    private int preenBoolHash;
    private int landingBoolHash;

    private int singTriggerHash;
    private int flyingDirectionHash;
    private int dieTriggerHash;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();
        birdCollider = GetComponent<BoxCollider>();
        solidCollider = GetComponent<SphereCollider>();
        audioSource = GetComponent<AudioSource>();

        if (rb == null)
        {
            Debug.LogError($"{name}: Không tìm thấy Rigidbody.");
        }

        if (anim == null)
        {
            Debug.LogError($"{name}: Không tìm thấy Animator.");
        }

        if (birdCollider != null)
        {
            bColCenter = birdCollider.center;
            bColSize = birdCollider.size;
        }

        idleAnimationHash = Animator.StringToHash("Base Layer.Idle");
        flyAnimationHash = Animator.StringToHash("Base Layer.fly");

        hopIntHash = Animator.StringToHash("hop");
        flyingBoolHash = Animator.StringToHash("flying");
        peckBoolHash = Animator.StringToHash("peck");
        ruffleBoolHash = Animator.StringToHash("ruffle");
        preenBoolHash = Animator.StringToHash("preen");
        landingBoolHash = Animator.StringToHash("landing");

        singTriggerHash = Animator.StringToHash("sing");
        flyingDirectionHash = Animator.StringToHash("flyingDirectionX");
        dieTriggerHash = Animator.StringToHash("die");
    }

    private void OnEnable()
    {
        if (anim == null)
            anim = GetComponent<Animator>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (mainCollider == null)
            mainCollider = GetComponent<Collider>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (dead)
        {
            Revive();
        }

        if (anim != null)
        {
            anim.SetFloat("IdleAgitated", agitationLevel);
        }
    }

    public void PauseBird()
    {
        if (dead)
            return;

        paused = true;

        if (anim != null)
        {
            originalAnimSpeed = anim.speed;
            anim.speed = 0f;
        }

        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                originalVelocity = rb.linearVelocity;
            }

            rb.isKinematic = true;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    public void UnPauseBird()
    {
        if (dead)
            return;

        paused = false;

        if (anim != null)
        {
            anim.speed = originalAnimSpeed;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = originalVelocity;
        }
    }

    public void FlyToTarget(Vector3 target)
    {
        if (dead)
            return;

        StopCoroutine(nameof(FlyToTargetRoutine));

        StartCoroutine(FlyToTargetRoutine(target));
    }

    private IEnumerator FlyToTargetRoutine(Vector3 target)
    {
        PlayFlyAwaySound();

        flying = true;
        landing = false;
        onGround = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.linearDamping = 0.5f;
            rb.useGravity = false;
        }

        if (anim != null)
        {
            anim.applyRootMotion = false;
            anim.SetBool(flyingBoolHash, true);
            anim.SetBool(landingBoolHash, false);
        }

        float animationTimeout = 1f;
        float animationTimer = 0f;

        while (anim != null &&
               anim.GetCurrentAnimatorStateInfo(0).shortNameHash !=
               Animator.StringToHash("fly") &&
               animationTimer < animationTimeout)
        {
            animationTimer += Time.deltaTime;
            yield return null;
        }

        if (rb != null && controller != null)
        {
            float scale = controller.birdScale;

            rb.AddForce(
                (transform.forward * 50f * scale) +
                (transform.up * 100f * scale),
                ForceMode.Force
            );
        }

        float t = 0f;

        while (t < 1f)
        {
            if (!paused)
            {
                t += Time.deltaTime;

                if (solidCollider != null &&
                    !solidCollider.enabled &&
                    controller != null &&
                    controller.collideWithObjects &&
                    t > 0.2f)
                {
                    solidCollider.enabled = true;
                }
            }

            yield return null;
        }

        Vector3 direction = (target - transform.position).normalized;

        if (direction == Vector3.zero)
        {
            direction = transform.forward;
        }

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation;

        distanceToTarget =
            Vector3.Distance(transform.position, target);

        if (direction.y > 0.5f)
        {
            Vector3 tempTarget =
                transform.position +
                new Vector3(
                    transform.forward.x,
                    0.5f,
                    transform.forward.z
                ) * distanceToTarget;

            Vector3 tempDirection =
                (tempTarget - transform.position).normalized;

            float rotateTime = 0f;

            while (direction.y > 0.5f && !dead)
            {
                if (!paused)
                {
                    tempDirection =
                        (tempTarget - transform.position).normalized;

                    targetRotation =
                        Quaternion.LookRotation(tempDirection);

                    transform.rotation =
                        Quaternion.Slerp(
                            startRotation,
                            targetRotation,
                            rotateTime
                        );

                    if (anim != null)
                    {
                        anim.SetFloat(
                            flyingDirectionHash,
                            FindBankingAngle(
                                transform.forward,
                                tempDirection
                            )
                        );
                    }

                    rotateTime += Time.deltaTime * 0.5f;

                    if (rb != null && controller != null)
                    {
                        rb.AddForce(
                            transform.forward *
                            70f *
                            controller.birdScale *
                            Time.deltaTime,
                            ForceMode.Force
                        );
                    }

                    direction =
                        (target - transform.position).normalized;
                }

                yield return null;
            }
        }

        startRotation = transform.rotation;
        t = 0f;

        distanceToTarget =
            Vector3.Distance(transform.position, target);

        while (distanceToTarget >= 1.5f &&
               !dead)
        {
            if (!paused)
            {
                distanceToTarget =
                    Vector3.Distance(
                        transform.position,
                        target
                    );

                direction =
                    (target - transform.position).normalized;

                if (direction == Vector3.zero)
                {
                    direction = transform.forward;
                }

                targetRotation =
                    Quaternion.LookRotation(direction);

                transform.rotation =
                    Quaternion.Slerp(
                        startRotation,
                        targetRotation,
                        t
                    );

                if (anim != null)
                {
                    anim.SetFloat(
                        flyingDirectionHash,
                        FindBankingAngle(
                            transform.forward,
                            direction
                        )
                    );
                }

                t += Time.deltaTime * 0.5f;

                if (rb != null && controller != null)
                {
                    rb.AddForce(
                        transform.forward *
                        70f *
                        controller.birdScale *
                        Time.deltaTime,
                        ForceMode.Force
                    );
                }

                if (CheckForwardCollision())
                {
                    AbortFlyToTarget();
                    yield break;
                }
            }

            yield return null;
        }

        float flyingForce =
            controller != null
                ? 50f * controller.birdScale
                : 50f;

        while (!dead)
        {
            if (!paused)
            {
                direction =
                    (target - transform.position).normalized;

                if (direction == Vector3.zero)
                    break;

                targetRotation =
                    Quaternion.LookRotation(direction);

                transform.rotation = targetRotation;

                if (anim != null)
                {
                    anim.SetFloat(
                        flyingDirectionHash,
                        FindBankingAngle(
                            transform.forward,
                            direction
                        )
                    );
                }

                if (rb != null)
                {
                    rb.AddForce(
                        transform.forward *
                        flyingForce *
                        Time.deltaTime,
                        ForceMode.Force
                    );
                }

                distanceToTarget =
                    Vector3.Distance(
                        transform.position,
                        target
                    );

                if (distanceToTarget <=
                    1.5f * GetBirdScale())
                {
                    if (solidCollider != null)
                    {
                        solidCollider.enabled = false;
                    }

                    if (distanceToTarget <
                        0.5f * GetBirdScale())
                    {
                        break;
                    }

                    if (rb != null)
                    {
                        rb.linearDamping = 2f;
                    }

                    flyingForce =
                        50f * GetBirdScale();
                }
                else if (distanceToTarget <=
                         5f * GetBirdScale())
                {
                    if (rb != null)
                    {
                        rb.linearDamping = 1f;
                    }

                    flyingForce =
                        50f * GetBirdScale();
                }

                if (CheckForwardCollision())
                {
                    AbortFlyToTarget();
                    yield break;
                }
            }

            yield return null;
        }

        StartLanding(target);
    }

    private void StartLanding(Vector3 target)
    {
        flying = false;
        landing = true;

        if (solidCollider != null)
        {
            solidCollider.enabled = false;
        }

        if (anim != null)
        {
            anim.SetFloat(flyingDirectionHash, 0f);
            anim.SetBool(landingBoolHash, true);
            anim.SetBool(flyingBoolHash, false);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        StartCoroutine(LandingRoutine(target));
    }

    private IEnumerator LandingRoutine(Vector3 target)
    {
        Vector3 velocity = Vector3.zero;

        Quaternion startRotation =
            transform.rotation;

        Vector3 euler =
            transform.localEulerAngles;

        euler.x = 0f;
        euler.z = 0f;

        Quaternion finalRotation =
            Quaternion.Euler(euler);

        float t = 0f;

        distanceToTarget =
            Vector3.Distance(
                transform.position,
                target
            );

        while (distanceToTarget >
               0.05f * GetBirdScale() &&
               t < 2f &&
               !dead)
        {
            if (!paused)
            {
                transform.rotation =
                    Quaternion.Slerp(
                        startRotation,
                        finalRotation,
                        t * 4f
                    );

                transform.position =
                    Vector3.SmoothDamp(
                        transform.position,
                        target,
                        ref velocity,
                        0.5f
                    );

                t += Time.deltaTime;

                distanceToTarget =
                    Vector3.Distance(
                        transform.position,
                        target
                    );
            }

            yield return null;
        }

        if (dead)
            yield break;

        transform.position = target;

        transform.localEulerAngles =
            new Vector3(
                0f,
                transform.localEulerAngles.y,
                0f
            );

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.linearDamping = 0.5f;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (anim != null)
        {
            anim.SetBool(landingBoolHash, false);
            anim.applyRootMotion = true;
        }

        landing = false;
        onGround = true;

        Collider[] hitColliders =
            Physics.OverlapSphere(
                target,
                0.05f * GetBirdScale()
            );

        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag("lb_bird") &&
                col.transform != transform)
            {
                col.SendMessage(
                    "FlyAway",
                    SendMessageOptions.DontRequireReceiver
                );
            }
        }
    }

    private float FindBankingAngle(
        Vector3 birdForward,
        Vector3 directionToTarget)
    {
        Vector3 cross =
            Vector3.Cross(
                birdForward,
                directionToTarget
            );

        return Vector3.Dot(cross, Vector3.up);
    }

    private void OnGroundBehaviors()
    {
        if (anim == null)
            return;

        idle =
            anim.GetCurrentAnimatorStateInfo(0).shortNameHash ==
            Animator.StringToHash("Idle");

        if (rb != null &&
            !rb.isKinematic)
        {
            rb.isKinematic = true;
        }

        if (!idle)
            return;

        if (Random.value < Time.deltaTime * 1f)
        {
            float rand = Random.value;

            if (rand < 0.3f)
            {
                DisplayBehavior(BirdBehavior.Sing);
            }
            else if (rand < 0.5f)
            {
                DisplayBehavior(BirdBehavior.Peck);
            }
            else if (rand < 0.6f)
            {
                DisplayBehavior(BirdBehavior.Preen);
            }
            else if (!perched && rand < 0.7f)
            {
                DisplayBehavior(BirdBehavior.Ruffle);
            }
            else if (!perched && rand < 0.85f)
            {
                DisplayBehavior(BirdBehavior.HopForward);
            }
            else if (!perched && rand < 0.9f)
            {
                DisplayBehavior(BirdBehavior.HopLeft);
            }
            else if (!perched && rand < 0.95f)
            {
                DisplayBehavior(BirdBehavior.HopRight);
            }
            else
            {
                DisplayBehavior(BirdBehavior.HopBackward);
            }

            anim.SetFloat(
                "IdleAgitated",
                Random.value
            );
        }

        if (Random.value <
            Time.deltaTime * 0.1f)
        {
            FlyAway();
        }
    }

    private void DisplayBehavior(
        BirdBehavior behavior)
    {
        idle = false;

        if (anim == null)
            return;

        switch (behavior)
        {
            case BirdBehavior.Sing:
                anim.SetTrigger(singTriggerHash);
                break;

            case BirdBehavior.Ruffle:
                anim.SetTrigger(ruffleBoolHash);
                break;

            case BirdBehavior.Preen:
                anim.SetTrigger(preenBoolHash);
                break;

            case BirdBehavior.Peck:
                anim.SetTrigger(peckBoolHash);
                break;

            case BirdBehavior.HopForward:
                anim.SetInteger(hopIntHash, 1);
                break;

            case BirdBehavior.HopLeft:
                anim.SetInteger(hopIntHash, -2);
                break;

            case BirdBehavior.HopRight:
                anim.SetInteger(hopIntHash, 2);
                break;

            case BirdBehavior.HopBackward:
                anim.SetInteger(hopIntHash, -1);
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("lb_bird"))
        {
            FlyAway();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (onGround &&
            (other.CompareTag("lb_groundTarget") ||
             other.CompareTag("lb_perchTarget")))
        {
            FlyAway();
        }
    }

    public void AbortFlyToTarget()
    {
        StopCoroutine(nameof(FlyToTargetRoutine));
        StopCoroutine(nameof(LandingRoutine));

        if (solidCollider != null)
        {
            solidCollider.enabled = false;
        }

        if (anim != null)
        {
            anim.SetBool(landingBoolHash, false);
            anim.SetFloat(flyingDirectionHash, 0f);
        }

        transform.localEulerAngles =
            new Vector3(
                0f,
                transform.localEulerAngles.y,
                0f
            );

        FlyAway();
    }

    public void FlyAway()
    {
        if (dead)
            return;

        StopCoroutine(nameof(FlyToTargetRoutine));
        StopCoroutine(nameof(LandingRoutine));

        if (anim != null)
        {
            anim.SetBool(landingBoolHash, false);
        }

        if (controller != null)
        {
            controller.SendMessage(
                "BirdFindTarget",
                gameObject,
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    public void Flee()
    {
        if (dead)
            return;

        StopCoroutine(nameof(FlyToTargetRoutine));
        StopCoroutine(nameof(LandingRoutine));

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (anim != null)
        {
            anim.Play(flyAnimationHash);
        }

        float scale = GetBirdScale();

        Vector3 farAwayTarget =
            transform.position +
            new Vector3(
                Random.Range(-100f, 100f) * scale,
                Random.Range(5f, 10f) * scale,
                Random.Range(-100f, 100f) * scale
            );

        StartCoroutine(
            FlyToTargetRoutine(farAwayTarget)
        );
    }

    public void CrowIsClose()
    {
        if (fleeCrows && !dead)
        {
            Flee();
        }
    }

    public void KillBird()
    {
        if (dead)
            return;

        if (controller != null)
        {
            controller.SendMessage(
                "FeatherEmit",
                transform.position,
                SendMessageOptions.DontRequireReceiver
            );
        }

        if (anim != null)
        {
            anim.SetTrigger(dieTriggerHash);
            anim.applyRootMotion = false;
        }

        dead = true;
        onGround = false;
        flying = false;
        landing = false;
        idle = false;
        perched = false;

        StopAllCoroutines();

        if (mainCollider != null)
        {
            mainCollider.isTrigger = false;
        }

        if (birdCollider != null)
        {
            birdCollider.center = Vector3.zero;

            birdCollider.size =
                new Vector3(
                    0.1f,
                    0.01f,
                    0.1f
                ) * GetBirdScale();
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    public void KillBirdWithForce(Vector3 force)
    {
        if (dead)
            return;

        if (controller != null)
        {
            controller.SendMessage(
                "FeatherEmit",
                transform.position,
                SendMessageOptions.DontRequireReceiver
            );
        }

        if (anim != null)
        {
            anim.SetTrigger(dieTriggerHash);
            anim.applyRootMotion = false;
        }

        dead = true;
        onGround = false;
        flying = false;
        landing = false;
        idle = false;
        perched = false;

        StopAllCoroutines();

        if (mainCollider != null)
        {
            mainCollider.isTrigger = false;
        }

        if (birdCollider != null)
        {
            birdCollider.center = Vector3.zero;

            birdCollider.size =
                new Vector3(
                    0.1f,
                    0.01f,
                    0.1f
                ) * GetBirdScale();
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            rb.AddForce(
                force,
                ForceMode.Impulse
            );
        }
    }

    public void Revive()
    {
        if (!dead)
            return;

        if (birdCollider != null)
        {
            birdCollider.center = bColCenter;
            birdCollider.size = bColSize;
        }

        if (mainCollider != null)
        {
            mainCollider.isTrigger = true;
        }

        dead = false;
        onGround = false;
        flying = false;
        landing = false;
        idle = true;
        perched = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (anim != null)
        {
            anim.Play(idleAnimationHash);
        }

        if (controller != null)
        {
            controller.SendMessage(
                "BirdFindTarget",
                gameObject,
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    public void SetController(lb_BirdController cont)
    {
        controller = cont;
    }

    public void ResetHopInt()
    {
        if (anim != null)
        {
            anim.SetInteger(hopIntHash, 0);
        }
    }

    public void ResetFlyingLandingVariables()
    {
        if (flying || landing)
        {
            flying = false;
            landing = false;
        }
    }

    public void PlaySong()
    {
        if (dead || audioSource == null)
            return;

        AudioClip clip = null;

        if (Random.value < 0.5f)
        {
            clip = song1;
        }
        else
        {
            clip = song2;
        }

        if (clip != null)
        {
            audioSource.PlayOneShot(
                clip,
                1f
            );
        }
    }

    private void PlayFlyAwaySound()
    {
        if (audioSource == null)
            return;

        AudioClip clip =
            Random.value < 0.5f
                ? flyAway1
                : flyAway2;

        if (clip != null)
        {
            audioSource.PlayOneShot(
                clip,
                0.1f
            );
        }
    }

    private bool CheckForwardCollision()
    {
        if (controller == null ||
            !controller.collideWithObjects)
        {
            return false;
        }

        float scale = controller.birdScale;

        Vector3 forward =
            transform.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.001f)
            return false;

        forward.Normalize();

        Vector3 origin =
            transform.position +
            transform.forward *
            (0.15f * scale);

        if (Physics.Raycast(
            origin,
            forward,
            out RaycastHit hit,
            0.75f * scale))
        {
            if (!hit.collider.isTrigger)
            {
                return true;
            }
        }

        return false;
    }

    private float GetBirdScale()
    {
        if (controller != null)
        {
            return Mathf.Max(
                controller.birdScale,
                0.01f
            );
        }

        return 1f;
    }

    private void Update()
    {
        if (onGround &&
            !paused &&
            !dead)
        {
            OnGroundBehaviors();
        }
    }
}