using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BotBase))]
public class BotBaseEditor : Editor
{
    SerializedProperty movesProp;

    void OnEnable()
    {
        movesProp = serializedObject.FindProperty("moves");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (movesProp.arraySize != 4)
        {
            movesProp.arraySize = 4;
        }

        // Draw all other properties in BotBase except "moves"
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.name == "moves") continue; // skip moves — we'll draw it custom
            EditorGUILayout.PropertyField(prop, true);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Moves", EditorStyles.boldLabel);

        for (int i = 0; i < movesProp.arraySize; i++)
        {
            var moveProp = movesProp.GetArrayElementAtIndex(i);
            var actionsProp = moveProp.FindPropertyRelative("actions");

            EditorGUILayout.BeginVertical("box");

            // Draw all properties in Move
            SerializedProperty moveIter = moveProp.Copy();
            SerializedProperty endProp = moveIter.GetEndProperty();

            moveIter.NextVisible(true); // enter first child

            while (moveIter.propertyPath != endProp.propertyPath)
            {
                if (moveIter.name != "actions")
                {
                    EditorGUILayout.PropertyField(moveIter, true);
                }

                if (!moveIter.NextVisible(false)) break;
            }

            EditorGUILayout.LabelField("Actions", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            for (int j = 0; j < actionsProp.arraySize; j++)
            {
                var actionProp = actionsProp.GetArrayElementAtIndex(j);

                // Get type name from managed reference
                string typeName = "Action";
                if (actionProp.managedReferenceValue != null)
                {
                    typeName = actionProp.managedReferenceValue.GetType().Name;
                }
                else
                {
                    // Fallback: extract from fullTypeName string if it's null (rare, but can happen when serialized)
                    var fullTypeName = actionProp.managedReferenceFullTypename;
                    if (!string.IsNullOrEmpty(fullTypeName))
                    {
                        // Unity serializes as "AssemblyName TypeName"
                        var parts = fullTypeName.Split(' ');
                        if (parts.Length == 2)
                            typeName = parts[1];
                    }
                }

                typeName = System.Text.RegularExpressions.Regex.Replace(typeName, "(\\B[A-Z])", " $1");

                EditorGUILayout.PropertyField(actionProp, new GUIContent(typeName), true);

                if (GUILayout.Button($"Remove {typeName}"))
                {
                    actionsProp.DeleteArrayElementAtIndex(j);
                    break;
                }
            }

            EditorGUI.indentLevel--;

            if (GUILayout.Button("Add Action"))
            {
                GenericMenu menu = new GenericMenu();
                AddSubclassOption<DamageAction>(menu, actionsProp);
                AddSubclassOption<StatusEffectAction>(menu, actionsProp);
                AddSubclassOption<ExtraTurnAction>(menu, actionsProp);
                AddSubclassOption<RemoveNegEffectsAction>(menu, actionsProp);
                AddSubclassOption<RemovePosEffectsAction>(menu, actionsProp);
                AddSubclassOption<HealAction>(menu, actionsProp);
                AddSubclassOption<ReviveAction>(menu, actionsProp);
                // Add more types as needed
                menu.ShowAsContext();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void AddSubclassOption<T>(GenericMenu menu, SerializedProperty listProp) where T : MoveAction, new()
    {
        menu.AddItem(new GUIContent(typeof(T).Name), false, () =>
        {
            var instance = new T();
            listProp.serializedObject.Update();
            listProp.arraySize++;
            var element = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
            element.managedReferenceValue = instance;
            listProp.serializedObject.ApplyModifiedProperties();
        });
    }
}