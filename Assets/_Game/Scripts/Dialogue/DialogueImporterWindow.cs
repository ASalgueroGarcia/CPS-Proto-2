using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System;
using TMPro;
using System.Text;

public class DialogueImporterWindow : EditorWindow
{
    private const int MAX_LINES_BARK = 1;
    private const int MAX_LINES_CONVERSATION = 3;
    private const int MIN_FONT_SIZE = 28;
    private const char CSV_SEPARATOR = ',';
    
    [SerializeField] private TextAsset csvFile;
    [SerializeField] private DialogueDatabase db;
    [SerializeField] private GameObject barkSignPrefab;
    [SerializeField] private GameObject conversationUIPrefab;

    [MenuItem("Tools/Dialogue/Import & Validate CSV")]
    public static void ShowWindow()
    {
        GetWindow<DialogueImporterWindow>("Dialogue Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Import and Validate the Dialogues", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);
        db = (DialogueDatabase)EditorGUILayout.ObjectField("Dialogue Database", db, typeof(DialogueDatabase), false);

        EditorGUILayout.Space();
        GUILayout.Label("Prefabs to validate the character overflow (TMP)", EditorStyles.boldLabel);
        barkSignPrefab = (GameObject)EditorGUILayout.ObjectField("Bark Sign Prefab", barkSignPrefab, typeof(GameObject), false);
        conversationUIPrefab = (GameObject)EditorGUILayout.ObjectField("Conversation UI Sign Prefab", conversationUIPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();

        GUI.enabled = csvFile != null && db != null;
        if(GUILayout.Button("Import and Validate", GUILayout.Height(35)))
            ImportAndValidate();
        
        GUI.enabled = true;
    }

    private void ImportAndValidate()
    {
        List<DialogueEntry> parsedEntries = ParseCSV(csvFile.text);
        db.SetEntries(parsedEntries);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        int warnings = ValidateTextOverflow(parsedEntries);
        Debug.Log($"[DialogueImporter] Imported {parsedEntries.Count} lines.");
        if(warnings > 0)
            Debug.LogWarning($"[DialogueImporter] Overflow warnings: {warnings}.");
    }

    private List<DialogueEntry> ParseCSV(string csv)
    {
        var entries = new List<DialogueEntry>();
        string[] lines = csv.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

        // Skip first line (header)
        for(int i = 1; i < lines.Length; i++)
        {
            List<string> cols = SplitCsvLine(lines[i]);
            if(cols.Count < DialogueDatabase.NumberOfRequiredColuns)
            {
                Debug.LogWarning($"[DialogueImporter] File number {i + 1} ignored because it has less than {DialogueDatabase.NumberOfRequiredColuns} columns.");
                continue;
            }

            if(!Enum.TryParse(cols[1].Trim(), out DialogueLineType lineType))
            {
                Debug.LogWarning($"[DialogueImporter] Unknown line type '{cols[1]}' in file number {i + 1}. This line will be ignored.");
                continue;
            }

            if(!Enum.TryParse(cols[2].Trim(), out Speaker speaker))
            {
                Debug.LogWarning($"[DialogueImporter] Unknown speaker '{cols[2]}' in file number {i + 1}. This line will be ignored.");
                continue;
            }

            entries.Add(new DialogueEntry
            {
                id = cols[0].Trim(),
                type = lineType,
                speaker = speaker,
                textEN = cols[3],
                textES = cols[4],
                nextId = cols.Count > 5 ? cols[5].Trim() : string.Empty
            });
        }

        return entries;
    }

    private int ValidateTextOverflow(List<DialogueEntry> entries)
    {
        if(barkSignPrefab == null && conversationUIPrefab == null)
        {
            Debug.LogWarning("[DialogueImporter] No prefabs assigned for text overflow validation. Skipping validation.");
            return 0;
        }   

        int warnings = 0;

        GameObject tmpBark = Instantiate(barkSignPrefab);
        GameObject tmpConv = Instantiate(conversationUIPrefab);

        tmpBark.hideFlags = HideFlags.HideAndDontSave;
        tmpConv.hideFlags = HideFlags.HideAndDontSave;

        try
        {
            TMP_Text barkText = tmpBark.GetComponentInChildren<TMP_Text>();
            TMP_Text convText = tmpConv.GetComponentInChildren<TMP_Text>();

            CheckFontSize(barkText, "Bark Sign Prefab");
            CheckFontSize(convText, "Conversation Sign Prefab");

            foreach(var entry in entries)
            {
                TMP_Text targetTmp = entry.type == DialogueLineType.Bark ? barkText : convText;
                int maxAllowedLines = entry.type == DialogueLineType.Bark ? MAX_LINES_BARK : MAX_LINES_CONVERSATION;

                for(int i = 0; i < DialogueDatabase.NumberOfLanguages; i++)
                {
                    string languageCode = ((GameLanguage)i).ToString();
                    string text = GetTextByLanguage(entry, (GameLanguage)i);
                    warnings += CheckSingleText(targetTmp, entry.id, languageCode, text, maxAllowedLines);
                }
            }
        }
        finally
        {
            DestroyImmediate(tmpBark);
            DestroyImmediate(tmpConv);
        }

        return warnings;
    }

    private int CheckSingleText(TMP_Text tmp, string id, string lang, string content, int maxLines)
    {
        if (string.IsNullOrEmpty(content)) return 0;

        tmp.text = content;
        tmp.ForceMeshUpdate();

        int actualLines = tmp.textInfo.lineCount;
        if (actualLines > maxLines)
        {
            Debug.LogWarning($"[Overflow] ID '{id}' ({lang}) ocupa {actualLines} líneas (máximo permitido: {maxLines}). Texto: \"{content}\"");
            return 1;
        }

        return 0;
    }

    private void CheckFontSize(TMP_Text tmp, string prefabName)
    {
        if (tmp != null && tmp.fontSize < MIN_FONT_SIZE)
        {
            Debug.LogWarning($"[DialogueImporter] The TMP_Text in '{prefabName}' has a font size of {tmp.fontSize}. Which is smaller than the recommended minimum of {MIN_FONT_SIZE}.");
        }
    }

    private string GetTextByLanguage(DialogueEntry entry, GameLanguage lang)
    {
        return lang switch
        {
            GameLanguage.EN => entry.textEN,
            GameLanguage.ES => entry.textES,
            _ => throw new ArgumentOutOfRangeException(nameof(lang), $"Unsupported language: {lang}")
        };
    }

    // This method splits a CSV line into columns, taking into account quoted fields that may contain commas.
    private List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == CSV_SEPARATOR && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
    }
}