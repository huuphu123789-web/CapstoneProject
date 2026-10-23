using System;
using System.Collections.Generic;
using UnityEngine;

namespace LocAsset.CharacterDesigner
{
    public enum NPCGender
    {
        Male = 0,
        Female = 1
    }

    public enum NPCTopType
    {
        TShirt = 0,
        ShirtOpenRolled = 1
    }

    public enum NPCHairStyle
    {
        Bald = -1,
        Haircut_A_01 = 0,
        Haircut_A_02 = 1,
        Haircut_A_03 = 2,
        Haircut_C_01 = 3,
        Haircut_C_02 = 4,
        Haircut_C_03 = 5,
        Haircut_D_01 = 6,
        Haircut_E_01 = 7
    }

    public enum NPCFacialHairStyle
    {
        CleanShaven = -1,
        Beard_Full_A01 = 0,
        Goatee_B01 = 1,
        Mustache_C01 = 2,
        Stubble_D01 = 3
    }

    [System.Serializable]
    public class BlendShapeValue
    {
        public string shapeName;
        [Range(0f, 100f)]
        public float weight;

        public BlendShapeValue(string name, float w)
        {
            shapeName = name;
            weight = w;
        }
    }

    /// <summary>
    /// Holds all configuration data for an NPC modular character.
    /// Supports serialization, presets, and runtime customization.
    /// </summary>
    [System.Serializable]
    public class NPCCharacterData
    {
        [Header("Citizen Identity")]
        public string characterName = "Citizen_01";

        [Header("Base Settings")]
        public NPCGender gender = NPCGender.Male;
        public NPCTopType topType = NPCTopType.TShirt;
        [Range(0, 7)]
        public int baseCharacterPreset = 0; // 0 to 7 (8 rich base character archetypes)
        [Range(0, 2)]
        public int headMeshIndex = 1; // 0: Head 1, 1: Head 2 (standard), 2: Head 3

        [Header("Eye Color")]
        [Range(0, 9)]
        public int eyeColorPreset = 0; // 0 to 9 preset eye colors
        public Color eyeColor = new Color(0.29f, 0.18f, 0.11f, 1f); // Natural warm brown
        public bool useCustomEyeColor = false;

        [Header("Skin Tone & Color")]
        [Range(0, 4)]
        public int skinVariantIndex = 0; // 0 to 4 (5 face skin variants)
        public Color skinColor = Color.white; // Custom skin color tint applied to entire body (head, neck, arms, hands, torso)
        public bool useCustomSkinColor = false; // True if using freeform skin color or preset tone

        [Header("Hair & Beard")]
        public NPCHairStyle hairStyle = NPCHairStyle.Haircut_A_01;
        [Range(0, 11)]
        public int hairColorIndex = 0; // 0 to 11 (12 preset hair color materials)
        public Color hairColor = new Color(0.12f, 0.12f, 0.12f, 1f); // Customizable RGB hair color
        public bool useCustomHairColor = false; // True if using freeform color picker

        public NPCFacialHairStyle facialHairStyle = NPCFacialHairStyle.CleanShaven;
        [Range(0, 11)]
        public int facialHairColorIndex = 0;
        public Color facialHairColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        public bool syncFacialHairColor = true; // Sync beard/mustache color with hair color

        [Header("Clothing Materials")]
        [Range(0, 5)]
        public int topMaterialIndex = 0; // Material variant for T-shirt or outer shirt
        [Range(0, 5)]
        public int innerTopMaterialIndex = 3; // Material variant for inner T-shirt (default 3: Clean White / Trắng)
        [Range(0, 4)]
        public int pantsMaterialIndex = 0; // Material variant for pants
        [Range(0, 3)]
        public int shoeMaterialIndex = 0; // Material variant for shoes

        [Header("Body Blendshapes")]
        [Range(0f, 100f)]
        public float athleticWeight = 0f;
        [Range(0f, 100f)]
        public float overweightWeight = 0f;
        [Range(0f, 100f)]
        public float underweightWeight = 0f;

        [Header("Facial Blendshapes")]
        [Range(-100f, 100f)] public float eyesSize = 0f;
        [Range(-100f, 100f)] public float eyesDistance = 0f;
        [Range(-100f, 100f)] public float eyesHeight = 0f;
        [Range(-100f, 100f)] public float eyesAngle = 0f;
        [Range(-100f, 100f)] public float eyesDepth = 0f;

        [Range(-100f, 100f)] public float noseHeight = 0f;
        [Range(-100f, 100f)] public float noseWidth = 0f;
        [Range(-100f, 100f)] public float nosebridgeDepth = 0f;
        [Range(-100f, 100f)] public float nosebridgeWidth = 0f;
        [Range(-100f, 100f)] public float noseAngle = 0f;

        [Range(-100f, 100f)] public float lipsSize = 0f;
        [Range(-100f, 100f)] public float mouthWidth = 0f;
        [Range(-100f, 100f)] public float mouthHeight = 0f;
        [Range(-100f, 100f)] public float mouthCornersHeight = 0f;

        [Range(-100f, 100f)] public float jawWidth = 0f;
        [Range(-100f, 100f)] public float chinWidth = 0f;
        [Range(-100f, 100f)] public float chinHeight = 0f;
        [Range(-100f, 100f)] public float chinDepth = 0f;
        [Range(0f, 100f)]    public float doubleChin = 0f;

        [Range(-100f, 100f)] public float cheeksSize = 0f;
        [Range(-100f, 100f)] public float cheekboneHeight = 0f;
        [Range(-100f, 100f)] public float cheekboneSize = 0f;

        [Range(-100f, 100f)] public float browsHeight = 0f;
        [Range(-100f, 100f)] public float browsAngle = 0f;
        [Range(-100f, 100f)] public float earsLength = 0f;
        [Range(-100f, 100f)] public float earsPointing = 0f;

        public List<BlendShapeValue> facialBlendShapes = new List<BlendShapeValue>();

        public void ResetFacialFeatures()
        {
            eyesSize = eyesDistance = eyesHeight = eyesAngle = eyesDepth = 0f;
            noseHeight = noseWidth = nosebridgeDepth = nosebridgeWidth = noseAngle = 0f;
            lipsSize = mouthWidth = mouthHeight = mouthCornersHeight = 0f;
            jawWidth = chinWidth = chinHeight = chinDepth = doubleChin = 0f;
            cheeksSize = cheekboneHeight = cheekboneSize = 0f;
            browsHeight = browsAngle = earsLength = earsPointing = 0f;
        }

        public NPCCharacterData Clone()
        {
            var clone = (NPCCharacterData)this.MemberwiseClone();
            clone.facialBlendShapes = new List<BlendShapeValue>();
            foreach (var bs in this.facialBlendShapes)
            {
                clone.facialBlendShapes.Add(new BlendShapeValue(bs.shapeName, bs.weight));
            }
            return clone;
        }
    }
}
