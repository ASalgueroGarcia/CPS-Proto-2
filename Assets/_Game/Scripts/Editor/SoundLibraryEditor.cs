using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SoundLibrary))]
public class SoundLibraryEditor : Editor
{
    private SerializedProperty soundsProperty;
    private bool showSounds = true;

    private void OnEnable()
    {
        soundsProperty = serializedObject.FindProperty("sounds");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SoundLibrary library = (SoundLibrary)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Sound Library", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Librería de sonidos reutilizable. Puedes crear múltiples librerías para organizar sonidos por categorías (UI, Player, Enemies, etc.)", MessageType.Info);

        EditorGUILayout.Space(5);

        // Botón para añadir sonido rápido
        if (GUILayout.Button("➕ Añadir Nuevo Sonido", GUILayout.Height(30)))
        {
            soundsProperty.arraySize++;
            serializedObject.ApplyModifiedProperties();
        }

        EditorGUILayout.Space(5);

        // Mostrar cantidad de sonidos
        showSounds = EditorGUILayout.Foldout(showSounds, $"Sonidos ({soundsProperty.arraySize})", true, EditorStyles.foldoutHeader);

        if (showSounds)
        {
            EditorGUI.indentLevel++;

            for (int i = 0; i < soundsProperty.arraySize; i++)
            {
                SerializedProperty soundProp = soundsProperty.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                SerializedProperty nameProp = soundProp.FindPropertyRelative("name");
                string displayName = string.IsNullOrEmpty(nameProp.stringValue) ? $"Sound {i}" : nameProp.stringValue;

                bool foldout = EditorGUILayout.Foldout(
                    EditorPrefs.GetBool($"SoundLibrary_{target.GetEntityId()}_Sound_{i}", false),
                    displayName,
                    true
                );
                EditorPrefs.SetBool($"SoundLibrary_{target.GetEntityId()}_Sound_{i}", foldout);

                // Botón de eliminar
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("✖", GUILayout.Width(25), GUILayout.Height(18)))
                {
                    soundsProperty.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    break;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                if (foldout)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(soundProp.FindPropertyRelative("name"));

                    // Detectar cambios en el AudioClip
                    SerializedProperty clipProp = soundProp.FindPropertyRelative("clip");
                    AudioClip previousClip = clipProp.objectReferenceValue as AudioClip;
                    EditorGUILayout.PropertyField(clipProp);
                    AudioClip newClip = clipProp.objectReferenceValue as AudioClip;

                    // Si se asignó un nuevo clip y el nombre está vacío, usar el nombre del archivo
                    if (newClip != null && newClip != previousClip)
                    {
                        SerializedProperty nameProperty = soundProp.FindPropertyRelative("name");
                        if (string.IsNullOrWhiteSpace(nameProperty.stringValue))
                        {
                            nameProperty.stringValue = newClip.name;
                        }
                    }

                    EditorGUILayout.PropertyField(soundProp.FindPropertyRelative("volume"));
                    EditorGUILayout.PropertyField(soundProp.FindPropertyRelative("pitch"));
                    EditorGUILayout.PropertyField(soundProp.FindPropertyRelative("loop"));
                    EditorGUILayout.PropertyField(soundProp.FindPropertyRelative("randomizePitch"));

                    SerializedProperty randomizePitchProp = soundProp.FindPropertyRelative("randomizePitch");
                    if (randomizePitchProp.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(soundProp.FindPropertyRelative("pitchVariation"));
                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();

        // Información útil al final
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("💡 Tip: Asigna esta librería al AudioManager en el campo 'Sound Libraries' para que los sonidos estén disponibles en el juego.", MessageType.None);
    }
}
#endif
