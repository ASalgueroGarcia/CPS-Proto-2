using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemy)), CanEditMultipleObjects]
public class EnemyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        SerializedProperty settings = serializedObject.FindProperty("settings");
        string[] fields = {
            "maxHealth", "damage", "speed", "alertRange", "alertDuration", "awarenessIsPermanent",
            "roamRadius", "idleDuration", "patrolSpeedMultiplier", "attackRange", "windupDuration",
            "attackCooldown", "chaseLeashMultiplier"
        };
        foreach (string field in fields)
        {
            EditorGUILayout.PropertyField(settings.FindPropertyRelative(field));
            if (field == "patrolSpeedMultiplier")
            {
                SerializedProperty speed = settings.FindPropertyRelative("speed");
                SerializedProperty multiplier = settings.FindPropertyRelative(field);
                EditorGUI.showMixedValue = speed.hasMultipleDifferentValues || multiplier.hasMultipleDifferentValues;
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.FloatField("Calculated Patrol Speed", speed.floatValue * multiplier.floatValue);
                EditorGUI.showMixedValue = false;
            }
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("attackStrategyComponent"));
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentState"));
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.HelpBox("These prefab settings supply health, navigation speed and attack timing at spawn. Attack-specific settings are on the assigned strategy component.", MessageType.Info);
    }
}
