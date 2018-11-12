using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Parabox.Debug.DefineManager
{
	public class ScriptingDefineWindow : EditorWindow
	{
		[MenuItem("Window/Plugins/Scripting Define Symbol Editor")]
		static void Init()
		{
			GetWindow<ScriptingDefineWindow>(true, "Define Symbol Editor", true);
		}

		Editor m_Editor;
		ScriptingDefineObject m_Asset;

		void OnEnable()
		{
			m_Asset = ScriptableObject.CreateInstance<ScriptingDefineObject>();
			m_Editor = Editor.CreateEditor(m_Asset);
		}

		void OnDisable()
		{
			Object.DestroyImmediate(m_Editor);
			Object.DestroyImmediate(m_Asset);
		}

		void OnGUI()
		{
			m_Editor.OnInspectorGUI();
		}
	}
}
