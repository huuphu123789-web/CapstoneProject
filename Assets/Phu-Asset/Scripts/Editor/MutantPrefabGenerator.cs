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
    private const string BasePrefabPath = "Assets/Phu-Asset/Prefabs/NPC/Npc-Normal.prefab";
    private const string OutputFolder = "Assets/Phu-Asset/Prefabs/NPC";

    [MenuItem("Tools/Border Anomaly/Generate 3 New Mutant Prefabs")]
    public static void GenerateMutants()
    {
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
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
            "\n\n1. NPC-Jumper.prefab\n2. NPC-Crawler.prefab\n3. NPC-Screamer.prefab", 
            "OK");
    }

    [MenuItem("Tools/Border Anomaly/Add 3 Mutants To Scene Inspection Queue")]
    public static void AddMutantsToInspectionQueue()
    {
        NPCInspectionManager manager = Object.FindFirstObjectByType<NPCInspectionManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Notice", "Please open the Night-1 scene containing NPCInspectionManager first!", "OK");
            return;
        }

        GameObject jumperPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-Jumper.prefab");
        GameObject crawlerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-Crawler.prefab");
        GameObject screamerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-Screamer.prefab");
        if (screamerPrefab == null)
        {
            screamerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/ETIENNE PIERRE GIRAR-Screamer.prefab");
        }

        if (jumperPrefab == null || crawlerPrefab == null || screamerPrefab == null)
        {
            GenerateMutants();
            jumperPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-Jumper.prefab");
            crawlerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-Crawler.prefab");
            screamerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-Screamer.prefab");
            if (screamerPrefab == null)
            {
                screamerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/ETIENNE PIERRE GIRAR-Screamer.prefab");
            }
        }

        Undo.RecordObject(manager, "Add 3 New Mutants To Queue");

        // 1. Jumper Profile
        NPCInspectionProfile jumperProfile = new NPCInspectionProfile
        {
            npcName = "Subject J-01 (The Jumper)",
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
            npcName = "Subject C-02 (The Crawler)",
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
            npcName = "Subject S-03 (The Screamer)",
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
            "1. Subject J-01 (The Jumper)\n" +
            "2. Subject C-02 (The Crawler)\n" +
            "3. Subject S-03 (The Screamer)", 
            "OK");
    }

    [MenuItem("Tools/Border Anomaly/Set Crawler As First In Inspection Queue")]
    public static void SetCrawlerAsFirstInQueue()
    {
        NPCInspectionManager manager = Object.FindFirstObjectByType<NPCInspectionManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Notice", "Please open the Night-1 scene containing NPCInspectionManager first!", "OK");
            return;
        }

        GameObject crawlerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-Crawler.prefab");
        if (crawlerPrefab == null)
        {
            GenerateMutants();
            crawlerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-Crawler.prefab");
        }

        Undo.RecordObject(manager, "Set Crawler As First In Queue");

        NPCInspectionProfile crawlerProfile = new NPCInspectionProfile
        {
            npcName = "Subject C-02 (The Crawler)",
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

        EditorUtility.DisplayDialog("Success", "Set Subject C-02 (The Crawler) as the FIRST NPC in Inspection Queue! Press Play to test immediately.", "OK");
    }

    [MenuItem("Tools/Border Anomaly/Generate Dog Bark NPC Prefab")]
    public static void GenerateDogBarkPrefabMenu()
    {
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
        if (basePrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Base prefab not found at: " + BasePrefabPath, "OK");
            return;
        }

        CreateDogBarkPrefab(basePrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", "Successfully generated NPC-DogBark.prefab in " + OutputFolder, "OK");
    }

    [MenuItem("Tools/Border Anomaly/Set Dog Bark NPC As First In Inspection Queue")]
    public static void SetDogBarkAsFirstInQueue()
    {
        NPCInspectionManager manager = Object.FindFirstObjectByType<NPCInspectionManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Notice", "Please open the Night-1 scene containing NPCInspectionManager first!", "OK");
            return;
        }

        GameObject dogBarkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-DogBark.prefab");
        if (dogBarkPrefab == null)
        {
            GenerateDogBarkPrefabMenu();
            dogBarkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-DogBark.prefab");
        }

        Undo.RecordObject(manager, "Set Dog Bark NPC As First In Queue");

        NPCInspectionProfile dogBarkProfile = new NPCInspectionProfile
        {
            npcName = "Subject D-04 (Suspect with Dog)",
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

        EditorUtility.DisplayDialog("Success", "Set Subject D-04 (Dog Bark NPC) as the FIRST NPC in Inspection Queue!\nPress Play to test immediately.", "OK");
    }

    [MenuItem("Tools/Border Anomaly/Set Jumper As First In Inspection Queue")]
    public static void SetJumperAsFirstInQueue()
    {
        NPCInspectionManager manager = Object.FindFirstObjectByType<NPCInspectionManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Notice", "Please open the Night-1 scene containing NPCInspectionManager first!", "OK");
            return;
        }

        GameObject jumperPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-Jumper.prefab");
        if (jumperPrefab == null)
        {
            GenerateMutants();
            jumperPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/NPC-Jumper.prefab");
        }

        Undo.RecordObject(manager, "Set Jumper As First In Queue");

        NPCInspectionProfile jumperProfile = new NPCInspectionProfile
        {
            npcName = "Subject J-01 (The Jumper)",
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
        PlayerCameraJumpscarePoint existing = Object.FindFirstObjectByType<PlayerCameraJumpscarePoint>();
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
        instance.name = "NPC-Jumper";

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

        string path = OutputFolder + "/NPC-Jumper.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        Debug.Log("[MutantGenerator] Created: " + path);
    }

    private static void CreateCrawlerPrefab(GameObject basePrefab)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = "NPC-Crawler";

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

        string path = OutputFolder + "/NPC-Crawler.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        Debug.Log("[MutantGenerator] Created: " + path);
    }

    private static void CreateScreamerPrefab(GameObject basePrefab)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = "NPC-Screamer";

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

        string path = OutputFolder + "/NPC-Screamer.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        Debug.Log("[MutantGenerator] Created: " + path);
    }

    private static void CreateDogBarkPrefab(GameObject basePrefab)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = "NPC-DogBark";

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

        string path = OutputFolder + "/NPC-DogBark.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        Debug.Log("[MutantGenerator] Created: " + path);
    }
}
#endif
