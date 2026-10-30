using System.Collections;
using UnityEngine;

/// <summary>
/// Hành vi NPC Chó Phát Hiện Quái Vật (Dog Bark Detection NPC):
/// Kế thừa từ NPCBaseBehavior.
/// - Khi NPC này đến vạch kiểm tra, chú chó ở bốt gác sẽ sủa lên để cảnh báo người chơi:
///   + Nếu chó sủa từ 1 đến 3 tiếng: Đây là người bình thường (Người chơi có thể Approve).
///   + Nếu chó sủa nhiều hơn 3 tiếng (4-6 tiếng dồn dập): Đây là sinh vật đột biến!
///     Người chơi bắt buộc phải nhấn DENY. Khi nhấn DENY, con quái sẽ lộ mặt và JUMPSCARE
///     người chơi bằng 1 tấm hình kinh dị toàn màn hình!
/// </summary>
public class NPCDogBarkBehavior : NPCBaseBehavior
{
    [Header("=== CẤU HÌNH TIẾNG CHÓ SỦA ===")]
    [Tooltip("Nếu true: Chó sủa > 3 tiếng (4-6 tiếng) và là quái vật. Nếu false: Chó sủa 1-3 tiếng (người an toàn)")]
    public bool isMutant = false;

    [Tooltip("Tự động ngẫu nhiên chọn người thường (1-3 tiếng sủa) hay quái vật (> 3 tiếng sủa) khi sinh ra")]
    public bool randomizeOnStart = true;

    [Tooltip("Số tiếng sủa chính xác (nếu để 0 sẽ tự động ngẫu nhiên theo trạng thái isMutant)")]
    public int barkCount = 0;

    [Header("=== ÂM THANH TIẾNG CHÓ SỦA ===")]
    [Tooltip("Vị trí chú chó tại bốt gác (tự động nhận diện nếu để trống)")]
    public Transform dogPoint;
    public AudioClip dogBarkClip;
    public AudioClip dogAggressiveBarkClip;
    public float barkInterval = 0.55f;

    [Header("=== TẤM HÌNH KINH DỊ JUMPSCARE ===")]
    [Tooltip("Kéo tấm hình kinh dị (Sprite) vào đây để hiển thị khi người chơi bấm Deny")]
    public Sprite jumpscareSprite;
    [Tooltip("Âm thanh tiếng thét kinh dị khi jumpscare")]
    public AudioClip jumpscareSound;
    public float jumpscareDuration = 1.8f;

    [Header("=== TRẠNG THÁI ===")]
    public bool isDogBarking = false;
    public bool hasFinishedBarking = false;
    public bool hasJumpscareTriggered = false;

    private AudioSource dogAudioSource;

    protected override void Awake()
    {
        base.Awake();

#if UNITY_EDITOR
        if (dogBarkClip == null)
            dogBarkClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/NPCVoice/Dog_Bark_Single.wav");

        if (dogAggressiveBarkClip == null)
            dogAggressiveBarkClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/NPCVoice/Dog_Bark_Aggressive.wav");

        if (jumpscareSprite == null)
            jumpscareSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Phu-Asset/Textures/Jumpscare/Horror_Jumpscare_Face.png");

        if (jumpscareSound == null)
            jumpscareSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/NPCVoice/Jumpscare_Scream_Horror.wav");
#endif
    }

    protected override void Start()
    {
        base.Start();

        // 1. Xác định trạng thái người thường hay đột biến
        if (randomizeOnStart)
        {
            isMutant = (Random.value > 0.45f); // ~55% tỷ lệ đột biến
        }

        // 2. Tính số tiếng sủa tương ứng
        if (barkCount <= 0)
        {
            if (isMutant)
            {
                // Quái vật -> Chó sủa nhiều hơn 3 tiếng (4, 5 hoặc 6 tiếng)
                barkCount = Random.Range(4, 7);
            }
            else
            {
                // Người an toàn -> Chó sủa từ 1 đến 3 tiếng
                barkCount = Random.Range(1, 4);
            }
        }

        // 3. Tìm hoặc tạo vị trí nguồn phát tiếng sủa của chú chó
        SetupDogAudioSource();
    }

    private void SetupDogAudioSource()
    {
        if (dogPoint == null)
        {
            GameObject dogObj = GameObject.Find("DogCompanion");
            if (dogObj == null) dogObj = GameObject.Find("Dog");
            if (dogObj != null) dogPoint = dogObj.transform;
        }

        if (dogPoint == null)
        {
            // Tự tạo một điểm chú chó nằm ngay cạnh cửa sổ bốt gác
            GameObject autoDog = new GameObject("DogCompanion_Booth");
            autoDog.transform.position = new Vector3(-50.8f, 0.25f, -49.6f);
            dogPoint = autoDog.transform;
        }

        dogAudioSource = dogPoint.GetComponent<AudioSource>();
        if (dogAudioSource == null)
        {
            dogAudioSource = dogPoint.gameObject.AddComponent<AudioSource>();
            dogAudioSource.spatialBlend = 0.85f;
            dogAudioSource.minDistance = 2f;
            dogAudioSource.maxDistance = 25f;
        }
    }

    protected override void OnInspectionStarted()
    {
        base.OnInspectionStarted();

        // Bắt đầu chuỗi tiếng chó sủa cảnh báo
        StartCoroutine(DogBarkRoutine());
    }

    private IEnumerator DogBarkRoutine()
    {
        // Chờ NPC bước hẳn vào tầm nhìn cửa sổ
        yield return new WaitForSeconds(0.8f);

        isDogBarking = true;
        SetupDogAudioSource();

        Debug.Log($"🐕 [NPCDogBarkBehavior] Chú chó bắt đầu sủa {barkCount} tiếng! (Đột biến: {isMutant})");

        for (int i = 0; i < barkCount; i++)
        {
            AudioClip clipToPlay;
            float pitch = 1.0f;

            if (isMutant)
            {
                // Tiếng sủa dồn dập, gắt gỏng khi gặp quái vật
                clipToPlay = (dogAggressiveBarkClip != null) ? dogAggressiveBarkClip : dogBarkClip;
                pitch = Random.Range(1.05f, 1.25f);
            }
            else
            {
                // Tiếng sủa bình thường khi gặp người quen
                clipToPlay = (dogBarkClip != null) ? dogBarkClip : dogAggressiveBarkClip;
                pitch = Random.Range(0.95f, 1.05f);
            }

            if (clipToPlay != null && dogAudioSource != null)
            {
                dogAudioSource.pitch = pitch;
                dogAudioSource.PlayOneShot(clipToPlay, 1.0f);
            }

            yield return new WaitForSeconds(barkInterval);
        }

        isDogBarking = false;
        hasFinishedBarking = true;
    }

    /// <summary>
    /// Kiểm tra xem NPC này có kích hoạt Jumpscare khi bấm Deny hay không
    /// </summary>
    public bool ShouldJumpscareOnDeny()
    {
        return (barkCount > 3);
    }

    /// <summary>
    /// Kích hoạt Jumpscare bằng 1 tấm hình kinh dị toàn màn hình
    /// </summary>
    public void TriggerImageJumpscare(System.Action onFinish = null)
    {
        if (hasJumpscareTriggered) return;
        hasJumpscareTriggered = true;

        Debug.Log("😱 [NPCDogBarkBehavior] KÍCH HOẠT JUMPSCARE BẰNG TẤM HÌNH KINH DỊ!");
        HorrorImageJumpscareUI.Show(jumpscareSprite, jumpscareSound, jumpscareDuration, onFinish);
    }
}
