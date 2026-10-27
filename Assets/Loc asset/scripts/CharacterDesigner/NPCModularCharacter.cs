using System;
using System.Collections.Generic;
using UnityEngine;

namespace LocAsset.CharacterDesigner
{
    /// <summary>
    /// Component attached to an NPC character to handle modular customization,
    /// bone remapping, material assignments, and blendshape synchronization.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class NPCModularCharacter : MonoBehaviour
    {
        [Header("Customization Data")]
        public NPCCharacterData characterData = new NPCCharacterData();

        [Header("Tracked Attached Accessories")]
        [SerializeField] private GameObject currentHairInstance;
        [SerializeField] private GameObject currentFacialHairInstance;

        public GameObject CurrentHair => currentHairInstance;
        public GameObject CurrentFacialHair => currentFacialHairInstance;

        public static readonly Color[] PresetHairColors = new Color[]
        {
            new Color(0.12f, 0.12f, 0.12f, 1f), // 01 Jet Black
            new Color(0.24f, 0.16f, 0.12f, 1f), // 02 Dark Brown
            new Color(0.38f, 0.22f, 0.14f, 1f), // 03 Chestnut Brown
            new Color(0.78f, 0.65f, 0.38f, 1f), // 04 Golden Blonde
            new Color(0.70f, 0.58f, 0.42f, 1f), // 05 Sandy Blonde
            new Color(0.55f, 0.20f, 0.12f, 1f), // 06 Auburn / Red
            new Color(0.65f, 0.65f, 0.68f, 1f), // 07 Silver Gray
            new Color(0.90f, 0.90f, 0.92f, 1f), // 08 Platinum White
            new Color(0.45f, 0.45f, 0.48f, 1f), // 09 Ash Gray
            new Color(0.20f, 0.45f, 0.85f, 1f), // 10 Electric Blue
            new Color(0.85f, 0.30f, 0.55f, 1f), // 11 Deep Pink
            new Color(0.20f, 0.65f, 0.40f, 1f)  // 12 Emerald Green
        };

        private Dictionary<string, Transform> boneCache;

        private void Awake()
        {
            RebuildBoneCache();
            ApplyColors();
        }

        private void OnValidate()
        {
            ApplyColors();
        }

        /// <summary>
        /// Applies hair and facial hair colors to attached instances.
        /// </summary>
        public void ApplyColors()
        {
            if (characterData == null) return;

            Color hColor = characterData.useCustomHairColor 
                ? characterData.hairColor 
                : GetPresetColor(characterData.hairColorIndex);

            Color fColor = characterData.syncFacialHairColor 
                ? hColor 
                : (characterData.useCustomHairColor ? characterData.facialHairColor : GetPresetColor(characterData.facialHairColorIndex));

            if (currentHairInstance != null)
            {
                SetRenderersColor(currentHairInstance, hColor);
            }
            if (currentFacialHairInstance != null)
            {
                SetRenderersColor(currentFacialHairInstance, fColor);
            }

            ApplySkinColors();
        }

        /// <summary>
        /// Checks if a renderer belongs to the human skin mesh (face, neck, torso, arms, hands, legs).
        /// </summary>
        public static bool IsSkinRenderer(Renderer r)
        {
            if (r == null) return false;
            string goName = r.gameObject.name.ToLowerInvariant();
            Transform parent = r.transform.parent;
            string parentName = parent != null ? parent.name.ToLowerInvariant() : "";

            // Explicit exclusions: clothing, shoes, hair, facial hair
            if (goName.Contains("tshirt") || goName.Contains("shirt") || goName.Contains("cloth") || 
                goName.Contains("pants") || goName.Contains("shoe") || goName.Contains("footwear") ||
                goName.Contains("hair") || goName.Contains("beard") || goName.Contains("mustache"))
            {
                return false;
            }
            if (parentName.Contains("upper_cloth") || parentName.Contains("lower_cloth") || parentName.Contains("footwear"))
            {
                return false;
            }

            // Positive skin identifiers
            if (goName.StartsWith("npc_hmn_") || goName.Contains("head") || goName.Contains("arms") || 
                goName.Contains("fingers") || goName.Contains("torso") || goName.Contains("parts") || 
                parentName.Contains("body"))
            {
                return true;
            }

            return false;
        }

        public static Color GetBodyTintForVariant(int variant)
        {
            switch (variant)
            {
                case 1: return new Color(0.88f, 0.80f, 0.72f, 1f); // Olive
                case 2: return new Color(0.76f, 0.62f, 0.50f, 1f); // Tanned
                case 3: return new Color(0.62f, 0.48f, 0.38f, 1f); // Weathered
                case 4: return new Color(0.33f, 0.24f, 0.18f, 1f); // Dark
                default: return Color.white;                        // Fair
            }
        }

        /// <summary>
        /// Synchronizes skin color across all skin renderers (head, arms, torso, hands, neck).
        /// </summary>
        public void ApplySkinColors()
        {
            if (characterData == null) return;

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();

            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (!IsSkinRenderer(r)) continue;

                string goName = r.gameObject.name.ToLowerInvariant();
                bool isHead = goName.Contains("head");

                Color targetColor;
                if (characterData.useCustomSkinColor)
                {
                    // Custom skin color applies equally to head and all body parts
                    targetColor = characterData.skinColor;
                }
                else
                {
                    // Head uses pre-baked texture variant without extra tint (Color.white)
                    // Body parts (arms, neck, torso, hands) get tinted to match the head texture variant
                    targetColor = isHead ? Color.white : GetBodyTintForVariant(characterData.skinVariantIndex);
                }

                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", targetColor);
                mpb.SetColor("_Color", targetColor);
                r.SetPropertyBlock(mpb);
            }
        }

        public static Color GetPresetColor(int index)
        {
            index = Mathf.Clamp(index, 0, PresetHairColors.Length - 1);
            return PresetHairColors[index];
        }

        public static void SetRenderersColor(GameObject target, Color color)
        {
            if (target == null) return;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            foreach (var r in target.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", color);
                mpb.SetColor("_Color", color);
                r.SetPropertyBlock(mpb);
            }
        }

        /// <summary>
        /// Builds a lookup dictionary of all bones in the character skeleton.
        /// </summary>
        public void RebuildBoneCache()
        {
            boneCache = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (!boneCache.ContainsKey(t.name))
                {
                    boneCache.Add(t.name, t);
                }
            }
        }

        /// <summary>
        /// Remaps all bones in the given accessory's SkinnedMeshRenderers to match this character's skeleton.
        /// </summary>
        public void RemapAccessoryBones(GameObject accessory)
        {
            if (accessory == null) return;
            if (boneCache == null || boneCache.Count == 0) RebuildBoneCache();

            Transform hipsBone = null;
            boneCache.TryGetValue("Hips", out hipsBone);
            if (hipsBone == null) boneCache.TryGetValue("Root", out hipsBone);

            SkinnedMeshRenderer[] renderers = accessory.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in renderers)
            {
                Transform[] sourceBones = smr.bones;
                Transform[] newBones = new Transform[sourceBones.Length];

                for (int i = 0; i < sourceBones.Length; i++)
                {
                    if (sourceBones[i] != null && boneCache.TryGetValue(sourceBones[i].name, out Transform targetBone))
                    {
                        newBones[i] = targetBone;
                    }
                    else
                    {
                        newBones[i] = null;
                    }
                }

                smr.bones = newBones;

                if (smr.rootBone != null && boneCache.TryGetValue(smr.rootBone.name, out Transform matchingRoot))
                {
                    smr.rootBone = matchingRoot;
                }
                else if (hipsBone != null)
                {
                    smr.rootBone = hipsBone;
                }
            }
        }

        /// <summary>
        /// Thoroughly removes all hair GameObjects and renderers anywhere in this character's hierarchy.
        /// Prevents duplicate hair clipping/overlapping.
        /// </summary>
        public void RemoveAllHair()
        {
            if (currentHairInstance != null)
            {
                if (Application.isPlaying) Destroy(currentHairInstance);
                else DestroyImmediate(currentHairInstance);
                currentHairInstance = null;
            }

            var toDestroy = new HashSet<GameObject>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t == null || t == this.transform) continue;
                string lower = t.name.ToLowerInvariant();

                // Exclude facial hair
                if (lower.Contains("facial") || lower.Contains("beard") || lower.Contains("mustache"))
                    continue;

                if (lower.StartsWith("npc_haircut_") || lower.Contains("haircut") || lower.Equals("hair") || lower.StartsWith("hair_"))
                {
                    Transform topHair = t;
                    while (topHair.parent != null && topHair.parent != this.transform)
                    {
                        string pLower = topHair.parent.name.ToLowerInvariant();
                        if (pLower.StartsWith("npc_haircut_") || pLower.Contains("haircut") || (pLower.Contains("hair") && !pLower.Contains("facial")))
                        {
                            topHair = topHair.parent;
                        }
                        else break;
                    }
                    toDestroy.Add(topHair.gameObject);
                }
            }

            foreach (var go in toDestroy)
            {
                if (go != null)
                {
                    if (Application.isPlaying) Destroy(go);
                    else DestroyImmediate(go);
                }
            }

            // Secondary pass: ensure no residual hair meshes or LOD renderers remain
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr == null) continue;
                string rName = smr.name.ToLowerInvariant();
                string mName = smr.sharedMesh != null ? smr.sharedMesh.name.ToLowerInvariant() : "";
                if ((rName.Contains("haircut") || mName.Contains("haircut")) && !rName.Contains("facial") && !mName.Contains("facial"))
                {
                    if (Application.isPlaying) Destroy(smr.gameObject);
                    else DestroyImmediate(smr.gameObject);
                }
            }
        }

        /// <summary>
        /// Thoroughly removes all facial hair GameObjects and renderers anywhere in this character's hierarchy.
        /// </summary>
        public void RemoveAllFacialHair()
        {
            if (currentFacialHairInstance != null)
            {
                if (Application.isPlaying) Destroy(currentFacialHairInstance);
                else DestroyImmediate(currentFacialHairInstance);
                currentFacialHairInstance = null;
            }

            var toDestroy = new HashSet<GameObject>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t == null || t == this.transform) continue;
                string lower = t.name.ToLowerInvariant();
                if (lower.StartsWith("npc_hairfacial_") || lower.Contains("hairfacial") || lower.Contains("beard") || lower.Contains("mustache"))
                {
                    Transform topFacial = t;
                    while (topFacial.parent != null && topFacial.parent != this.transform)
                    {
                        string pLower = topFacial.parent.name.ToLowerInvariant();
                        if (pLower.StartsWith("npc_hairfacial_") || pLower.Contains("hairfacial") || pLower.Contains("beard") || pLower.Contains("mustache"))
                        {
                            topFacial = topFacial.parent;
                        }
                        else break;
                    }
                    toDestroy.Add(topFacial.gameObject);
                }
            }

            foreach (var go in toDestroy)
            {
                if (go != null)
                {
                    if (Application.isPlaying) Destroy(go);
                    else DestroyImmediate(go);
                }
            }

            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr == null) continue;
                string rName = smr.name.ToLowerInvariant();
                string mName = smr.sharedMesh != null ? smr.sharedMesh.name.ToLowerInvariant() : "";
                if (rName.Contains("hairfacial") || mName.Contains("hairfacial") || rName.Contains("beard") || mName.Contains("mustache"))
                {
                    if (Application.isPlaying) Destroy(smr.gameObject);
                    else DestroyImmediate(smr.gameObject);
                }
            }
        }

        /// <summary>
        /// Sets the current hair model and remaps its bones.
        /// </summary>
        public void SetHairInstance(GameObject newHair)
        {
            RemoveAllHair();

            currentHairInstance = newHair;
            if (currentHairInstance != null)
            {
                currentHairInstance.transform.SetParent(this.transform, false);
                currentHairInstance.transform.localPosition = Vector3.zero;
                currentHairInstance.transform.localRotation = Quaternion.identity;
                currentHairInstance.transform.localScale = Vector3.one;
                RemapAccessoryBones(currentHairInstance);
            }
            ApplyColors();
        }

        /// <summary>
        /// Sets the current facial hair model and remaps its bones.
        /// </summary>
        public void SetFacialHairInstance(GameObject newFacialHair)
        {
            RemoveAllFacialHair();

            currentFacialHairInstance = newFacialHair;
            if (currentFacialHairInstance != null)
            {
                currentFacialHairInstance.transform.SetParent(this.transform, false);
                currentFacialHairInstance.transform.localPosition = Vector3.zero;
                currentFacialHairInstance.transform.localRotation = Quaternion.identity;
                currentFacialHairInstance.transform.localScale = Vector3.one;
                RemapAccessoryBones(currentFacialHairInstance);
            }
            ApplyColors();
        }

        /// <summary>
        /// Synchronizes athletic, overweight, underweight and facial blendshapes across all meshes.
        /// </summary>
        public void SyncBlendShapes()
        {
            if (characterData == null) return;

            SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in smrs)
            {
                if (smr == null || smr.sharedMesh == null) continue;
                Mesh mesh = smr.sharedMesh;
                int count = mesh.blendShapeCount;
                if (count == 0) continue;

                for (int i = 0; i < count; i++)
                {
                    string shapeName = mesh.GetBlendShapeName(i);
                    string lower = shapeName.ToLowerInvariant();

                    if (lower.Contains("athletic"))
                    {
                        smr.SetBlendShapeWeight(i, characterData.athleticWeight);
                    }
                    else if (lower.Contains("overweight") || lower.Contains("fat") || lower.Contains("heavy"))
                    {
                        smr.SetBlendShapeWeight(i, characterData.overweightWeight);
                    }
                    else if (lower.Contains("underweight") || lower.Contains("thin") || lower.Contains("slim"))
                    {
                        smr.SetBlendShapeWeight(i, characterData.underweightWeight);
                    }
                    // Facial blendshapes
                    else if (MatchBlendShape(lower, "eyes_size")) smr.SetBlendShapeWeight(i, characterData.eyesSize);
                    else if (MatchBlendShape(lower, "eyes_distance")) smr.SetBlendShapeWeight(i, characterData.eyesDistance);
                    else if (MatchBlendShape(lower, "eyes_height")) smr.SetBlendShapeWeight(i, characterData.eyesHeight);
                    else if (MatchBlendShape(lower, "eyes_angle")) smr.SetBlendShapeWeight(i, characterData.eyesAngle);
                    else if (MatchBlendShape(lower, "eyes_depth")) smr.SetBlendShapeWeight(i, characterData.eyesDepth);

                    else if (MatchBlendShape(lower, "nosebridge_depth")) smr.SetBlendShapeWeight(i, characterData.nosebridgeDepth);
                    else if (MatchBlendShape(lower, "nosebridge_width")) smr.SetBlendShapeWeight(i, characterData.nosebridgeWidth);
                    else if (MatchBlendShape(lower, "nose_height")) smr.SetBlendShapeWeight(i, characterData.noseHeight);
                    else if (MatchBlendShape(lower, "nose_width")) smr.SetBlendShapeWeight(i, characterData.noseWidth);
                    else if (MatchBlendShape(lower, "nose_angle")) smr.SetBlendShapeWeight(i, characterData.noseAngle);

                    else if (MatchBlendShape(lower, "lips_size")) smr.SetBlendShapeWeight(i, characterData.lipsSize);
                    else if (MatchBlendShape(lower, "mouthcorners_height")) smr.SetBlendShapeWeight(i, characterData.mouthCornersHeight);
                    else if (MatchBlendShape(lower, "mouth_width")) smr.SetBlendShapeWeight(i, characterData.mouthWidth);
                    else if (MatchBlendShape(lower, "mouth_height")) smr.SetBlendShapeWeight(i, characterData.mouthHeight);

                    else if (MatchBlendShape(lower, "jaw_width")) smr.SetBlendShapeWeight(i, characterData.jawWidth);
                    else if (MatchBlendShape(lower, "chin_width")) smr.SetBlendShapeWeight(i, characterData.chinWidth);
                    else if (MatchBlendShape(lower, "chin_height")) smr.SetBlendShapeWeight(i, characterData.chinHeight);
                    else if (MatchBlendShape(lower, "chin_depth")) smr.SetBlendShapeWeight(i, characterData.chinDepth);
                    else if (MatchBlendShape(lower, "doublechin")) smr.SetBlendShapeWeight(i, characterData.doubleChin);

                    else if (MatchBlendShape(lower, "cheekbone_height")) smr.SetBlendShapeWeight(i, characterData.cheekboneHeight);
                    else if (MatchBlendShape(lower, "cheekbone_size")) smr.SetBlendShapeWeight(i, characterData.cheekboneSize);
                    else if (MatchBlendShape(lower, "cheeks_size")) smr.SetBlendShapeWeight(i, characterData.cheeksSize);

                    else if (MatchBlendShape(lower, "brows_height")) smr.SetBlendShapeWeight(i, characterData.browsHeight);
                    else if (MatchBlendShape(lower, "brows_angle")) smr.SetBlendShapeWeight(i, characterData.browsAngle);
                    else if (MatchBlendShape(lower, "ears_length")) smr.SetBlendShapeWeight(i, characterData.earsLength);
                    else if (MatchBlendShape(lower, "ears_pointing")) smr.SetBlendShapeWeight(i, characterData.earsPointing);
                    else
                    {
                        // Check custom facial blendshapes
                        if (characterData.facialBlendShapes != null)
                        {
                            foreach (var bs in characterData.facialBlendShapes)
                            {
                                if (MatchBlendShape(lower, bs.shapeName))
                                {
                                    smr.SetBlendShapeWeight(i, bs.weight);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool MatchBlendShape(string meshLowerName, string key)
        {
            string lowerKey = key.ToLowerInvariant();
            return meshLowerName.StartsWith(lowerKey) || meshLowerName.Contains(lowerKey);
        }

        /// <summary>
        /// Registers newly attached accessories (hair, facial hair) into the character's LODGroup.
        /// </summary>
        public void RefreshLODGroup()
        {
            LODGroup lodGroup = GetComponent<LODGroup>();
            if (lodGroup == null) return;

            LOD[] lods = lodGroup.GetLODs();
            if (lods == null || lods.Length == 0) return;

            // Collect all renderers by LOD level
            for (int lvl = 0; lvl < lods.Length; lvl++)
            {
                string lodTag = "lod" + lvl;
                var rendererList = new List<Renderer>();

                foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
                {
                    if (r == null) continue;
                    string rName = r.gameObject.name.ToLowerInvariant();

                    // If renderer explicitly has lod tag matching this level
                    if (rName.Contains(lodTag))
                    {
                        rendererList.Add(r);
                    }
                    // For LOD 0, also include non-LOD tagged items if they don't have lod in their name
                    else if (lvl == 0 && !rName.Contains("lod"))
                    {
                        rendererList.Add(r);
                    }
                }

                lods[lvl].renderers = rendererList.ToArray();
            }

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }

        /// <summary>
        /// Randomizes the character's configuration with intelligent styling rules.
        /// </summary>
        public void Randomize()
        {
            if (characterData == null) characterData = new NPCCharacterData();

            characterData.gender = (UnityEngine.Random.value > 0.5f) ? NPCGender.Male : NPCGender.Female;
            characterData.topType = (UnityEngine.Random.value > 0.5f) ? NPCTopType.TShirt : NPCTopType.ShirtOpenRolled;
            characterData.skinVariantIndex = UnityEngine.Random.Range(0, 5);

            // Hair
            int hairIndex = UnityEngine.Random.Range(-1, 8);
            characterData.hairStyle = (NPCHairStyle)hairIndex;
            characterData.hairColorIndex = UnityEngine.Random.Range(0, 12);
            characterData.useCustomHairColor = false;
            characterData.hairColor = GetPresetColor(characterData.hairColorIndex);

            // Facial hair (male only)
            if (characterData.gender == NPCGender.Male)
            {
                int facialIndex = UnityEngine.Random.Range(-1, 4);
                characterData.facialHairStyle = (NPCFacialHairStyle)facialIndex;
                characterData.facialHairColorIndex = characterData.hairColorIndex; // Match hair color
                characterData.facialHairColor = characterData.hairColor;
                characterData.syncFacialHairColor = true;
            }
            else
            {
                characterData.facialHairStyle = NPCFacialHairStyle.CleanShaven;
            }

            // Clothing materials
            characterData.topMaterialIndex = UnityEngine.Random.Range(0, (characterData.topType == NPCTopType.TShirt) ? 8 : 7);
            characterData.pantsMaterialIndex = UnityEngine.Random.Range(0, 7);
            characterData.shoeMaterialIndex = UnityEngine.Random.Range(0, 4);

            // Body blendshapes (choose 1 dominant body type or normal)
            float bodyTypeRoll = UnityEngine.Random.value;
            if (bodyTypeRoll < 0.3f)
            {
                characterData.athleticWeight = UnityEngine.Random.Range(20f, 90f);
                characterData.overweightWeight = 0f;
                characterData.underweightWeight = 0f;
            }
            else if (bodyTypeRoll < 0.6f)
            {
                characterData.athleticWeight = 0f;
                characterData.overweightWeight = UnityEngine.Random.Range(20f, 90f);
                characterData.underweightWeight = 0f;
            }
            else if (bodyTypeRoll < 0.8f)
            {
                characterData.athleticWeight = 0f;
                characterData.overweightWeight = 0f;
                characterData.underweightWeight = UnityEngine.Random.Range(20f, 80f);
            }
            else
            {
                // Average build
                characterData.athleticWeight = 0f;
                characterData.overweightWeight = 0f;
                characterData.underweightWeight = 0f;
            }

            SyncBlendShapes();
        }
    }
}
