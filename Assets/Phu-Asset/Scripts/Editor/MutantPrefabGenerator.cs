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

    private static void RemoveOtherBehaviors(GameObject obj)
    {
        var normal = obj.GetComponent<NPCNormalBehavior>();
        if (normal != null) Object.DestroyImmediate(normal);

        var weird = obj.GetComponent<NPCWeirdBehaviors>();
        if (weird != null) Object.DestroyImmediate(weird);

        var spin = obj.GetComponent<NPCSpinningLegsBehavior>();
        if (spin != null) Object.DestroyImmediate(spin);

        var head = obj.GetComponent<NPCHeadSpinBehavior>();
        if (head != null) Object.DestroyImmediate(head);

        var exploder = obj.GetComponent<NPCExploderBehavior>();
        if (exploder != null) Object.DestroyImmediate(exploder);

        var floating = obj.GetComponent<NPCFloatingBehavior>();
        if (floating != null) Object.DestroyImmediate(floating);

        var detached = obj.GetComponent<NPCDetachedLimbsBehavior>();
        if (detached != null) Object.DestroyImmediate(detached);
    }

    private static void CreateJumperPrefab(GameObject basePrefab)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = "NPC-Jumper";

        RemoveOtherBehaviors(instance);

        NPCJumperBehavior jumper = instance.AddComponent<NPCJumperBehavior>();
        jumper.minTriggerDelay = 2.0f;
        jumper.maxTriggerDelay = 4.0f;
        jumper.jumpscareDuration = 1.8f;

        // Load jumpscare prefab & horror sounds
        jumper.jumpscarePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Phu-Asset/Prefabs/JumpScare/JumpScare.prefab");
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
}
#endif
