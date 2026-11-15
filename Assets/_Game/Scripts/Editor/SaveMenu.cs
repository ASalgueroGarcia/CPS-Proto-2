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
        // In Play Mode the game holds the active slot in memory and would write it straight back.
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Delete all save slots", "Stop Play Mode first.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Delete all save slots",
                "Delete the 3 save slots in\n" + Application.persistentDataPath + "?\nSettings are kept.",
                "Delete", "Cancel"))
        {
            return;
        }

        var store = new JsonFileStore(Application.persistentDataPath);
        for (int slot = 0; slot < SaveService.SlotCount; slot++)
        {
            try
            {
                store.Delete(SaveService.FileNameOf(slot));
            }
            catch (IOException e)
            {
                Debug.LogError($"[SaveMenu] Slot {slot + 1} wasn't deleted: {e.Message}");
            }
        }

        Debug.Log("[SaveMenu] Save slots deleted.");
    }
}
