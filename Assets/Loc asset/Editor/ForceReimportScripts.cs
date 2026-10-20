#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Tools > Force Reimport Scripts
/// </summary>
public static class ForceReimportScripts
{
    [MenuItem("Tools/Force Reimport Scripts")]
    public static void DoReimport()
    {
        string[] paths = new[]
        {
            "Assets/Loc asset/scripts/NewspaperInteractable.cs",
            "Assets/Loc asset/scripts/TaskManager.cs",
            "Assets/Loc asset/scripts/GameIntroEvent.cs",
            "Assets/Loc asset/scripts/CharacterDesigner/NPCCharacterData.cs",
            "Assets/Loc asset/scripts/CharacterDesigner/NPCModularCharacter.cs",
            "Assets/Editor/CharacterDesigner/NPCAssetDatabase.cs",
            "Assets/Editor/CharacterDesigner/NPCCharacterBuilder.cs",
            "Assets/Editor/CharacterDesigner/NPCCharacterDesignerWindow.cs",
            "Assets/Editor/CharacterDesigner/NPCModularCharacterEditor.cs"
        };

        foreach (var p in paths)
        {
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[ForceReimport] Imported: " + p);
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        Debug.Log("[ForceReimport] Done! Check Console for compile errors.");
    }
}
#endif