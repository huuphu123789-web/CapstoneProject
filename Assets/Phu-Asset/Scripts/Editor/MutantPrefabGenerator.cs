#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Editor Utility to automatically generate 3 new Mutant NPC Prefabs:
/// 1. NPC-Jumper (The Jumper)
/// 2. NPC-Crawler (The Crawler)
/// 3. NPC-Screamer (The Screamer)
/// </summary>
public static class MutantPrefabGenerator
{
    private const string BasePrefabPath = "Assets/Phu-Asset/Prefabs/NPC/Victor Jean Mercier-Normal.prefab";
    private const string FallbackBasePrefabPath = "Assets/Phu-Asset/Prefabs/NPC/Npc-Normal.prefab";
    private const string OutputFolder = "Assets/Phu-Asset/Prefabs/NPC";

    public static GameObject GetBasePrefab()
    {
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
        if (basePrefab == null) basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FallbackBasePrefabPath);
        return basePrefab;
    }

    public static GameObject GetJumperPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/Nguyen Van An-Jumper.prefab");
        if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-Jumper.prefab");
        return prefab;
    }

    public static GameObject GetCrawlerPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/Tran Quoc Hung-Crawler.prefab");
        if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-Crawler.prefab");
        return prefab;
    }

    public static GameObject GetScreamerPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/ETIENNE PIERRE GIRAR-Screamer.prefab");
        if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-Screamer.prefab");
        return prefab;
    }

    public static GameObject GetDogBarkPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/Le Van Dat-DogBark.prefab");
        if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-DogBark.prefab");
        return prefab;
    }

    [MenuItem("Tools/Border Anomaly/Force Refresh Assets & Reload Scene")]
    public static void ForceRefreshAssetsAndReloadScene()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(activeScene.path);
        }
        Debug.Log($"[Border Anomaly] All assets refreshed and scene '{activeScene.name}' reloaded!");
        EditorUtility.DisplayDialog("Border Anomaly", $"Đã làm mới tài nguyên và mở lại Scene '{activeScene.name}' thành công!", "OK");
    }

    [MenuItem("Tools/Border Anomaly/Generate 3 New Mutant Prefabs")]
    public static void GenerateMutants()
    {
        GameObject basePrefab = GetBasePrefab();
        if (basePrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Base prefab not found at: " + BasePrefabPath, "OK");
            return;
        }

        if (!Directory.Exists(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
        }

        // 1. Generate Jumper
        CreateJumperPrefab(basePrefab);

        // 2. Generate Crawler
        CreateCrawlerPrefab(basePrefab);

        // 3. Generate Screamer
        CreateScreamerPrefab(basePrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", 
            "Successfully generated 3 New Mutant Prefabs in:\n" + OutputFolder + 
            "\n\n1. Nguyen Van An-Jumper.prefab\n2. Tran Quoc Hung-Crawler.prefab\n3. ETIENNE PIERRE GIRAR-Screamer.prefab", 
            "OK");
    }

    [MenuItem("Tools/Border Anomaly/Add 3 Mutants To Scene Inspection Queue")]
    public static void AddMutantsToInspectionQueue()
    {
        NPCInspectionManager manager = Object.FindAnyObjectByType<NPCInspectionManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Notice", "Please open the Night-1 scene containing NPCInspectionManager first!", "OK");
            return;
        }

        GameObject jumperPrefab = GetJumperPrefab();
        GameObject crawlerPrefab = GetCrawlerPrefab();
        GameObject screamerPrefab = GetScreamerPrefab();

        if (jumperPrefab == null || crawlerPrefab == null || screamerPrefab == null)
        {
            GenerateMutants();
            jumperPrefab = GetJumperPrefab();
            crawlerPrefab = GetCrawlerPrefab();
            screamerPrefab = GetScreamerPrefab();
        }

        Undo.RecordObject(manager, "Add 3 New Mutants To Queue");

        // 1. Jumper Profile
        NPCInspectionProfile jumperProfile = new NPCInspectionProfile
        {
            npcName = "Nguyen Van An - Jumper",
            isMutant = true,
            npcPrefab = jumperPrefab,
            dialogueOnArrive = "Officer! Open the gate quickly, I can hear them breathing behind me!",
            defenseDialogues = new string[]
            {
                "Why are you taking so long?! Look at my papers, they are authentic!",
                "They are coming for me! Open the door right now! OPEN IT!!",
                "I am not one of them! Look at my hands! I'm shivering from the cold!"
            },
            dialogueOnApproved = "Hehehe... finally inside...",
            dialogueOnDenied = "GRAAAHHH! YOU CANNOT LOCK ME OUT!"
        };

        // 2. Crawler Profile
        NPCInspectionProfile crawlerProfile = new NPCInspectionProfile
        {
            npcName = "Tran Quoc Hung - Crawler",
            isMutant = true,
            npcPrefab = crawlerPrefab,
            dialogueOnArrive = "Officer... down here... please help me... my legs...",
            defenseDialogues = new string[]
            {
                "I had to crawl through the black mud... my bones got twisted in the dark...",
                "Don't look at my joints... it was just a bear trap... I'm still human...",
                "The documents are in my coat... reach down and inspect them..."
            },
            dialogueOnApproved = "*Chittering noise* ...Warm meat...",
            dialogueOnDenied = "*HISSS* YOU WON'T ESCAPE THIS MIST!"
        };

        // 3. Screamer Profile
        NPCInspectionProfile screamerProfile = new NPCInspectionProfile
        {
            npcName = "ETIENNE PIERRE GIRAR - Screamer",
            isMutant = true,
            npcPrefab = screamerPrefab,
            dialogueOnArrive = "The static... it won't stop ringing... CAN YOU HEAR IT?!",
            defenseDialogues = new string[]
            {
                "Stop staring at those documents! TURN OFF THE TRANSMITTER!",
                "My skull is splitting open... MAKE THE NOISE STOP!",
                "Look at my surrender confirm! Just stamp it and let me through!!"
            },
            dialogueOnApproved = "Now... we all scream together...",
            dialogueOnDenied = "*DEAFENING DEMONIC SHRIEK*"
        };

        manager.npcQueue.Add(jumperProfile);
        manager.npcQueue.Add(crawlerProfile);
        manager.npcQueue.Add(screamerProfile);

        EditorUtility.SetDirty(manager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

        EditorUtility.DisplayDialog("Success", 
            "Successfully added 3 New Mutants to NPCInspectionManager.npcQueue!\n\n" +
            "1. Nguyen Van An - Jumper\n" +
            "2. Tran Quoc Hung - Crawler\n" +
            "3. ETIENNE PIERRE GIRAR - Screamer", 
            "OK");
    }

    [MenuItem("Tools/Border Anomaly/Set Crawler As First In Inspection Queue")]
    public static void SetCrawlerAsFirstInQueue()
    {
        NPCInspectionManager manager = Object.FindAnyObjectByType<NPCInspectionManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Notice", "Please open the Night-1 scene containing NPCInspectionManager first!", "OK");
            return;
        }

        GameObject crawlerPrefab = GetCrawlerPrefab();
        if (crawlerPrefab == null)
        {
            GenerateMutants();
            crawlerPrefab = GetCrawlerPrefab();
        }

        Undo.RecordObject(manager, "Set Crawler As First In Queue");

        NPCInspectionProfile crawlerProfile = new NPCInspectionProfile
        {
            npcName = "Tran Quoc Hung - Crawler",
            isMutant = true,
            npcPrefab = crawlerPrefab,
            dialogueOnArrive = "Officer... down here... please help me... my legs...",
            defenseDialogues = new string[]
            {
                "I had to crawl through the mud... don't look at my joints...",
                "The documents are here... just look at them..."
            },
            dialogueOnApproved = "*Chittering noise* ...Warm meat...",
            dialogueOnDenied = "*HISSS* YOU WON'T ESCAPE THIS MIST!"
        };

        if (manager.npcQueue == null) manager.npcQueue = new System.Collections.Generic.List<NPCInspectionProfile>();
        manager.npcQueue.Insert(0, crawlerProfile);

        EditorUtility.SetDirty(manager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

        EditorUtility.DisplayDialog("Success", "Set Tran Quoc Hung - Crawler as the FIRST NPC in Inspection Queue! Press Play to test immediately.", "OK");
    }

    [MenuItem("Tools/Border Anomaly/Generate Dog Bark NPC Prefab")]
    public static void GenerateDogBarkPrefabMenu()
    {
        GameObject basePrefab = GetBasePrefab();
        if (basePrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Base prefab not found at: " + BasePrefabPath, "OK");
            return;
        }

        CreateDogBarkPrefab(basePrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", "Successfully generated Le Van Dat-DogBark.prefab in " + OutputFolder, "OK");
    }

    [MenuItem("Tools/Border Anomaly/Set Dog Bark NPC As First In Inspection Queue")]
    public static void SetDogBarkAsFirstInQueue()
    {
        NPCInspectionManager manager = Object.FindAnyObjectByType<NPCInspectionManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Notice", "Please open the Night-1 scene containing NPCInspectionManager first!", "OK");
            return;
        }

        GameObject dogBarkPrefab = GetDogBarkPrefab();
        if (dogBarkPrefab == null)
        {
            GenerateDogBarkPrefabMenu();
            dogBarkPrefab = GetDogBarkPrefab();
        }

        Undo.RecordObject(manager, "Set Dog Bark NPC As First In Queue");

        NPCInspectionProfile dogBarkProfile = new NPCInspectionProfile
        {
            npcName = "Le Van Dat - DogBark",
            isMutant = true,
            npcPrefab = dogBarkPrefab,
            dialogueOnArrive = "Officer, please let me pass! Why is that dog barking like crazy at me?!",
            defenseDialogues = new string[]
            {
                "Quiet that dog down! It's making me nervous... I'm a registered citizen!",
                "Dogs always bark at strangers in this thick mist, don't listen to it!",
                "Check my papers! Everything is legitimate! Just tell that mutt to shut up!"
            },
            dialogueOnApproved = "Heh... the mutt knew, but you didn't...",
            dialogueOnDenied = "YOU SHOULD HAVE TRUSTED THE DOG! *ROAR*"
        };

        if (manager.npcQueue == null) manager.npcQueue = new System.Collections.Generic.List<NPCInspectionProfile>();
        manager.npcQueue.Insert(0, dogBarkProfile);

        EditorUtility.SetDirty(manager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

        EditorUtility.DisplayDialog("Success", "Set Le Van Dat - DogBark as the FIRST NPC in Inspection Queue!\nPress Play to test immediately.", "OK");
    }

    [MenuItem("Tools/Border Anomaly/Set Jumper As First In Inspection Queue")]
    public static void SetJumperAsFirstInQueue()
    {
        NPCInspectionManager manager = Object.FindAnyObjectByType<NPCInspectionManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Notice", "Please open the Night-1 scene containing NPCInspectionManager first!", "OK");
            return;
        }

        GameObject jumperPrefab = GetJumperPrefab();
        if (jumperPrefab == null)
        {
            GenerateMutants();
            jumperPrefab = GetJumperPrefab();
        }

        Undo.RecordObject(manager, "Set Jumper As First In Queue");

        NPCInspectionProfile jumperProfile = new NPCInspectionProfile
        {
            npcName = "Nguyen Van An - Jumper",
            isMutant = true,
            npcPrefab = jumperPrefab,
            dialogueOnArrive = "Officer! Open the gate quickly, I can hear them breathing behind me!",
            defenseDialogues = new string[]
            {
                "Why are you taking so long?! Look at my papers, they are authentic!",
                "They are coming for me! Open the door right now! OPEN IT!!",
                "I am not one of them! Look at my hands! I'm shivering from the cold!"
            },
            dialogueOnApproved = "Hehehe... finally inside...",
            dialogueOnDenied = "GRAAAHHH! YOU CANNOT LOCK ME OUT!"
        };

        if (manager.npcQueue == null) manager.npcQueue = new System.Collections.Generic.List<NPCInspectionProfile>();
        manager.npcQueue.Insert(0, jumperProfile);

        EditorUtility.SetDirty(manager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

        EditorUtility.DisplayDialog("Success", "Set Subject J-01 (The Jumper) as the FIRST NPC in Inspection Queue!\nPress Play to test immediately.", "OK");
    }

    [MenuItem("Tools/Border Anomaly/Create Player Camera Jumpscare Point In Scene")]
    public static void CreateCameraJumpscarePointInScene()
    {
        PlayerCameraJumpscarePoint existing = Object.FindAnyObjectByType<PlayerCameraJumpscarePoint>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing.gameObject);
            EditorUtility.DisplayDialog("Notice", "PlayerCameraJumpscarePoint already exists on Camera! Selected it in Hierarchy for you.", "OK");
            return;
        }

        PlayerCameraJumpscarePoint newPoint = PlayerCameraJumpscarePoint.CreateDefaultCameraPoint();
        if (newPoint != null)
        {
            Undo.RegisterCreatedObjectUndo(newPoint.gameObject, "Create PlayerCameraJumpscarePoint");
            Selection.activeGameObject = newPoint.gameObject;
            EditorGUIUtility.PingObject(newPoint.gameObject);
            EditorUtility.SetDirty(newPoint.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(newPoint.gameObject.scene);
            EditorUtility.DisplayDialog("Success", "Successfully created PlayerCameraJumpscarePoint attached to Camera!\nYou can view its red Gizmo in Scene view and adjust localOffset in Inspector.", "OK");
        }
    }

    [MenuItem("Tools/Border Anomaly/Refresh Controls Tab In Player Prefab")]
    public static void SetupControlsTabInPlayerPrefab()
    {
        string playerPrefabPath = "Assets/Phu-Asset/Prefabs/Player/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
        if (playerPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Player prefab not found at " + playerPrefabPath, "OK");
            return;
        }

        Sprite keyboardGuide = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Phu-Asset/Textures/UI/Keyboard_Controls_Guide.jpg");
        if (keyboardGuide == null)
        {
            EditorUtility.DisplayDialog("Error", "Keyboard guide sprite not found at Assets/Phu-Asset/Textures/UI/Keyboard_Controls_Guide.jpg", "OK");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        Transform controlsPanel = instance.transform.Find("Canvas/SettingPanel/Panel_Controls");
        if (controlsPanel == null)
        {
            // Search deep
            Transform[] all = instance.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t.name == "Panel_Controls") { controlsPanel = t; break; }
            }
        }

        if (controlsPanel != null)
        {
            UnityEngine.UI.Image img = controlsPanel.GetComponent<UnityEngine.UI.Image>();
            if (img == null) img = controlsPanel.gameObject.AddComponent<UnityEngine.UI.Image>();
            img.sprite = keyboardGuide;
            img.color = Color.white;
            img.type = UnityEngine.UI.Image.Type.Simple;
            img.preserveAspect = true;

            PrefabUtility.SaveAsPrefabAsset(instance, playerPrefabPath);
            Object.DestroyImmediate(instance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", "Successfully applied Keyboard Controls Guide image to Panel_Controls in Player.prefab!", "OK");
        }
        else
        {
            Object.DestroyImmediate(instance);
            EditorUtility.DisplayDialog("Error", "Could not locate Panel_Controls in Player.prefab", "OK");
        }
    }

    private static void RemoveOtherBehaviors(GameObject obj)
    {
        var existingBehaviors = obj.GetComponents<NPCBaseBehavior>();
        foreach (var b in existingBehaviors)
        {
            if (b != null) Object.DestroyImmediate(b);
        }
    }

    private static void CreateJumperPrefab(GameObject basePrefab)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = "Nguyen Van An-Jumper";

        RemoveOtherBehaviors(instance);

        NPCJumperBehavior jumper = instance.AddComponent<NPCJumperBehavior>();
        jumper.minTriggerDelay = 1.0f;
        jumper.maxTriggerDelay = 1.8f;
        jumper.flyDuration = 0.22f;
        jumper.leapHeightArc = 0.35f;
        jumper.faceLatchDistance = 0.35f;
        jumper.faceScareDuration = 1.0f;
        jumper.cameraShakeDuration = 0.7f;
        jumper.cameraShakeMagnitude = 0.6f;
        jumper.advanceInspectionQueue = true;

        // Load jumpscare prefab & horror sounds
        jumper.jumpscareModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Phu-Asset/Prefabs/JumpScare/JumpScare.prefab");
        jumper.jumpscarePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Phu-Asset/Prefabs/JumpScare/JumpScare.prefab");
        jumper.jumpscareSpawnPoint = null;
        jumper.jumpscareSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Impacts & Hits/JumpScare.wav");
        jumper.nervousBreathingSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Dark/Squishing_Short_01.wav");

        string path = OutputFolder + "/Nguyen Van An-Jumper.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        Debug.Log("[MutantGenerator] Created: " + path);
    }

    private static void CreateCrawlerPrefab(GameObject basePrefab)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = "Tran Quoc Hung-Crawler";

        RemoveOtherBehaviors(instance);

        NPCCrawlerBehavior crawler = instance.AddComponent<NPCCrawlerBehavior>();
        crawler.walkSpeed = 2.4f;
        crawler.talkDurationBeforeVanish = 5.0f;
        crawler.behindDistance = 2.4f;
        crawler.flyDuration = 0.22f;
        crawler.faceScareDuration = 0.9f;
        crawler.advanceInspectionQueue = true;

        // Load horror sounds for vanish & face jumpscare
        crawler.vanishSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Dark/Squishing_Short_01.wav");
        crawler.behindWhisperSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Dark/Drn_Chrom_01.wav");
        crawler.jumpscareRoarSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Impacts & Hits/JumpScare.wav");

        string path = OutputFolder + "/Tran Quoc Hung-Crawler.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        Debug.Log("[MutantGenerator] Created: " + path);
    }

    private static void CreateScreamerPrefab(GameObject basePrefab)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = "ETIENNE PIERRE GIRAR-Screamer";

        RemoveOtherBehaviors(instance);

        NPCScreamerBehavior screamer = instance.AddComponent<NPCScreamerBehavior>();
        screamer.screamDuration = 3.2f;
        screamer.cameraShakeIntensity = 0.22f;

        // Find head bone
        Transform head = instance.transform.Find("Head");
        if (head == null) head = instance.transform.Find("Model/Head");
        if (head == null)
        {
            Transform[] allChildren = instance.GetComponentsInChildren<Transform>();
            foreach (var t in allChildren)
            {
                if (t.name.ToLower().Contains("head")) { head = t; break; }
            }
        }
        screamer.headBone = head;

        // Load scream audio
        screamer.screamShriekSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Dark/Drn_Autonomy_01.wav");
        screamer.radioStaticSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Dark/Drn_Abandoned_Lab_01.wav");
        screamer.earRingingSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Dark/Drn_Alien_01.wav");

        string path = OutputFolder + "/ETIENNE PIERRE GIRAR-Screamer.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        Debug.Log("[MutantGenerator] Created: " + path);
    }

    private static void CreateDogBarkPrefab(GameObject basePrefab)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = "Le Van Dat-DogBark";

        RemoveOtherBehaviors(instance);

        NPCDogBarkBehavior dogBark = instance.AddComponent<NPCDogBarkBehavior>();
        dogBark.isMutant = true;
        dogBark.randomizeOnStart = true;
        dogBark.barkInterval = 0.55f;
        dogBark.jumpscareDuration = 1.8f;

        dogBark.dogBarkClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/NPCVoice/Dog_Bark_Single.wav");
        dogBark.dogAggressiveBarkClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/NPCVoice/Dog_Bark_Aggressive.wav");
        dogBark.jumpscareSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Phu-Asset/Textures/Jumpscare/Horror_Jumpscare_Face.png");
        dogBark.jumpscareSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/NPCVoice/Jumpscare_Scream_Horror.wav");

        string path = OutputFolder + "/Le Van Dat-DogBark.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        Debug.Log("[MutantGenerator] Created: " + path);
    }

    [MenuItem("Tools/Border Anomaly/Setup Player Spawn In Cellar (Night-1)")]
    public static void SetupPlayerSpawnInCellar()
    {
        // 1. Tìm StartSpawn hoặc CellarSpawnPoint
        Transform spawnPoint = null;
        string[] candidates = new string[] { "StartSpawn", "startspawn", "CellarSpawnPoint", "cellarspawnpoint" };
        foreach (string cName in candidates)
        {
            GameObject ptObj = GameObject.Find(cName);
            if (ptObj != null) { spawnPoint = ptObj.transform; break; }
        }

        if (spawnPoint == null)
        {
            GameObject cellar = GameObject.Find("Cellar");
            if (cellar != null)
            {
                foreach (string cName in candidates)
                {
                    Transform pt = cellar.transform.Find(cName);
                    if (pt != null) { spawnPoint = pt; break; }
                }
                if (spawnPoint == null)
                {
                    foreach (Transform c in cellar.GetComponentsInChildren<Transform>(true))
                    {
                        string lower = c.name.ToLower();
                        if (lower == "startspawn" || lower == "cellarspawnpoint" || lower.Contains("startspawn") || lower.Contains("cellarspawn"))
                        {
                            spawnPoint = c;
                            break;
                        }
                    }
                }
            }
        }

        if (spawnPoint == null)
        {
            EditorUtility.DisplayDialog("Thông báo", "Chưa tìm thấy GameObject 'StartSpawn' trong Scene hiện tại.\nHãy tạo 1 GameObject rỗng tên 'StartSpawn' tại vị trí mong muốn trên map mới rồi chạy lại!", "OK");
            return;
        }

        Vector3 targetPos = spawnPoint.position + Vector3.up * 0.08f;
        Quaternion targetRot = spawnPoint.rotation;

        // 2. Tìm hoặc Tạo Player
        PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
        GameObject playerObj = pc != null ? pc.gameObject : GameObject.FindWithTag("Player");
        if (playerObj != null && playerObj.transform.parent != null && playerObj.transform.parent.CompareTag("Player"))
        {
            playerObj = playerObj.transform.parent.gameObject;
        }

        if (playerObj == null)
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Phu-Asset/Prefabs/Player/Player.prefab");
            if (playerPrefab == null) playerPrefab = Resources.Load<GameObject>("Player");
            if (playerPrefab != null)
            {
                playerObj = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                playerObj.name = "Player";
                Undo.RegisterCreatedObjectUndo(playerObj, "Spawn Player In Cellar");
                pc = playerObj.GetComponentInChildren<PlayerController>();
                Debug.Log("[PlayerSetup] Đã tạo mới Player từ Prefab trong Scene!");
            }
        }

        if (playerObj != null)
        {
            Undo.RecordObject(playerObj.transform, "Position Player In Cellar");
            CharacterController cc = playerObj.GetComponentInChildren<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerObj.transform.position = targetPos;
            playerObj.transform.rotation = targetRot;
            if (cc != null) cc.enabled = true;
            Debug.Log($"[PlayerSetup] Đã đặt Player tại vị trí Cellar: {targetPos}");
        }

        // 3. Đảm bảo PlayerSpawnManager tồn tại trong Scene
        PlayerSpawnManager spawner = Object.FindAnyObjectByType<PlayerSpawnManager>();
        if (spawner == null)
        {
            GameObject spawnerObj = new GameObject("PlayerSpawnManager");
            spawner = spawnerObj.AddComponent<PlayerSpawnManager>();
            Undo.RegisterCreatedObjectUndo(spawnerObj, "Create PlayerSpawnManager");
        }

        spawner.defaultSpawnPoint = spawnPoint;
        spawner.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Phu-Asset/Prefabs/Player/Player.prefab");
        spawner.autoPositionOnStart = true;
        EditorUtility.SetDirty(spawner);

        // 4. Căn chỉnh Camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam.transform, "Align Camera With Player");
            cam.transform.position = targetPos + Vector3.up * 1.6f;
            cam.transform.rotation = targetRot;
        }

        // 5. Lưu Scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Thành công", $"Đã thiết lập Player xuất hiện tại vị trí CellarSpawnPoint trong hầm Cellar thành công!\nTọa độ: {targetPos}\nPlayer và PlayerSpawnManager đã sẵn sàng.", "OK");
    }
}
#endif
