using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(ScriptingDefineObject))]
public class ScriptingDefineEditor : Editor
{
	ReorderableList m_ReorderableList;

	string[] m_BuildTargetDisplayNames;
	BuildTargetGroup[] m_BuildTargetValues;

	SerializedProperty m_BuildTarget;
	SerializedProperty m_Defines;
	SerializedProperty m_IsApplied;
	BuildTargetGroup m_CurrentTargetGroup;

	void OnEnable()
	{
		m_BuildTargetValues = (BuildTargetGroup[]) System.Enum.GetValues(typeof(BuildTargetGroup));
		m_BuildTargetDisplayNames = m_BuildTargetValues.Select(x => x.ToString()).ToArray();

		m_BuildTarget = serializedObject.FindProperty("m_BuildTarget");
		m_CurrentTargetGroup = (BuildTargetGroup) m_BuildTarget.intValue;

		SetBuildTarget(m_CurrentTargetGroup == BuildTargetGroup.Unknown
			? BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)
			: m_CurrentTargetGroup);

		m_ReorderableList = new ReorderableList(serializedObject, m_Defines);
		m_ReorderableList.drawHeaderCallback += OnDrawHeader;
		m_ReorderableList.drawElementCallback += OnDrawListElement;
	}

	void OnDisable()
	{
		if (!m_IsApplied.boolValue)
		{
			if (EditorUtility.DisplayDialog("Unsaved Changes", "Would you like to save changes to the scripting defines?", "Yes",
				"No"))
				ApplyDefines();
		}
	}

	void SetBuildTarget(BuildTargetGroup target)
	{
		m_CurrentTargetGroup = target;
		m_BuildTarget.intValue = (int) target;

		m_Defines = serializedObject.FindProperty("m_Defines");
		m_IsApplied = serializedObject.FindProperty("m_IsApplied");

		var defs = GetScriptingDefineSymbols((BuildTargetGroup) m_BuildTarget.enumValueIndex);
		m_Defines.arraySize = defs.Length;
		for (int i = 0; i < defs.Length; i++)
			m_Defines.GetArrayElementAtIndex(i).stringValue = defs[i];

		m_IsApplied.boolValue = true;

		serializedObject.ApplyModifiedProperties();
	}

	string[] GetScriptingDefineSymbols(BuildTargetGroup group)
	{
		string res = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
		return res.Split(';');
	}

	void ApplyDefines()
	{
		string[] arr = new string[m_Defines.arraySize];

		for (int i = 0, c = arr.Length; i < c; i++)
			arr[i] = m_Defines.GetArrayElementAtIndex(i).stringValue;

		PlayerSettings.SetScriptingDefineSymbolsForGroup(m_CurrentTargetGroup, string.Join(";", arr));

		m_IsApplied.boolValue = true;

		serializedObject.ApplyModifiedProperties();
	}

	void OnDrawHeader(Rect rect)
	{
		var cur = ((BuildTargetGroup) (m_BuildTarget.intValue));
		GUI.Label(rect, cur.ToString(), EditorStyles.boldLabel);
	}

	void OnDrawListElement(Rect rect, int index, bool isactive, bool isfocused)
	{
		var element = m_ReorderableList.serializedProperty.GetArrayElementAtIndex(index);

		EditorGUIUtility.labelWidth = 4;
		EditorGUI.PropertyField(new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight), element);
		EditorGUIUtility.labelWidth = 0;
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		var cur = ((BuildTargetGroup) (m_BuildTarget.intValue));

		GUILayout.Label("Editing: " + cur.ToString(), EditorStyles.boldLabel);

		EditorGUI.BeginChangeCheck();
		cur = (BuildTargetGroup) EditorGUILayout.EnumPopup(cur);
		if(EditorGUI.EndChangeCheck())
			SetBuildTarget(cur);

		EditorGUI.BeginChangeCheck();
		m_ReorderableList.DoLayoutList();
		if (EditorGUI.EndChangeCheck())
			m_IsApplied.boolValue = false;

		GUILayout.BeginHorizontal();

		GUILayout.FlexibleSpace();

		bool wasEnabled = GUI.enabled;

		GUI.enabled = !m_IsApplied.boolValue;

		if (GUILayout.Button("Apply", EditorStyles.miniButton))
			ApplyDefines();

		GUI.enabled = wasEnabled;

		GUILayout.EndHorizontal();

		serializedObject.ApplyModifiedProperties();
	}
}
