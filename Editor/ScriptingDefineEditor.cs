using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Parabox.Debug.DefineManager
{
	[CustomEditor(typeof(ScriptingDefineObject))]
	public class ScriptingDefineEditor : Editor
	{
		const int k_CompilerCount = 3;
		ReorderableList m_ReorderableList;

//		string[] m_BuildTargetDisplayNames;
//		BuildTargetGroup[] m_BuildTargetValues;

		SerializedProperty m_Compiler;
		SerializedProperty m_BuildTarget;
		SerializedProperty m_Defines;
		SerializedProperty m_IsApplied;
		BuildTargetGroup m_CurrentTargetGroup;

		static class Styles
		{
			public static GUIStyle listContainer;
			static bool s_IsInitialized;

			public static void Init()
			{
				if (s_IsInitialized)
					return;
				s_IsInitialized = true;
				listContainer = new GUIStyle()
				{
					margin = new RectOffset(4, 4, 4, 4)
				};
			}
		}

		void OnEnable()
		{
//			m_BuildTargetValues = (BuildTargetGroup[]) System.Enum.GetValues(typeof(BuildTargetGroup));
//			m_BuildTargetDisplayNames = m_BuildTargetValues.Select(x => x.ToString()).ToArray();

			m_Compiler = serializedObject.FindProperty("m_Compiler");
			SetCompilerTarget((Compiler) m_Compiler.intValue);

			m_ReorderableList = new ReorderableList(serializedObject, m_Defines);
			m_ReorderableList.drawHeaderCallback += OnDrawHeader;
			m_ReorderableList.drawElementCallback += OnDrawListElement;
		}

		void OnDisable()
		{
			if (!m_IsApplied.boolValue)
			{
				if (EditorUtility.DisplayDialog("Unsaved Changes", "Would you like to save changes to the scripting defines?",
					"Yes",
					"No"))
					ApplyDefines();
			}
		}

		void SetCompilerTarget(Compiler compiler)
		{
			m_Compiler.intValue = (int) compiler;

			m_Defines = serializedObject.FindProperty("m_Defines");
			m_IsApplied = serializedObject.FindProperty("m_IsApplied");

			if (m_Compiler.intValue == (int) Compiler.Platform)
			{
				m_BuildTarget = serializedObject.FindProperty("m_BuildTarget");
				m_CurrentTargetGroup = (BuildTargetGroup) m_BuildTarget.intValue;

				SetBuildTarget(m_CurrentTargetGroup == BuildTargetGroup.Unknown
					? BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)
					: m_CurrentTargetGroup);
			}
			else
			{
				var defs = GlobalDefineUtility.GetDefines((Compiler) m_Compiler.intValue);

				m_Defines.arraySize = defs.Length;

				for (int i = 0; i < defs.Length; i++)
					m_Defines.GetArrayElementAtIndex(i).stringValue = defs[i];

				m_IsApplied.boolValue = true;
				serializedObject.ApplyModifiedProperties();
			}
		}

		void SetBuildTarget(BuildTargetGroup target)
		{
			m_CurrentTargetGroup = target;
			m_BuildTarget.intValue = (int) target;

			var defs = GetScriptingDefineSymbols((BuildTargetGroup) m_BuildTarget.enumValueIndex);
			m_Defines.arraySize = defs.Length;
			for (int i = 0; i < defs.Length; i++)
				m_Defines.GetArrayElementAtIndex(i).stringValue = defs[i];

			m_IsApplied.boolValue = true;
			serializedObject.ApplyModifiedProperties();
		}

		static string[] GetScriptingDefineSymbols(BuildTargetGroup group)
		{
			string res = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
			return res.Split(';');
		}

		void ApplyDefines()
		{
			string[] arr = new string[m_Defines.arraySize];

			for (int i = 0, c = arr.Length; i < c; i++)
				arr[i] = m_Defines.GetArrayElementAtIndex(i).stringValue;

			if(m_Compiler.intValue == (int) Compiler.Platform)
				PlayerSettings.SetScriptingDefineSymbolsForGroup(m_CurrentTargetGroup, string.Join(";", arr));
			else
				GlobalDefineUtility.SetDefines((Compiler) m_Compiler.intValue, arr);

			m_IsApplied.boolValue = true;

			serializedObject.ApplyModifiedProperties();

			GUI.FocusControl("");
		}

		void OnDrawHeader(Rect rect)
		{
			var cur = ((Compiler) m_Compiler.intValue).ToString();

			if (m_Compiler.intValue == (int) Compiler.Platform)
				cur += " " + ((BuildTargetGroup) (m_BuildTarget.intValue));

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
			Styles.Init();

			serializedObject.Update();

			Color oldColor = GUI.backgroundColor;

			GUILayout.Space(2);

			GUILayout.BeginHorizontal();

			for (int i = 0; i < k_CompilerCount; i++)
			{
				if (i == m_Compiler.intValue)
					GUI.backgroundColor = Color.gray;

				GUIStyle st;
				switch (i)
				{
					case 0:
						st = EditorStyles.miniButtonLeft;
						break;
					case k_CompilerCount - 1:
						st = EditorStyles.miniButtonRight;
						break;
					default:
						st = EditorStyles.miniButtonMid;
						break;
				}

				if (GUILayout.Button(((Compiler) i).ToString(), st))
				{
					m_Compiler.intValue = i;
					SetCompilerTarget((Compiler) i);
				}

				GUI.backgroundColor = oldColor;
			}

			GUILayout.EndHorizontal();

			if (m_Compiler.intValue == (int) Compiler.Platform)
			{
				var cur = ((BuildTargetGroup) (m_BuildTarget.intValue));

				GUILayout.Space(3);

				EditorGUI.BeginChangeCheck();
				cur = (BuildTargetGroup) EditorGUILayout.EnumPopup(cur);
				if (EditorGUI.EndChangeCheck())
					SetBuildTarget(cur);
			}

			EditorGUI.BeginChangeCheck();

			GUILayout.BeginVertical(Styles.listContainer);

			m_ReorderableList.DoLayoutList();
			if (EditorGUI.EndChangeCheck())
				m_IsApplied.boolValue = false;

			GUILayout.EndVertical();

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
}
