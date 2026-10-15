using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor shortcuts for the save files: the editor and builds share the same folder
/// (Application.persistentDataPath), so testing a fresh start means deleting the slots.
/// </summary>
public static class SaveMenu
{
    [MenuItem("Tools/Save System/Open Save Folder")]
    private static void OpenSaveFolder()
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }

    [MenuItem("Tools/Save System/Delete All Save Slots")]
    private static void DeleteAllSlots()
    {
        if (!EditorUtility.DisplayDialog("Delete all save slots",
                "Delete the 3 save slots in\n" + Application.persistentDataPath + "?\nSettings are kept.",
                "Delete", "Cancel"))
        {
            return;
        }

        var store = new JsonFileStore(Application.persistentDataPath);
        for (int slot = 0; slot < SaveService.SlotCount; slot++)
        {
            store.Delete(SaveService.FileNameOf(slot));
        }

        Debug.Log("[SaveMenu] Save slots deleted.");
    }
}
