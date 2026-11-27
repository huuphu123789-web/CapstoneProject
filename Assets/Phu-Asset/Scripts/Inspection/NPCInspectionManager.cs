using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class NPCInspectionProfile
{
    public string npcName = "Citizen";
    public bool isMutant = false;
    public GameObject npcPrefab;
    public Texture2D cccdTexture;
    public Texture2D surrenderConfirmTexture;

    [Header("=== DEFENSE DIALOGUE ===")]
    [Tooltip("First line spoken upon arriving at the guard booth window")]
    [TextArea(2, 4)] public string dialogueOnArrive = "Hello officer! I'm a normal human! Please let me in!";

    [Tooltip("List of continuous defense dialogues while waiting for documents to be inspected")]
    [TextArea(2, 4)] public string[] defenseDialogues;

    [Tooltip("Dialogue when approved to pass through")]
    [TextArea(2, 4)] public string dialogueOnApproved = "Thank you so much, officer! You saved my life!";

    [Tooltip("Dialogue when denied entry")]
    [TextArea(2, 4)] public string dialogueOnDenied = "No! Please don't leave me out here in the dark!!";

    [Header("=== VOICE ACTING AUDIO CLIPS ===")]
    [Tooltip("Voice audio clip when NPC first arrives at the window")]
    public AudioClip voiceArriveClip;

    [Tooltip("List of defense voice audio clips cycled continuously")]
    public AudioClip[] defenseVoiceClips;

    [Tooltip("Voice audio clip when approved")]
    public AudioClip voiceApproveClip;

    [Tooltip("Voice audio clip when denied")]
    public AudioClip voiceDenyClip;

    /// <summary>
    /// Returns the defense dialogues. Falls back to default English survivor/mutant lines if none provided.
    /// </summary>
    public string[] GetEffectiveDefenseDialogues()
    {
        if (defenseDialogues != null && defenseDialogues.Length > 0)
        {
            bool hasValid = false;
            for (int i = 0; i < defenseDialogues.Length; i++)
            {
                if (!string.IsNullOrEmpty(defenseDialogues[i])) { hasValid = true; break; }
            }
            if (hasValid) return defenseDialogues;
        }

        if (isMutant)
        {
            return new string[]
            {
                "I am... I am fine... See? I look completely normal... Just like you...",
                "Officer... why are you staring at me? My papers... they are real...",
                "Open the gate... quickly... It is so cold... I am starving... I mean, exhausted...",
                "Don't suspect me... I am human... I swear... Please..."
            };
        }
        else
        {
            return new string[]
            {
                "I am a normal human, officer! Look at me, I am not one of those mutants!",
                "I have my ID card and surrender confirm right here! Everything is legitimate, check it yourself!",
                "Please, it is freezing out here and monsters are prowling in the dark... don't leave me outside!",
                "I swear on my life, I am completely clean! No signs of infection at all!"
            };
        }
    }

    public AudioClip GetEffectiveArriveClip()
    {
        if (voiceArriveClip != null) return voiceArriveClip;
        return LoadVoiceWithFallback(
            isMutant ? "Assets/Phu-Asset/Audio/NPCVoice/Trinh Hoang Long/Mutant_Arrive.wav" : "Assets/Phu-Asset/Audio/NPCVoice/Victor Jean Mercier/Human_Arrive.wav",
            isMutant ? "Assets/Phu-Asset/Audio/NPCVoice/Mutant/Mutant_Arrive.wav" : "Assets/Phu-Asset/Audio/NPCVoice/Human/Human_Arrive.wav"
        );
    }

    public AudioClip GetEffectiveDefenseClip(int index)
    {
        if (defenseVoiceClips != null && defenseVoiceClips.Length > 0)
        {
            AudioClip c = defenseVoiceClips[index % defenseVoiceClips.Length];
            if (c != null) return c;
        }
        int clipNum = (index % 4) + 1;
        return LoadVoiceWithFallback(
            isMutant ? $"Assets/Phu-Asset/Audio/NPCVoice/Trinh Hoang Long/Mutant_Defense_{clipNum}.wav" : $"Assets/Phu-Asset/Audio/NPCVoice/Victor Jean Mercier/Human_Defense_{clipNum}.wav",
            isMutant ? $"Assets/Phu-Asset/Audio/NPCVoice/Mutant/Mutant_Defense_{clipNum}.wav" : $"Assets/Phu-Asset/Audio/NPCVoice/Human/Human_Defense_{clipNum}.wav"
        );
    }

    public AudioClip GetEffectiveApproveClip()
    {
        if (voiceApproveClip != null) return voiceApproveClip;
        return LoadVoiceWithFallback(
            isMutant ? "Assets/Phu-Asset/Audio/NPCVoice/Trinh Hoang Long/Mutant_Approve.wav" : "Assets/Phu-Asset/Audio/NPCVoice/Victor Jean Mercier/Human_Approve.wav",
            isMutant ? "Assets/Phu-Asset/Audio/NPCVoice/Mutant/Mutant_Approve.wav" : "Assets/Phu-Asset/Audio/NPCVoice/Human/Human_Approve.wav"
        );
    }

    public AudioClip GetEffectiveDenyClip()
    {
        if (voiceDenyClip != null) return voiceDenyClip;
        return LoadVoiceWithFallback(
            isMutant ? "Assets/Phu-Asset/Audio/NPCVoice/Trinh Hoang Long/Mutant_Deny.wav" : "Assets/Phu-Asset/Audio/NPCVoice/Victor Jean Mercier/Human_Deny.wav",
            isMutant ? "Assets/Phu-Asset/Audio/NPCVoice/Mutant/Mutant_Deny.wav" : "Assets/Phu-Asset/Audio/NPCVoice/Human/Human_Deny.wav"
        );
    }

    private static AudioClip LoadVoiceWithFallback(string primaryPath, string fallbackPath)
    {
        AudioClip clip = LoadAudioFromPath(primaryPath);
        if (clip == null) clip = LoadAudioFromPath(fallbackPath);
        return clip;
    }

    private static AudioClip LoadAudioFromPath(string path)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
#else
        return null;
#endif
    }
}

/// <summary>
/// Quản lý toàn bộ quy trình kiểm tra NPC tại cổng gác:
/// 1. Tự động khởi động khi TaskManager hoàn thành 4 nhiệm vụ ban đầu.
/// 2. Điều phối NPC đi tới vạch InspectionZone trước bốt gác.
/// 3. Xuất trình CCCD và Giấy xác nhận đầu hàng (Surrender Confirm / Giấy thông hành).
/// 4. Xử lý quyết định của người chơi: Cho qua (Approve), Từ chối (Reject), hoặc Bắn súng tiêu diệt (Shoot).
/// 5. Báo cáo hoàn thành cho TaskManager để mở khóa giường ngủ.
/// </summary>
public class NPCInspectionManager : MonoBehaviour
{
    public static NPCInspectionManager instance;

    [Header("=== DANH SÁCH NPC KIỂM TRA (INSPECTION QUEUE) ===")]
    [Tooltip("Danh sách NPC sẽ đến cổng kiểm tra. Bạn có thể tự do thêm 4 mẫu tại đây.")]
    public List<NPCInspectionProfile> npcQueue = new List<NPCInspectionProfile>();

    [Tooltip("Tự động nạp mẫu khi danh sách trống. Tắt (false) để bạn tự thiết lập 4 mẫu trong Inspector.")]
    public bool autoPopulateDefaults = false;

    [Header("=== TỰ ĐỘNG BẮT ĐẦU KIỂM TRA ===")]
    [Tooltip("Bật (tick) nếu muốn bắt đầu cho NPC đi vào kiểm tra ngay khi bấm Play (bất kể có TaskManager hay không)")]
    public bool startInspectionOnStart = false;

    [Tooltip("Nếu trong Scene chưa gắn TaskManager, tự động bắt đầu kiểm tra NPC sau 1 giây để test ngay")]
    public bool autoStartIfNoTaskManager = true;

    [Header("=== CÁC ĐIỂM TỌA ĐỘ TRONG SCENE ===")]
    [Tooltip("Điểm đứng chờ kiểm tra trước bốt gác (inspectionZone / InspectionZone)")]
    public Transform inspectionPoint;

    [Tooltip("Điểm xuất phát của NPC bên ngoài cổng (NPC_SpawnPoint). Nếu để trống sẽ dùng tạm điểm dismiss")]
    public Transform spawnPoint;

    [Tooltip("Độ lệch chiều cao Y khi spawn (cộng thêm vào nếu NPC bị chìm chân hoặc lơ lửng)")]
    public float spawnHeightOffset = 0f;

    [Tooltip("Điểm NPC đi vào sau khi được Cho qua (pass / NPC_PassDestination)")]
    public Transform passDestination;

    [Tooltip("Điểm NPC quay đầu bỏ đi sau khi bị Từ chối (dismiss / NPC_DismissDestination)")]
    public Transform dismissDestination;

    [Header("=== ÂM THANH & HIỆU ỨNG ===")]
    public AudioClip documentOpenSound;
    public AudioClip gateBuzzerSound;
    public AudioClip alarmBreachSound;
    public AudioClip correctDecisionChime;

    [Header("=== GIAO DIỆN LỜI THOẠI (DIALOGUE SUBTITLE UI) ===")]
    [Tooltip("Panel chứa khung phụ đề lời thoại NPC (nếu để trống script tự động tạo ở đáy màn hình)")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueNameText;
    public TextMeshProUGUI dialogueContentText;
    public CanvasGroup dialogueCanvasGroup;

    [Header("=== TRẠNG THÁI HIỆN TẠI ===")]
    public int currentNPCIndex = -1;
    public bool isInspectionActive = false;
    public bool isCurrentNPCAtDesk = false;

    private GameObject currentNPCObject;
    private NavMeshAgent currentAgent;
    private NPCInspectionProfile currentProfile;
    public bool hasDecisionBeenMade = false;

    private Coroutine typingCoroutine;
    private Coroutine defenseCycleCoroutine;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        ValidateOrFormatNPCNames();
    }

    void Start()
    {
        EnsureSceneReferences();
        EnsureDefaultProfiles();
        ValidateOrFormatNPCNames();
        EnsureDeskAndButtons();
        EnsureDialogueUI();
        EnsureUniqueDialoguesAssigned();
        EnsureVoiceClipsAssigned();
        EnsureDocumentTexturesAssigned();

        bool noTaskManager = (TaskManager.instance == null && FindAnyObjectByType<TaskManager>() == null);
        if (startInspectionOnStart || (autoStartIfNoTaskManager && noTaskManager))
        {
            Invoke(nameof(StartGateInspection), 1.0f);
        }
    }

    void Update()
    {
        // Khi NPC đã đứng ở vạch kiểm tra và chưa có quyết định -> luôn xoay mặt mượt về phía Player
        if (isCurrentNPCAtDesk && !hasDecisionBeenMade && currentNPCObject != null && currentNPCObject.activeInHierarchy)
        {
            NPCJumperBehavior jumper = currentNPCObject.GetComponentInChildren<NPCJumperBehavior>();
            if (jumper != null && jumper.hasTriggeredScare) return;

            Transform playerTarget = Camera.main != null ? Camera.main.transform : null;
            if (playerTarget == null)
            {
                GameObject p = GameObject.FindWithTag("Player");
                if (p != null) playerTarget = p.transform;
            }

            if (playerTarget != null)
            {
                Vector3 dir = playerTarget.position - currentNPCObject.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                    currentNPCObject.transform.rotation = Quaternion.Slerp(
                        currentNPCObject.transform.rotation,
                        targetRot,
                        Time.deltaTime * 6f
                    );
                }
            }
        }
    }

    /// <summary>
    /// Giữ nguyên tên NPC mà người dùng đã điền sẵn trong Inspector của NPCInspectionManager.
    /// Chỉ tự điền tên sạch (bỏ đuôi -Normal/-Floating để tránh lộ vai) nếu ô npcName đang để trống.
    /// </summary>
    public void ValidateOrFormatNPCNames()
    {
        if (npcQueue == null) return;
        string[] availableNames = new string[]
        {
            "Tran Quoc Hung", "Le Van Dat", "Pham Minh Tuan", "Nguyen Van An",
            "ETIENNE PIERRE GIRAR", "Bui Hoang Viet", "Vu Quang Huy", "Hoang Duc Anh",
            "Vo Van Nam", "Victor Jean Mercier"
        };
        int nameIdx = 0;

        foreach (var profile in npcQueue)
        {
            if (profile == null) continue;

            // Nếu người dùng đã ghi sẵn tên NPC trong Inspector -> giữ nguyên 100%
            if (!string.IsNullOrEmpty(profile.npcName) &&
                !profile.npcName.Equals("Citizen", StringComparison.OrdinalIgnoreCase) &&
                !profile.npcName.StartsWith("Subject", StringComparison.OrdinalIgnoreCase) &&
                !profile.npcName.StartsWith("NPC-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Nếu để trống -> lấy phần tên người (trước dấu '-') từ tên Prefab để không lộ đuôi đột biến
            if (profile.npcPrefab != null)
            {
                string pName = profile.npcPrefab.name;
                int dash = pName.IndexOf('-');
                if (dash > 0 && !pName.StartsWith("NPC-", StringComparison.OrdinalIgnoreCase))
                {
                    profile.npcName = pName.Substring(0, dash).Trim();
                    continue;
                }
            }

            profile.npcName = availableNames[nameIdx % availableNames.Length];
            nameIdx++;
        }
    }

    private struct NPCDialoguePack
    {
        public string keyword;
        public string voiceFolder;
        public string arrive;
        public string[] defense;
        public string approved;
        public string denied;
    }

    /// <summary>
    /// Tự động gán 10 bộ lời thoại + voice khớp từng chữ hoàn toàn khác nhau, không trùng lặp cho từng NPC trong danh sách
    /// (nếu NPC chưa được viết thoại riêng trong Inspector).
    /// </summary>
    public void EnsureUniqueDialoguesAssigned()
    {
        if (npcQueue == null || npcQueue.Count == 0) return;

        NPCDialoguePack[] packs = new NPCDialoguePack[]
        {
            // 1. Victor Jean Mercier - Người thường kiệt sức từ trạm tiền tiêu
            new NPCDialoguePack
            {
                keyword = "victor",
                voiceFolder = "Set_01_Victor",
                arrive = "Evening, officer. My patrol truck broke down two miles back. Here are my clearance papers.",
                defense = new string[]
                {
                    "My hands are shaking from the frost, not from infection. Check the seal on my ID.",
                    "The radio tower went dead an hour ago. I walked straight along the main road.",
                    "I have served at Sector Four for three winters. You can verify my serial number.",
                    "Please hurry, the mist is thickening behind the treeline."
                },
                approved = "Appreciate it, officer. Stay sharp at the post tonight.",
                denied = "Wait—my papers are genuine! You are locking me out here in the fog!"
            },
            // 2. Trinh Hoang Long - SpinningLegs (Cứng nhắc, cố giả giọng quân nhân)
            new NPCDialoguePack
            {
                keyword = "trinh hoang long",
                voiceFolder = "Set_02_SpinningLegs",
                arrive = "Reporting for entry, sentinel. Stand still... my knees are merely stiff from the march.",
                defense = new string[]
                {
                    "Do not look down at my boots. The mud makes every step feel... twisted.",
                    "Every stamp on that folder is regulation. Why are you hesitating?",
                    "A soldier does not flinch when his joints turn a little further than usual.",
                    "Open the barrier now. The night air makes my legs restless."
                },
                approved = "Wise decision... I will walk right through.",
                denied = "You noticed the legs, didn't you? Big mistake!"
            },
            // 3. Tran Quoc Hung - Crawler (Thì thào, hoảng loạn dưới bóng tối)
            new NPCDialoguePack
            {
                keyword = "crawler",
                voiceFolder = "Set_03_Crawler",
                arrive = "Psst... officer... keep the spotlight low. Something crawled beside me in the ditch.",
                defense = new string[]
                {
                    "I am fine, I just prefer staying close to the ground where they cannot see me.",
                    "Check the pass key quickly, please... the glass between us feels so thin.",
                    "Did you hear scratching behind your booth? No? Must be the wind...",
                    "Let me inside before I have to climb over the fence myself."
                },
                approved = "Good... stay right where you are, officer.",
                denied = "Don't turn your back on the booth... I'll find another way in!"
            },
            // 4. Le Van Dat - DogBark (Lo lắng vì con chó sủa)
            new NPCDialoguePack
            {
                keyword = "dogbark",
                voiceFolder = "Set_04_DogBark",
                arrive = "Hello there! Mind telling your guard dog to settle down? I only came from the supply road.",
                defense = new string[]
                {
                    "Dogs always bark at strangers in the dark. It doesn't mean anything is wrong with me!",
                    "Look at my documents, not the hound! I smell like campfire smoke, that's all.",
                    "Seriously, why is it staring at me like that? Just scan my card already.",
                    "I swear I am clean—your dog is just spooked by the forest!"
                },
                approved = "Ha! See? Told you the mutt was overreacting. Thanks, officer!",
                denied = "Shut that dog up!! You shouldn't have trusted a beast over me!"
            },
            // 5. Pham Minh Tuan - Glitch (Ngắt quãng như sóng nhiễu)
            new NPCDialoguePack
            {
                keyword = "glitch",
                voiceFolder = "Set_05_Glitch",
                arrive = "Off-officer... requesting gate—gate clearance. Signal is... I mean, my voice is fine.",
                defense = new string[]
                {
                    "Why does the booth light keep flickering? I am standing—standing completely still.",
                    "Read the ID again. Every line is—is—is synchronized with central command.",
                    "Don't blink, officer. People get nervous when I move between frames.",
                    "Grant access now before the static gets louder."
                },
                approved = "Access... granted. Entering—entering perimeter.",
                denied = "Error... rejection not accepted! You cannot erase me!"
            },
            // 6. Nguyen Van An - Jumper (Nóng nảy, thúc giục)
            new NPCDialoguePack
            {
                keyword = "jumper",
                voiceFolder = "Set_06_Jumper",
                arrive = "Open the gate! Stop staring at the console and buzz me through right now!",
                defense = new string[]
                {
                    "How long does it take to read two sheets of paper?! My heart is pounding out of my chest!",
                    "You think I walked all this way just to stand under your floodlight?",
                    "Keep your hand off that red switch, officer. I am warning you.",
                    "One more second of waiting and I am coming straight through that window!"
                },
                approved = "Finally! Should have opened it the moment I got here!",
                denied = "You hit deny?! You're dead, officer!!"
            },
            // 7. ETIENNE PIERRE GIRAR - Screamer (Lạnh lẽo, trống rỗng)
            new NPCDialoguePack
            {
                keyword = "screamer",
                voiceFolder = "Set_07_Screamer",
                arrive = "Good night, watchman. It is terribly quiet out here... almost too quiet for your ears.",
                defense = new string[]
                {
                    "My papers are in order. Though I wonder if the glass can hold back a loud sound.",
                    "Do your speakers ever ring when someone stands this close to the booth?",
                    "Take your time reading my name. I am very patient... until my throat opens.",
                    "Approve my passage, and I promise not to wake the whole base."
                },
                approved = "How polite... enjoy the silence while it lasts.",
                denied = "Then cover your ears, watchman!"
            },
            // 8. Bui Hoang Viet - Boomer (Không nói chuyện, chỉ phát tiếng bom hẹn giờ bíp bíp)
            new NPCDialoguePack
            {
                keyword = "boomer",
                voiceFolder = "",
                arrive = "*BEEP... BEEP... BEEP...*",
                defense = new string[]
                {
                    "*BEEP... BEEP... BEEP...*",
                    "*BEEP—BEEP—BEEP...*",
                    "*TICK... TICK... BEEP...*",
                    "*BEEP! BEEP! BEEP!*"
                },
                approved = "*BEEP—BEEP—BEEP!*",
                denied = "*BEEP—BEEP—BEEP!*"
            },
            // 9. Vu Quang Huy - DetachedLimbs (Cứng đờ, giấu tay)
            new NPCDialoguePack
            {
                keyword = "detached",
                voiceFolder = "Set_09_Detached",
                arrive = "Evening, sir. I slid my papers through the slot—sorry if my grip looked awkward.",
                defense = new string[]
                {
                    "My shoulders went numb an hour ago. Feels like my arms aren't even attached.",
                    "Please keep your eyes on the ID photo. Staring at my sleeves makes me uneasy.",
                    "Everything is still in one piece, officer. Mostly.",
                    "Just approve the pass so I can sit somewhere warm and pull myself together."
                },
                approved = "Much obliged... try not to mind anything I drop.",
                denied = "Fine! Keep the gate shut—pieces of me will still slip inside!"
            },
            // 10. Hoang Duc Anh / Vo Van Nam - Floating & HeadSpin (Mơ hồ, rợn người)
            new NPCDialoguePack
            {
                keyword = "floating",
                voiceFolder = "Set_10_Floating",
                arrive = "Officer... have you ever felt like the ground below this checkpoint isn't real?",
                defense = new string[]
                {
                    "I feel so light tonight, like a single gust of wind could lift me over your roof.",
                    "Look closely at my surrender confirm. Nothing unusual—just ignore the height.",
                    "From up here, your guard booth looks awfully small and fragile.",
                    "Let me pass, or I might drift right past the wire anyway."
                },
                approved = "Lovely... I will float right in.",
                denied = "You think a locked gate can hold me to the earth?!"
            }
        };

        HashSet<int> usedIndices = new HashSet<int>();

        for (int i = 0; i < npcQueue.Count; i++)
        {
            NPCInspectionProfile npc = npcQueue[i];
            if (npc == null) continue;

            // Kiểm tra xem người dùng đã tự viết lời thoại riêng chưa
            bool hasCustomArrive = !string.IsNullOrEmpty(npc.dialogueOnArrive) &&
                                   !npc.dialogueOnArrive.StartsWith("Hello officer! I'm a normal human");
            bool hasCustomDefense = npc.defenseDialogues != null && npc.defenseDialogues.Length > 0 &&
                                    !string.IsNullOrEmpty(npc.defenseDialogues[0]);

            // Ưu tiên chọn bộ thoại khớp với tên/loại NPC mà chưa bị trùng
            string fullName = ((npc.npcName ?? "") + " " + (npc.npcPrefab != null ? npc.npcPrefab.name : "")).ToLowerInvariant();
            int chosenIdx = -1;

            for (int p = 0; p < packs.Length; p++)
            {
                if (!usedIndices.Contains(p) && fullName.Contains(packs[p].keyword))
                {
                    chosenIdx = p;
                    break;
                }
            }

            if (chosenIdx < 0)
            {
                for (int p = 0; p < packs.Length; p++)
                {
                    int candidate = (i + p) % packs.Length;
                    if (!usedIndices.Contains(candidate))
                    {
                        chosenIdx = candidate;
                        break;
                    }
                }
            }

            if (chosenIdx < 0) chosenIdx = i % packs.Length;
            usedIndices.Add(chosenIdx);

            NPCDialoguePack pack = packs[chosenIdx];
            if (!hasCustomArrive) npc.dialogueOnArrive = pack.arrive;
            if (!hasCustomDefense) npc.defenseDialogues = pack.defense;
            if (string.IsNullOrEmpty(npc.dialogueOnApproved) || npc.dialogueOnApproved.StartsWith("Thank you so much, officer!"))
                npc.dialogueOnApproved = pack.approved;
            if (string.IsNullOrEmpty(npc.dialogueOnDenied) || npc.dialogueOnDenied.StartsWith("No! Please don't leave me out here"))
                npc.dialogueOnDenied = pack.denied;

            // Gán luôn bộ voice .wav khớp từng chữ tương ứng với bộ thoại (bỏ qua nếu voiceFolder rỗng như Boomer chỉ có tiếng bíp)
            if (!string.IsNullOrEmpty(pack.voiceFolder))
            {
                string voiceBase = $"Assets/Phu-Asset/Audio/NPCVoice/{pack.voiceFolder}";
                if (npc.voiceArriveClip == null) npc.voiceArriveClip = LoadAudioFromPath($"{voiceBase}/Arrive.wav");
                if (npc.voiceApproveClip == null) npc.voiceApproveClip = LoadAudioFromPath($"{voiceBase}/Approve.wav");
                if (npc.voiceDenyClip == null) npc.voiceDenyClip = LoadAudioFromPath($"{voiceBase}/Deny.wav");
                if (npc.defenseVoiceClips == null || npc.defenseVoiceClips.Length == 0)
                {
                    npc.defenseVoiceClips = new AudioClip[4];
                    for (int d = 0; d < 4; d++)
                    {
                        npc.defenseVoiceClips[d] = LoadAudioFromPath($"{voiceBase}/Defense_{d + 1}.wav");
                    }
                }
            }
        }
    }

    private AudioClip LoadAudioFromPath(string path)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
#else
        return null;
#endif
    }

    /// <summary>
    /// Tự động nạp ảnh CCCD và Surrender Confirm cho các NPC trong danh sách nếu người dùng chưa kéo thủ công
    /// </summary>
    public void EnsureDocumentTexturesAssigned()
    {
        if (npcQueue == null) return;

        string[] fallbackCccds = new string[]
        {
            "Assets/Loc asset/sprites/CCCD/CCQN/Victor Jean Mercier.png",
            "Assets/Loc asset/sprites/CCCD/CCQN/Trinh Hoang Long.png",
            "Assets/Loc asset/sprites/CCCD/CCQN/ETIENNE PIERRE GIRAR.png",
            "Assets/Loc asset/sprites/CCCD/CCQN/npc-1.png",
            "Assets/Loc asset/sprites/CCCD/CCQN/npc-2.png",
            "Assets/Loc asset/sprites/CCCD/CCQN/npc-3.png",
            "Assets/Loc asset/sprites/CCCD/CCQN/npc-6.png",
            "Assets/Loc asset/sprites/CCCD/CCQN/npc-8.png",
            "Assets/Loc asset/sprites/CCCD/CCQN/npc-9.png",
            "Assets/Loc asset/sprites/CCCD/CCQN/npc-12.png"
        };

        string[] fallbackSurrenders = new string[]
        {
            "Assets/Loc asset/sprites/CCCD/Surrender Confirm/Victor Jean Mercier.png",
            "Assets/Loc asset/sprites/CCCD/Surrender Confirm/npc-6.png",
            "Assets/Loc asset/sprites/CCCD/Surrender Confirm/npc-2.png",
            "Assets/Loc asset/sprites/CCCD/Surrender Confirm/npc-3.png",
            "Assets/Loc asset/sprites/CCCD/Surrender Confirm/npc-8.png",
            "Assets/Loc asset/sprites/CCCD/Surrender Confirm/npc-9.png",
            "Assets/Loc asset/sprites/CCCD/Surrender Confirm/npc-12.png"
        };

        for (int i = 0; i < npcQueue.Count; i++)
        {
            var npc = npcQueue[i];
            if (npc == null) continue;

            string lowerName = (npc.npcName ?? "").ToLowerInvariant();
            if (npc.npcPrefab != null) lowerName += " " + npc.npcPrefab.name.ToLowerInvariant();

            if (npc.cccdTexture == null)
            {
                if (lowerName.Contains("victor"))
                    npc.cccdTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/CCQN/Victor Jean Mercier.png");
                else if (lowerName.Contains("trinh hoang long"))
                    npc.cccdTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/CCQN/Trinh Hoang Long.png");
                else if (lowerName.Contains("etienne"))
                    npc.cccdTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/CCQN/ETIENNE PIERRE GIRAR.png");

                if (npc.cccdTexture == null)
                    npc.cccdTexture = LoadTextureFromPath(fallbackCccds[i % fallbackCccds.Length]);
            }

            if (npc.surrenderConfirmTexture == null)
            {
                if (lowerName.Contains("victor"))
                    npc.surrenderConfirmTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/Surrender Confirm/Victor Jean Mercier.png");
                else if (lowerName.Contains("tran quoc hung"))
                    npc.surrenderConfirmTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/pass key/giay_thong_hanh_trần_quốc_hùng.png");

                if (npc.surrenderConfirmTexture == null)
                    npc.surrenderConfirmTexture = LoadTextureFromPath(fallbackSurrenders[i % fallbackSurrenders.Length]);
            }
        }
    }

    /// <summary>
    /// Tự động nạp file lồng tiếng tiếng Việt cho các NPC trong danh sách nếu chưa kéo thủ công
    /// </summary>
    public void EnsureVoiceClipsAssigned()
    {
        if (npcQueue == null) return;
        foreach (var npc in npcQueue)
        {
            // Boomer chỉ phát tiếng bíp bom hẹn giờ, không nạp giọng nói đè lên
            if (IsBoomerProfile(npc)) continue;

            if (npc.voiceArriveClip == null) npc.voiceArriveClip = npc.GetEffectiveArriveClip();
            if (npc.voiceApproveClip == null) npc.voiceApproveClip = npc.GetEffectiveApproveClip();
            if (npc.voiceDenyClip == null) npc.voiceDenyClip = npc.GetEffectiveDenyClip();
            if (npc.defenseVoiceClips == null || npc.defenseVoiceClips.Length == 0)
            {
                npc.defenseVoiceClips = new AudioClip[4];
                for (int i = 0; i < 4; i++)
                {
                    npc.defenseVoiceClips[i] = npc.GetEffectiveDefenseClip(i);
                }
            }
        }
    }

    private static bool IsBoomerProfile(NPCInspectionProfile npc)
    {
        if (npc == null) return false;
        if (npc.npcPrefab != null && npc.npcPrefab.GetComponentInChildren<NPCExploderBehavior>(true) != null)
            return true;
        string combined = ((npc.npcName ?? "") + " " + (npc.npcPrefab != null ? npc.npcPrefab.name : "")).ToLowerInvariant();
        return combined.Contains("boomer") || combined.Contains("exploder");
    }

    private static string NormalizeObjectName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        return raw.Trim().Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
    }

    private static Transform FindPointByNames(params string[] candidateNames)
    {
        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int c = 0; c < candidateNames.Length; c++)
        {
            string targetNorm = NormalizeObjectName(candidateNames[c]);
            if (string.IsNullOrEmpty(targetNorm)) continue;

            for (int i = 0; i < allTransforms.Length; i++)
            {
                Transform t = allTransforms[i];
                if (t != null && NormalizeObjectName(t.name) == targetNorm)
                {
                    return t;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Tự động tìm kiếm các điểm tọa độ có sẵn trong Scene (nhận diện không phân biệt hoa thường:
    /// inspectionZone, pass, dismiss, NPC_SpawnPoint...) nếu chưa kéo vào Inspector.
    /// </summary>
    public void EnsureSceneReferences()
    {
        // 1. Inspection Point (inspectionZone / InspectionZone)
        if (inspectionPoint == null)
        {
            inspectionPoint = FindPointByNames("inspectionZone", "InspectionPoint", "NPC_InspectionPoint");
            if (inspectionPoint == null)
            {
                Debug.LogWarning("[NPCInspectionManager] Chưa gán InspectionPoint và không tìm thấy 'inspectionZone' trong Scene.");
            }
        }

        // 2. Pass Destination (pass / PassDestination / NPC_PassDestination)
        if (passDestination == null)
        {
            passDestination = FindPointByNames("pass", "PassPoint", "PassDestination", "NPC_PassDestination");
        }

        // 3. Dismiss Destination (dismiss / DismissDestination / NPC_DismissDestination)
        if (dismissDestination == null)
        {
            dismissDestination = FindPointByNames("dismiss", "DismissPoint", "DismissDestination", "NPC_DismissDestination");
        }

        // 4. Spawn Point (NPC_SpawnPoint / npc spawn point; nếu chưa đặt riêng thì dùng tạm dismiss)
        if (spawnPoint == null)
        {
            spawnPoint = FindPointByNames("NPC_SpawnPoint", "NPCSpawnPoint", "NPCSpawn");
            if (spawnPoint == null && dismissDestination != null)
            {
                spawnPoint = dismissDestination;
                Debug.Log("[NPCInspectionManager] Chưa đặt 'NPC_SpawnPoint' riêng -> dùng tạm điểm 'dismiss' làm nơi xuất phát cho NPC.");
            }
            else if (spawnPoint == null)
            {
                Debug.LogWarning("[NPCInspectionManager] Chưa gán SpawnPoint ('NPC_SpawnPoint').");
            }
        }

        if (dismissDestination == null)
        {
            dismissDestination = spawnPoint;
        }
    }

    public static Vector3 GetSampledNavMeshPosition(Vector3 sourcePos, float maxDistance = 3.0f)
    {
        if (NavMesh.SamplePosition(sourcePos, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return sourcePos;
    }

    /// <summary>
    /// Tự động nạp mẫu 2 NPC (1 Người thường và 1 Đột biến) với đầy đủ ảnh CCCD và Surrender Confirm
    /// </summary>
    public void EnsureDefaultProfiles()
    {
        if (!autoPopulateDefaults) return;
        if (npcQueue != null && npcQueue.Count > 0) return;

        npcQueue = new List<NPCInspectionProfile>();

        // --- NPC 1: Human Survivor ---
        NPCInspectionProfile p1 = new NPCInspectionProfile();
        p1.npcName = "Nguyen Van An (ID: 079201004521)";
        p1.isMutant = false;
        p1.dialogueOnArrive = "Hello officer! I just escaped from the forest. Here are my ID and pass papers, please let me in!";
        p1.defenseDialogues = new string[]
        {
            "I am a normal human, officer! Look at me, I am not one of those mutants!",
            "I have my ID card and surrender confirm right here! Everything is legitimate, check it yourself!",
            "Please, it is freezing out here and monsters are prowling in the dark... don't leave me outside!",
            "I swear on my life, I am completely clean! No signs of infection at all!"
        };
        p1.dialogueOnApproved = "Thank you so much, officer! You saved my life!";
        p1.dialogueOnDenied = "No! Please don't leave me out here in the dark!!";

        // Load hình ảnh
        p1.cccdTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/CCQN/Victor Jean Mercier.png");
        if (p1.cccdTexture == null) p1.cccdTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/CCQN/npc-3.png");
        if (p1.cccdTexture == null) p1.cccdTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/CCQN/npc-1.png");

        p1.surrenderConfirmTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/Surrender Confirm/Victor Jean Mercier.png");
        if (p1.surrenderConfirmTexture == null) p1.surrenderConfirmTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/pass key/npc-1.png");
        if (p1.surrenderConfirmTexture == null) p1.surrenderConfirmTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/Surrender Confirm/npc-3.png");

        // Tìm prefab
        p1.npcPrefab = LoadPrefabFromPath("Assets/Phu-Asset/Prefabs/NPC/Victor Jean Mercier-Normal.prefab");
        if (p1.npcPrefab == null) p1.npcPrefab = LoadPrefabFromPath("Assets/Phu-Asset/Prefabs/NPC/Npc-Normal.prefab");
        if (p1.npcPrefab == null) p1.npcPrefab = LoadPrefabFromPath("Assets/Loc asset/Prefabs/NPC/npc_1 Variant.prefab");

        npcQueue.Add(p1);

        // --- NPC 2: Mutant Anomaly ---
        NPCInspectionProfile p2 = new NPCInspectionProfile();
        p2.npcName = "Trinh Hoang Long (ID: 031198007214)";
        p2.isMutant = true;
        p2.dialogueOnArrive = "Officer... I am... normal... Please let me in...";
        p2.defenseDialogues = new string[]
        {
            "I am... I am fine... See? I look completely normal... Just like you...",
            "Officer... why are you staring at me? My papers... they are real...",
            "Open the gate... quickly... It is so cold... I am starving... I mean, exhausted...",
            "Don't suspect me... I am human... I swear... Please..."
        };
        p2.dialogueOnApproved = "Heheheh... Thank... you... officer...";
        p2.dialogueOnDenied = "Grrrraaaagh! You will pay for this!";

        p2.cccdTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/CCQN/Trinh Hoang Long.png");
        if (p2.cccdTexture == null) p2.cccdTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/CCQN/npc-6.png");

        p2.surrenderConfirmTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/Surrender Confirm/npc-6.png");
        if (p2.surrenderConfirmTexture == null) p2.surrenderConfirmTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/pass key/giay_thong_hanh_trần_quốc_hùng.png");

        p2.npcPrefab = LoadPrefabFromPath("Assets/Phu-Asset/Prefabs/NPC/Trinh Hoang Long-SpinningLegs.prefab");
        if (p2.npcPrefab == null) p2.npcPrefab = LoadPrefabFromPath("Assets/Phu-Asset/Prefabs/NPC/Pham Minh Tuan-Glitch.prefab");
        if (p2.npcPrefab == null) p2.npcPrefab = LoadPrefabFromPath("Assets/Phu-Asset/Prefabs/NPC/NPC-Glitch.prefab");
        if (p2.npcPrefab == null) p2.npcPrefab = LoadPrefabFromPath("Assets/Phu-Asset/Prefabs/NPC/Vo Van Nam-HeadSpin.prefab");
        if (p2.npcPrefab == null) p2.npcPrefab = LoadPrefabFromPath("Assets/Phu-Asset/Prefabs/NPC/Npc-HeadSpin.prefab");
        if (p2.npcPrefab == null) p2.npcPrefab = LoadPrefabFromPath("Assets/Phu-Asset/Prefabs/NPC/Bui Hoang Viet-Boomer.prefab");
        if (p2.npcPrefab == null) p2.npcPrefab = LoadPrefabFromPath("Assets/Phu-Asset/Prefabs/NPC/Npc-Boomer.prefab");

        npcQueue.Add(p2);

        Debug.Log($"[NPCInspectionManager] Đã cấu hình mặc định {npcQueue.Count} lượt kiểm tra NPC cho Night 1!");
    }

    private Texture2D LoadTextureFromPath(string path)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
#else
        return null;
#endif
    }

    private GameObject LoadPrefabFromPath(string path)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
        return null;
#endif
    }

    /// <summary>
    /// Đảm bảo có DocumentInspectionUI. Không tự động tạo bàn/nút ở tọa độ map Night-1 cũ.
    /// </summary>
    public void EnsureDeskAndButtons()
    {
        // 1. Đảm bảo có DocumentInspectionUI
        if (DocumentInspectionUI.instance == null)
        {
            GameObject uiGO = new GameObject("DocumentInspectionUI_Manager");
            uiGO.AddComponent<DocumentInspectionUI>();
        }
    }

    /// <summary>
    /// Kích hoạt bắt đầu nhiệm vụ kiểm tra cổng gác (được TaskManager gọi khi xong 4 việc dọn dẹp)
    /// </summary>
    public void StartGateInspection()
    {
        if (isInspectionActive) return;
        isInspectionActive = true;
        currentNPCIndex = -1;

        Debug.Log("[NPCInspectionManager] BẮT ĐẦU NHIỆM VỤ KIỂM TRA CỔNG GÁC!");

        // Thông báo bằng âm thanh chuông hoặc điện thoại nếu có
        if (correctDecisionChime != null && AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(correctDecisionChime);
        }

        SpawnNextNPC();
    }

    /// <summary>
    /// Triệu hồi NPC tiếp theo trong hàng chờ tiến tới bốt gác
    /// </summary>
    public void SpawnNextNPC()
    {
        currentNPCIndex++;

        if (currentNPCIndex >= npcQueue.Count)
        {
            // Đã kiểm tra xong toàn bộ NPC trong đêm!
            CompleteInspectionTask();
            return;
        }

        currentProfile = npcQueue[currentNPCIndex];
        hasDecisionBeenMade = false;
        isCurrentNPCAtDesk = false;

        // Dừng hội thoại của NPC trước
        StopDefenseDialogueLoop();
        HideDialogue();

        if (InspectionDeskInteractable.instance != null)
        {
            InspectionDeskInteractable.instance.SetDocumentsAvailable(false);
        }

        if (currentNPCObject != null)
        {
            NPCBaseBehavior behavior = currentNPCObject.GetComponentInChildren<NPCBaseBehavior>();
            if (behavior != null) behavior.isInspecting = false;
        }

        // Hủy NPC cũ nếu còn
        if (currentNPCObject != null)
        {
            Destroy(currentNPCObject);
        }

        Vector3 spawnPos = (spawnPoint != null) ? spawnPoint.position : transform.position;
        spawnPos.y += spawnHeightOffset;
        Quaternion spawnRot = (spawnPoint != null) ? spawnPoint.rotation : transform.rotation;

        if (currentProfile.npcPrefab != null)
        {
            currentNPCObject = Instantiate(currentProfile.npcPrefab, spawnPos, spawnRot);
        }
        else
        {
            // Tạo NPC tạm thời nếu thiếu Prefab
            currentNPCObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            currentNPCObject.name = currentProfile.npcName;
            currentNPCObject.transform.position = spawnPos;
        }

        // BẢO VỆ CẤU TRÚC PREFAB:
        // Nếu Prefab có 1 GameObject rỗng bọc ngoài Model duy nhất (như Trinh Hoang Long-SpinningLegs):
        // Ta "mở bao bì", lấy Model thực sự làm root để không bị tách rời (split) giữa root và thân NPC!
        if (currentNPCObject.transform.childCount == 1 &&
            currentNPCObject.GetComponent<Animator>() == null &&
            currentNPCObject.GetComponent<LODGroup>() == null &&
            currentNPCObject.GetComponent<SkinnedMeshRenderer>() == null &&
            currentNPCObject.GetComponent<MeshRenderer>() == null)
        {
            Transform actualModel = currentNPCObject.transform.GetChild(0);
            actualModel.SetParent(null, true);
            Destroy(currentNPCObject);
            currentNPCObject = actualModel.gameObject;
        }

        currentNPCObject.name = currentProfile.npcName;
        currentNPCObject.tag = "NPC";

        // Nếu có container "Model" con bên trong, đảm bảo localPosition chuẩn (0,0,0)
        Transform modelChild = currentNPCObject.transform.Find("Model");
        if (modelChild != null)
        {
            modelChild.localPosition = Vector3.zero;
        }

        // Vô hiệu hóa và dọn dẹp các NavMeshAgent ở object con (nếu có) để tránh xung đột
        NavMeshAgent[] allAgents = currentNPCObject.GetComponentsInChildren<NavMeshAgent>(true);
        foreach (var a in allAgents)
        {
            if (a.gameObject != currentNPCObject)
            {
                a.enabled = false;
                Destroy(a);
            }
        }

        // Vô hiệu hóa trọng lực physics để không bị kéo lọt xuống sàn
        Rigidbody[] rbs = currentNPCObject.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs)
        {
            rb.isKinematic = true;
        }

        // Đảm bảo có CapsuleCollider để đạn của súng có thể bắn trúng
        CapsuleCollider col = currentNPCObject.GetComponent<CapsuleCollider>();
        if (col == null) col = currentNPCObject.GetComponentInChildren<CapsuleCollider>();
        if (col == null)
        {
            col = currentNPCObject.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.35f;
        }

        // Thiết lập NavMeshAgent duy nhất trên ROOT NPC
        currentAgent = currentNPCObject.GetComponent<NavMeshAgent>();
        if (currentAgent == null) currentAgent = currentNPCObject.AddComponent<NavMeshAgent>();
        currentAgent.enabled = true;
        currentAgent.stoppingDistance = 0.5f;

        NPCBaseBehavior npcBehavior = currentNPCObject.GetComponentInChildren<NPCBaseBehavior>();
        if (npcBehavior != null)
        {
            currentAgent.baseOffset = 0f;
            currentAgent.speed = npcBehavior.walkSpeed;
        }
        else
        {
            currentAgent.speed = 2.4f;
            currentAgent.baseOffset = 0f;
        }

        // Reset vị trí Agent chuẩn xác trên NavMesh (nếu đã Bake NavMesh) hoặc chuyển sang chế độ đi bộ trực tiếp
        currentNPCObject.transform.position = spawnPos;
        currentNPCObject.transform.rotation = spawnRot;

        bool hasNavMesh = NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 8.0f, NavMesh.AllAreas);
        if (hasNavMesh)
        {
            currentAgent.Warp(hit.position);
        }
        else
        {
            currentAgent.enabled = false;
            currentNPCObject.transform.position = SnapPositionToGround(spawnPos) + Vector3.up * spawnHeightOffset;
        }

        // Đảm bảo có NPCHealth để có thể bắn bằng súng
        NPCHealth health = currentNPCObject.GetComponent<NPCHealth>();
        if (health == null) health = currentNPCObject.AddComponent<NPCHealth>();
        if (health.onDisappear == null) health.onDisappear = new UnityEngine.Events.UnityEvent();
        health.onDisappear.RemoveListener(OnNPCShot);
        health.onDisappear.AddListener(OnNPCShot);

        Vector3 targetPos = (inspectionPoint != null) ? inspectionPoint.position : transform.position;
        if (hasNavMesh && currentAgent.enabled && currentAgent.isOnNavMesh)
        {
            targetPos = GetSampledNavMeshPosition(targetPos, 8.0f);
            currentAgent.isStopped = false;
            currentAgent.SetDestination(targetPos);
        }

        Animator anim = currentNPCObject.GetComponentInChildren<Animator>();
        if (anim != null) anim.SetBool("isWalk", true);

        StartCoroutine(WaitForNPCArrivalRoutine(targetPos, currentAgent.speed));

        Debug.Log($"[NPCInspectionManager] NPC [{currentProfile.npcName}] đang tiến về bốt gác...");
    }

    private Vector3 SnapPositionToGround(Vector3 pos)
    {
        Ray ray = new Ray(pos + Vector3.up * 3.0f, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, 10.0f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float bestDist = float.MaxValue;
        float bestY = pos.y;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i].collider;
            if (c == null) continue;
            if (currentNPCObject != null && c.transform.IsChildOf(currentNPCObject.transform)) continue;
            if (c.CompareTag("Player") || c.CompareTag("NPC")) continue;
            if (hits[i].distance < bestDist)
            {
                bestDist = hits[i].distance;
                bestY = hits[i].point.y;
            }
        }
        return new Vector3(pos.x, bestY, pos.z);
    }

    private IEnumerator MoveNPCDirectlyRoutine(Vector3 targetPos, float moveSpeed, float maxDuration)
    {
        float elapsed = 0f;
        while (currentNPCObject != null && elapsed < maxDuration)
        {
            Vector3 curr = currentNPCObject.transform.position;
            Vector3 flatCurr = new Vector3(curr.x, 0f, curr.z);
            Vector3 flatTarget = new Vector3(targetPos.x, 0f, targetPos.z);
            float dist = Vector3.Distance(flatCurr, flatTarget);
            if (dist <= 0.45f) break;

            Vector3 dir = (flatTarget - flatCurr).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                currentNPCObject.transform.rotation = Quaternion.Slerp(currentNPCObject.transform.rotation, targetRot, Time.deltaTime * 8f);
            }

            Vector3 nextXZ = Vector3.MoveTowards(flatCurr, flatTarget, moveSpeed * Time.deltaTime);
            Vector3 snapped = SnapPositionToGround(new Vector3(nextXZ.x, curr.y, nextXZ.z));
            currentNPCObject.transform.position = snapped + Vector3.up * spawnHeightOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitForNPCArrivalRoutine(Vector3 fallbackTargetPos, float walkSpeed)
    {
        // Nếu Scene đã Bake NavMesh -> dùng NavMeshAgent
        if (currentAgent != null && currentAgent.enabled && currentAgent.isOnNavMesh)
        {
            yield return new WaitForSeconds(0.3f);
            while (currentAgent != null && currentNPCObject != null)
            {
                if (currentAgent.enabled && currentAgent.isOnNavMesh && !currentAgent.pathPending)
                {
                    if (!currentAgent.hasPath || currentAgent.remainingDistance <= currentAgent.stoppingDistance + 0.35f)
                    {
                        OnNPCArrivedAtDesk();
                        yield break;
                    }
                }
                yield return new WaitForSeconds(0.2f);
            }
            yield break;
        }

        // Dự phòng: Nếu chưa kịp Bake NavMeshSurface, cho NPC đi bộ mượt thẳng tới điểm inspectionZone
        yield return StartCoroutine(MoveNPCDirectlyRoutine(fallbackTargetPos, Mathf.Max(1.8f, walkSpeed), 45f));
        if (currentNPCObject != null)
        {
            OnNPCArrivedAtDesk();
        }
    }

    /// <summary>
    /// Khi NPC đã tới trước cửa sổ bốt gác
    /// </summary>
    private void OnNPCArrivedAtDesk()
    {
        isCurrentNPCAtDesk = true;

        if (currentAgent != null && currentAgent.enabled && currentAgent.isOnNavMesh)
        {
            currentAgent.isStopped = true;
            currentAgent.updateRotation = false;
        }

        Animator anim = currentNPCObject.GetComponentInChildren<Animator>();
        if (anim != null) anim.SetBool("isWalk", false);

        // Quay mặt về phía người chơi hoặc bốt gác
        if (currentNPCObject != null)
        {
            Vector3 lookTarget = currentNPCObject.transform.position + currentNPCObject.transform.forward;
            if (Camera.main != null)
            {
                lookTarget = Camera.main.transform.position;
            }
            else
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) lookTarget = playerObj.transform.position;
            }

            Vector3 lookDir = (new Vector3(lookTarget.x, currentNPCObject.transform.position.y, lookTarget.z) - currentNPCObject.transform.position).normalized;
            if (lookDir != Vector3.zero)
            {
                currentNPCObject.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        // Kích hoạt tập giấy tờ trên bàn để người chơi bấm E soi
        if (InspectionDeskInteractable.instance != null)
        {
            InspectionDeskInteractable.instance.SetDocumentsAvailable(true);
        }

        // Kích hoạt các hành vi tương ứng của NPC (Người thường & Đột biến) thông qua script cha NPCBaseBehavior
        NPCBaseBehavior npcBehavior = currentNPCObject.GetComponentInChildren<NPCBaseBehavior>();
        if (npcBehavior != null)
        {
            npcBehavior.StartInspection();
        }

        Debug.Log($"[NPCInspectionManager] NPC [{currentProfile.npcName}] ĐÃ ĐẾN VẠCH KIỂM TRA. Giấy tờ đã sẵn sàng!");

        // Bắt đầu chuỗi hội thoại biện hộ / thanh minh liên tục của NPC
        StartDefenseDialogueLoop();
    }

    /// <summary>
    /// Mở/hiển thị giấy tờ của NPC hiện tại (lên 2 màn hình Secure Control Panel và/hoặc UI toàn màn hình)
    /// </summary>
    public void OpenCurrentNPCDocuments()
    {
        if (!isCurrentNPCAtDesk || currentProfile == null) return;

        if (documentOpenSound != null && AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(documentOpenSound);
        }

        bool displayedOnControlPanel = false;
        bool shouldOpenFullscreen = true;

        if (InspectionDeskInteractable.instance != null && InspectionDeskInteractable.instance.HasControlPanelScreens)
        {
            InspectionDeskInteractable.instance.DisplayDocumentsOnScreens(
                currentProfile.cccdTexture,
                currentProfile.surrenderConfirmTexture
            );
            displayedOnControlPanel = true;
            shouldOpenFullscreen = InspectionDeskInteractable.instance.alsoOpenFullscreenUI;
        }

        if (shouldOpenFullscreen && DocumentInspectionUI.instance != null)
        {
            DocumentInspectionUI.instance.OpenInspection(
                currentProfile.cccdTexture,
                currentProfile.surrenderConfirmTexture,
                currentProfile.npcName
            );
        }
        else if (displayedOnControlPanel)
        {
            Debug.Log($"[NPCInspectionManager] Đã hiển thị CCCD & Surrender Confirm của [{currentProfile.npcName}] lên 2 màn hình Secure Control Panel!");
        }
    }

    /// <summary>
    /// Người chơi bấm CHO QUA (APPROVE PASS)
    /// </summary>
    public void ApproveCurrentNPC()
    {
        if (hasDecisionBeenMade || !isCurrentNPCAtDesk) return;
        hasDecisionBeenMade = true;

        StopDefenseDialogueLoop();
        AudioClip voice = (currentProfile != null && !IsBoomerProfile(currentProfile)) ? currentProfile.GetEffectiveApproveClip() : null;
        string approveText = (currentProfile != null && !string.IsNullOrEmpty(currentProfile.dialogueOnApproved))
            ? currentProfile.dialogueOnApproved
            : (currentProfile != null && currentProfile.isMutant
                ? "Heheheh... Thank... you... officer..."
                : "Thank you so much, officer! You saved my life!");

        float dur = (voice != null && voice.length > 0) ? Mathf.Max(3.5f, voice.length + 1.0f) : 4.0f;
        ShowDialogue(currentProfile != null ? currentProfile.npcName : "Citizen", approveText, dur, voice);

        if (gateBuzzerSound != null && AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(gateBuzzerSound);
        }

        if (currentProfile.isMutant)
        {
            // Nếu là NPC Jumper và chưa kịp jumpscare, kích hoạt vồ ngay lập tức!
            NPCJumperBehavior jumper = currentNPCObject != null ? currentNPCObject.GetComponentInChildren<NPCJumperBehavior>() : null;
            if (jumper != null && !jumper.hasTriggeredScare)
            {
                jumper.TriggerImmediateJumpscare();
                return;
            }

            // NGUY HIỂM: Cho nhầm đột biến vào căn cứ!
            Debug.LogWarning("[NPCInspectionManager] CẢNH BÁO: Người chơi đã cho ĐỘT BIẾN qua cổng!");
            TriggerBreachAlarm();
            StartCoroutine(MutantAttackRoutine());
        }
        else
        {
            // ĐÚNG ĐẮN: Người thường được vào an toàn
            Debug.Log("[NPCInspectionManager] CHUẨN XÁC: Cho công dân bình thường qua cổng an toàn.");
            if (correctDecisionChime != null && AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(correctDecisionChime);
            }

            StartCoroutine(NPCWalkThroughGateRoutine());
        }
    }

    /// <summary>
    /// Người chơi bấm TỪ CHỐI (REJECT DENY)
    /// </summary>
    public void RejectCurrentNPC()
    {
        if (hasDecisionBeenMade || !isCurrentNPCAtDesk) return;
        hasDecisionBeenMade = true;

        StopDefenseDialogueLoop();
        AudioClip voice = (currentProfile != null && !IsBoomerProfile(currentProfile)) ? currentProfile.GetEffectiveDenyClip() : null;
        string denyText = (currentProfile != null && !string.IsNullOrEmpty(currentProfile.dialogueOnDenied))
            ? currentProfile.dialogueOnDenied
            : (currentProfile != null && currentProfile.isMutant
                ? "Grrrraaaagh! You will pay for this!"
                : "No! Please don't leave me out here in the dark!!");

        float dur = (voice != null && voice.length > 0) ? Mathf.Max(3.5f, voice.length + 1.0f) : 4.0f;
        // KIỂM TRA JUMPSCARE TẤM HÌNH KINH DỊ CỦA NPC CHÓ SỦA (> 3 TIẾNG SỦA):
        NPCDogBarkBehavior dogBark = currentNPCObject != null ? currentNPCObject.GetComponentInChildren<NPCDogBarkBehavior>() : null;
        if (dogBark != null && dogBark.ShouldJumpscareOnDeny())
        {
            Debug.Log("[NPCInspectionManager] Người chơi bấm DENY và bị JUMPSCARE bằng tấm hình kinh dị!");
            if (correctDecisionChime != null && AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(correctDecisionChime);
            }

            dogBark.TriggerImageJumpscare(() =>
            {
                StartCoroutine(MutantFleesRoutine());
            });
            return;
        }

        // KIỂM TRA JUMPSCARE CỦA NPC JUMPER KHI BẤM TỪ CHỐI:
        NPCJumperBehavior jumper = currentNPCObject != null ? currentNPCObject.GetComponentInChildren<NPCJumperBehavior>() : null;
        if (jumper != null && !jumper.hasTriggeredScare)
        {
            Debug.Log("[NPCInspectionManager] Người chơi bấm DENY và bị NPC Jumper vồ thẳng vào mặt!");
            if (correctDecisionChime != null && AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(correctDecisionChime);
            }
            jumper.TriggerImmediateJumpscare();
            return;
        }

        // KIỂM TRA NPC LƠ LỬNG (FLOATING): KHI BẤM TỪ CHỐI -> BAY VÒNG VÒNG RỒI BAY THẲNG LÊN TRỜI BIẾN MẤT
        NPCFloatingBehavior floating = currentNPCObject != null ? currentNPCObject.GetComponentInChildren<NPCFloatingBehavior>() : null;
        if (floating != null)
        {
            Debug.Log("[NPCInspectionManager] CHUẨN XÁC: Đã từ chối NPC Floating -> Bay lơ lửng vòng vòng rồi bay lên trời biến mất!");
            ShowDialogue(currentProfile != null ? currentProfile.npcName : "Floating Mutant", denyText, dur, voice);
            if (correctDecisionChime != null && AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(correctDecisionChime);
            }
            if (InspectionDeskInteractable.instance != null)
            {
                InspectionDeskInteractable.instance.SetDocumentsAvailable(false);
            }

            floating.TriggerDenyFlyAway(() =>
            {
                if (currentNPCObject != null)
                {
                    Destroy(currentNPCObject);
                }
                SpawnNextNPC();
            });
            return;
        }

        if (currentProfile.isMutant || (dogBark != null && dogBark.isMutant))
        {
            // ĐÚNG ĐẮN: Phát hiện và đuổi đột biến
            Debug.Log("[NPCInspectionManager] CHUẨN XÁC: Đã phát hiện và từ chối sinh vật đột biến!");
            ShowDialogue(currentProfile != null ? currentProfile.npcName : "Mutant", denyText, dur, voice);
            if (correctDecisionChime != null && AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(correctDecisionChime);
            }

            StartCoroutine(MutantFleesRoutine());
        }
        else
        {
            // Nhầm lẫn: Đuổi người thường
            Debug.LogWarning("[NPCInspectionManager] Bạn đã từ chối một người bình thường.");
            ShowDialogue(currentProfile != null ? currentProfile.npcName : "Citizen", denyText, dur, voice);
            StartCoroutine(NPCDismissedRoutine());
        }
    }

    /// <summary>
    /// Khi NPC bị người chơi bắn hạ bằng súng
    /// </summary>
    public void OnNPCShot()
    {
        if (hasDecisionBeenMade) return;
        hasDecisionBeenMade = true;

        StopDefenseDialogueLoop();
        HideDialogue();

        if (currentProfile != null && currentProfile.isMutant)
        {
            Debug.Log("[NPCInspectionManager] TUYỆT VỜI! Bạn đã dùng súng tiêu diệt sinh vật đột biến tại chỗ!");
            if (correctDecisionChime != null && AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(correctDecisionChime);
            }
        }
        else
        {
            Debug.LogWarning("[NPCInspectionManager] Bạn đã bắn nhầm một người dân vô tội!");
        }

        if (InspectionDeskInteractable.instance != null)
        {
            InspectionDeskInteractable.instance.SetDocumentsAvailable(false);
        }

        if (currentNPCObject != null)
        {
            NPCBaseBehavior behavior = currentNPCObject.GetComponentInChildren<NPCBaseBehavior>();
            if (behavior != null) behavior.isInspecting = false;
        }

        // Chờ 2.5s rồi gọi NPC tiếp theo
        Invoke(nameof(SpawnNextNPC), 2.5f);
    }

    /// <summary>
    /// Khi NPC Boomer (Exploder) tự nổ tung tại bốt gác:
    /// Tự động tính là đã Từ chối (Deny), tắt toàn bộ log thoại/voice và gọi NPC tiếp theo.
    /// </summary>
    public void OnNPCExploded()
    {
        if (hasDecisionBeenMade) return;
        hasDecisionBeenMade = true;
        isCurrentNPCAtDesk = false;

        StopDefenseDialogueLoop();
        HideDialogue();

        if (currentNPCObject != null)
        {
            AudioSource[] srcs = currentNPCObject.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < srcs.Length; i++)
            {
                if (srcs[i] != null) srcs[i].Stop();
            }
        }

        if (InspectionDeskInteractable.instance != null)
        {
            InspectionDeskInteractable.instance.SetDocumentsAvailable(false);
        }

        Debug.Log("💥 [NPCInspectionManager] NPC Boomer đã nổ tung -> Tự động tính là Deny, tắt thoại và chuyển sang NPC tiếp theo!");

        if (currentNPCObject != null)
        {
            Destroy(currentNPCObject, 0.2f);
            currentNPCObject = null;
        }

        Invoke(nameof(SpawnNextNPC), 2.0f);
    }

    private IEnumerator MoveNPCOutAndDestroyRoutine(Transform destinationTransform, float speed, float maxSafetyTimeout = 45f)
    {
        if (InspectionDeskInteractable.instance != null)
        {
            InspectionDeskInteractable.instance.SetDocumentsAvailable(false);
        }

        if (currentNPCObject != null)
        {
            NPCBaseBehavior behavior = currentNPCObject.GetComponentInChildren<NPCBaseBehavior>();
            if (behavior != null) behavior.isInspecting = false;
        }

        if (currentNPCObject != null && destinationTransform != null)
        {
            Animator anim = currentNPCObject.GetComponentInChildren<Animator>();
            if (anim != null) anim.SetBool("isWalk", true);

            if (currentAgent != null && currentAgent.enabled && currentAgent.isOnNavMesh)
            {
                Vector3 target = GetSampledNavMeshPosition(destinationTransform.position, 8.0f);
                currentAgent.updateRotation = true;
                currentAgent.isStopped = false;
                currentAgent.speed = speed;
                currentAgent.SetDestination(target);

                // Chờ 0.25s để NavMeshAgent tính lộ trình
                yield return new WaitForSeconds(0.25f);

                float elapsed = 0f;
                while (currentNPCObject != null && currentAgent != null && elapsed < maxSafetyTimeout)
                {
                    if (!currentAgent.enabled || !currentAgent.isOnNavMesh) break;

                    Vector3 curr = currentNPCObject.transform.position;
                    float flatDist = Vector2.Distance(new Vector2(curr.x, curr.z), new Vector2(target.x, target.z));
                    if (flatDist <= Mathf.Max(0.75f, currentAgent.stoppingDistance + 0.35f))
                    {
                        break;
                    }

                    if (!currentAgent.pathPending && currentAgent.hasPath &&
                        currentAgent.remainingDistance <= currentAgent.stoppingDistance + 0.45f)
                    {
                        break;
                    }

                    elapsed += 0.15f;
                    yield return new WaitForSeconds(0.15f);
                }
            }
            else
            {
                // Chế độ đi bộ trực tiếp (chưa bake NavMesh): đi tới sát điểm đích mới biến mất
                yield return StartCoroutine(MoveNPCDirectlyRoutine(destinationTransform.position, speed, maxSafetyTimeout));
            }
        }
        else
        {
            yield return new WaitForSeconds(2.0f);
        }

        if (currentNPCObject != null)
        {
            Destroy(currentNPCObject);
        }

        SpawnNextNPC();
    }

    private IEnumerator NPCWalkThroughGateRoutine()
    {
        yield return StartCoroutine(MoveNPCOutAndDestroyRoutine(passDestination, 2.2f, 45f));
    }

    private IEnumerator NPCDismissedRoutine()
    {
        yield return StartCoroutine(MoveNPCOutAndDestroyRoutine(dismissDestination, 2.0f, 45f));
    }

    private IEnumerator MutantFleesRoutine()
    {
        yield return StartCoroutine(MoveNPCOutAndDestroyRoutine(dismissDestination, 4.5f, 45f));
    }

    private IEnumerator MutantAttackRoutine()
    {
        // Đèn chớp tắt báo động
        if (TaskManager.instance != null)
        {
            TaskManager.instance.FlickerFlashlight(5, 0.08f, 0.8f);
        }

        yield return StartCoroutine(MoveNPCOutAndDestroyRoutine(passDestination, 5.0f, 45f));
    }

    private void TriggerBreachAlarm()
    {
        if (alarmBreachSound != null && AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(alarmBreachSound);
        }
        else if (AudioManager.instance != null && gateBuzzerSound != null)
        {
            AudioManager.instance.PlaySFX(gateBuzzerSound);
        }
    }

    /// <summary>
    /// Khi đã hoàn thành kiểm tra toàn bộ danh sách NPC
    /// </summary>
    public void CompleteInspectionTask()
    {
        isInspectionActive = false;
        Debug.Log("[NPCInspectionManager] ĐÃ HOÀN THÀNH TOÀN BỘ KIỂM TRA CỔNG GÁC!");

        if (InspectionDeskInteractable.instance != null)
        {
            InspectionDeskInteractable.instance.SetDocumentsAvailable(false);
        }

        if (currentNPCObject != null)
        {
            NPCBaseBehavior behavior = currentNPCObject.GetComponentInChildren<NPCBaseBehavior>();
            if (behavior != null) behavior.isInspecting = false;
        }

        if (TaskManager.instance != null)
        {
            TaskManager.instance.CompleteGateInspection();
        }
    }

    // ================= HỆ THỐNG PHỤ ĐỀ & LỜI THOẠI BIỆN HỘ CỦA NPC =================

    /// <summary>
    /// Đảm bảo giao diện phụ đề đối thoại tồn tại trên màn hình
    /// </summary>
    public void EnsureDialogueUI()
    {
        if (dialoguePanel != null && dialogueContentText != null) return;

        Canvas targetCanvas = null;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (var c in canvases)
        {
            if (c.gameObject.name == "NPCDialogue_Canvas")
            {
                targetCanvas = c;
                break;
            }
        }

        if (targetCanvas == null)
        {
            GameObject canvasGO = new GameObject("NPCDialogue_Canvas");
            targetCanvas = canvasGO.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 160; // Nằm trên cả DocumentInspectionUI (150) để người chơi vừa xem hồ sơ vừa đọc lời thanh minh

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
        }
        else
        {
            targetCanvas.gameObject.SetActive(true);
        }

        // Tạo khung Panel phụ đề nằm sát đáy màn hình
        GameObject panelGO = new GameObject("NPCDialogue_Panel");
        panelGO.transform.SetParent(targetCanvas.transform, false);

        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0f);
        panelRT.anchorMax = new Vector2(0.5f, 0f);
        panelRT.pivot = new Vector2(0.5f, 0f);
        panelRT.anchoredPosition = new Vector2(0f, 65f);
        panelRT.sizeDelta = new Vector2(1200f, 100f);

        dialogueCanvasGroup = panelGO.AddComponent<CanvasGroup>();
        dialogueCanvasGroup.alpha = 1f;
        dialogueCanvasGroup.blocksRaycasts = false;
        dialogueCanvasGroup.interactable = false;

        // Tên người nói (Chữ trắng căn giữa)
        GameObject nameGO = new GameObject("SpeakerNameText");
        nameGO.transform.SetParent(panelGO.transform, false);
        RectTransform nameRT = nameGO.AddComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 1f);
        nameRT.anchorMax = new Vector2(1f, 1f);
        nameRT.pivot = new Vector2(0.5f, 1f);
        nameRT.anchoredPosition = new Vector2(0f, 0f);
        nameRT.sizeDelta = new Vector2(0f, 32f);

        dialogueNameText = nameGO.AddComponent<TextMeshProUGUI>();
        dialogueNameText.fontSize = 22f;
        dialogueNameText.fontStyle = FontStyles.Bold;
        dialogueNameText.color = Color.white;
        dialogueNameText.alignment = TextAlignmentOptions.Center;
        dialogueNameText.raycastTarget = false;

        // Nội dung lời thoại (Dòng chữ trắng chạy căn giữa màn hình, không khung hộp)
        GameObject contentGO = new GameObject("DialogueContentText");
        contentGO.transform.SetParent(panelGO.transform, false);
        RectTransform contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 0f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = new Vector2(0f, -34f);
        contentRT.sizeDelta = new Vector2(0f, -34f);

        dialogueContentText = contentGO.AddComponent<TextMeshProUGUI>();
        dialogueContentText.fontSize = 23f;
        dialogueContentText.fontStyle = FontStyles.Normal;
        dialogueContentText.color = Color.white;
        dialogueContentText.alignment = TextAlignmentOptions.Top;
        dialogueContentText.textWrappingMode = TMPro.TextWrappingModes.Normal;
        dialogueContentText.raycastTarget = false;

        TMP_FontAsset subtitleFont = TMP_Settings.defaultFontAsset;
        if (subtitleFont == null) subtitleFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Roboto-Bold SDF");
        if (subtitleFont != null)
        {
            dialogueNameText.font = subtitleFont;
            dialogueContentText.font = subtitleFont;
        }

        // Thêm bóng nhẹ để chữ trắng nổi rõ trên mọi nền sáng/tối
        Shadow nameShadow = nameGO.AddComponent<Shadow>();
        nameShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        nameShadow.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow contentShadow = contentGO.AddComponent<Shadow>();
        contentShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        contentShadow.effectDistance = new Vector2(1.5f, -1.5f);

        dialoguePanel = panelGO;
        dialoguePanel.SetActive(false);
    }

    /// <summary>
    /// Hiển thị lời thoại kèm hiệu ứng gõ chữ và phát âm thanh
    /// </summary>
    public void ShowDialogue(string speakerName, string message, float duration = 5f, AudioClip voiceClip = null)
    {
        // 1. Luôn luôn phát giọng nói lồng tiếng 3D nếu có audio clip (không phụ thuộc vào text)
        if (voiceClip != null)
        {
            PlayVoiceClip(voiceClip);
        }

        // 2. Nếu không có nội dung chữ thì không cần mở khung phụ đề
        if (string.IsNullOrEmpty(message)) return;

        EnsureDialogueUI();
        if (dialoguePanel == null || dialogueContentText == null) return;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeDialogueRoutine(speakerName, message, duration));
    }

    private IEnumerator TypeDialogueRoutine(string speakerName, string fullMessage, float duration)
    {
        dialoguePanel.SetActive(true);
        if (dialogueCanvasGroup != null) dialogueCanvasGroup.alpha = 1f;

        if (dialogueNameText != null)
        {
            dialogueNameText.text = speakerName;
        }

        dialogueContentText.text = "";

        // Tốc độ gõ chữ mượt mà
        float charDelay = 0.02f;
        for (int i = 0; i < fullMessage.Length; i++)
        {
            dialogueContentText.text = fullMessage.Substring(0, i + 1);

            char c = fullMessage[i];
            if (c == '.' || c == '!' || c == '?' || c == ',')
            {
                yield return new WaitForSeconds(0.08f);
            }
            else
            {
                yield return new WaitForSeconds(charDelay);
            }
        }

        dialogueContentText.text = fullMessage;

        yield return new WaitForSeconds(duration);

        if (dialogueCanvasGroup != null)
        {
            float elapsed = 0f;
            float fadeDuration = 0.4f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                dialogueCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        typingCoroutine = null;
    }

    /// <summary>
    /// Phát âm thanh giọng nói 3D chân thực phát ra từ chính NPC đang đứng ngoài cửa kính bốt gác
    /// </summary>
    public void PlayVoiceClip(AudioClip clip)
    {
        if (clip == null) return;

        if (currentNPCObject != null)
        {
            AudioSource src = currentNPCObject.GetComponent<AudioSource>();
            if (src == null)
            {
                src = currentNPCObject.AddComponent<AudioSource>();
            }

            src.spatialBlend = 0.65f; // Âm thanh định hướng 3D vừa phải để nghe rõ giọng nói qua cửa kính bốt gác
            src.minDistance = 2.5f;
            src.maxDistance = 25f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.pitch = 1.0f;
            src.clip = clip;
            src.volume = 1.0f;
            src.Play();
            return;
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : transform.position);
        }
    }

    /// <summary>
    /// Ẩn ngay lập tức khung thoại
    /// </summary>
    public void HideDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Bắt đầu chuỗi hội thoại biện hộ / thanh minh liên tục khi NPC đứng chờ tại bốt gác
    /// </summary>
    public void StartDefenseDialogueLoop()
    {
        StopDefenseDialogueLoop();
        defenseCycleCoroutine = StartCoroutine(DefenseDialogueLoopRoutine());
    }

    /// <summary>
    /// Dừng chuỗi hội thoại biện hộ khi người chơi đã đưa ra quyết định
    /// </summary>
    public void StopDefenseDialogueLoop()
    {
        if (defenseCycleCoroutine != null)
        {
            StopCoroutine(defenseCycleCoroutine);
            defenseCycleCoroutine = null;
        }
    }

    private IEnumerator DefenseDialogueLoopRoutine()
    {
        if (currentProfile == null) yield break;

        // 1. Câu thoại đầu tiên khi vừa tới trước cửa kính
        string firstLine = !string.IsNullOrEmpty(currentProfile.dialogueOnArrive)
            ? currentProfile.dialogueOnArrive
            : (currentProfile.isMutant
                ? "Officer... I am... normal... Please let me in..."
                : "Hello officer! Here are my papers, please check them!");

        AudioClip arriveVoice = IsBoomerProfile(currentProfile) ? null : currentProfile.GetEffectiveArriveClip();
        float arriveShowTime = (arriveVoice != null && arriveVoice.length > 0) ? Mathf.Max(4.5f, arriveVoice.length + 1.2f) : 5.0f;
        ShowDialogue(currentProfile.npcName, firstLine, arriveShowTime, arriveVoice);

        // Chờ hết câu thoại đầu tiên + nghỉ trước khi bắt đầu chuỗi thanh minh
        yield return new WaitForSeconds(arriveShowTime + 3.0f);

        string[] defenseLines = currentProfile.GetEffectiveDefenseDialogues();
        int lineIdx = 0;

        // Vòng lặp liên tục thanh minh / biện hộ khi đang đứng chờ quyết định
        while (isCurrentNPCAtDesk && !hasDecisionBeenMade && currentNPCObject != null && currentNPCObject.activeInHierarchy)
        {
            string line = (defenseLines != null && defenseLines.Length > 0)
                ? defenseLines[lineIdx % defenseLines.Length]
                : "";

            AudioClip voice = IsBoomerProfile(currentProfile) ? null : currentProfile.GetEffectiveDefenseClip(lineIdx);
            lineIdx++;

            float showTime = (voice != null && voice.length > 0) ? Mathf.Max(4.5f, voice.length + 1.2f) : 5.0f;
            ShowDialogue(currentProfile.npcName, line, showTime, voice);

            // Giữ thoại xong nghỉ 3.5s trước câu tiếp theo
            yield return new WaitForSeconds(showTime + 3.5f);
        }
    }

    private void OnDrawGizmos()
    {
        // 1. Spawn Point (Màu Xanh Biển)
        if (spawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.6f);
            Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward * 2.5f);
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.Label(spawnPoint.position + Vector3.up * 1.2f, "🔵 [NPC Spawn Point]");
#endif
        }

        // 2. Inspection Point (Màu Vàng)
        if (inspectionPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(inspectionPoint.position, 0.6f);
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(inspectionPoint.position + Vector3.up * 1.2f, "🟡 [Inspection Zone]");
#endif
            // Đường đi từ Spawn tới Inspection Point
            if (spawnPoint != null)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
                Gizmos.DrawLine(spawnPoint.position, inspectionPoint.position);
            }
        }

        // 3. Pass Destination (Màu Xanh Lá)
        if (passDestination != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(passDestination.position, 0.6f);
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(passDestination.position + Vector3.up * 1.2f, "🟢 [Pass Destination]");
#endif
            // Tuyến đường cho qua từ Inspection tới Pass
            if (inspectionPoint != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                Gizmos.DrawLine(inspectionPoint.position, passDestination.position);
            }
        }

        // 4. Dismiss Destination (Màu Đỏ)
        if (dismissDestination != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(dismissDestination.position, 0.6f);
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(dismissDestination.position + Vector3.up * 1.2f, "🔴 [Dismiss Destination]");
#endif
            // Tuyến đường đuổi đi từ Inspection tới Dismiss
            if (inspectionPoint != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                Gizmos.DrawLine(inspectionPoint.position, dismissDestination.position);
            }
        }
    }
}
