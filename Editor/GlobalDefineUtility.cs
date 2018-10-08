using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parabox.Debug.DefineManager
{
	enum Compiler
	{
		CSharp = 0,
		Editor = 1,
		Platform = 2
	}

	static class GlobalDefineUtility
	{
		// http://forum.unity3d.com/threads/93901-global-define/page2
		// Do not modify these paths
		const string k_CSharpPath = "Assets/mcs.rsp";
		const string k_EditorPath = "Assets/gmcs.rsp";

		public static string[] GetDefines(Compiler compiler)
		{
			if (compiler == Compiler.CSharp)
				return ParseRspFile(k_CSharpPath);
			else if(compiler == Compiler.Editor)
				return ParseRspFile(k_EditorPath);

			return null;
		}

		public static void SetDefines(Compiler compiler, string[] defs)
		{
			switch (compiler)
			{
				case Compiler.CSharp:
					WriteDefines(k_CSharpPath, defs);
					break;

				case Compiler.Editor:
					WriteDefines(k_EditorPath, defs);
					break;
			}

			string first = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories).FirstOrDefault();

			if(!string.IsNullOrEmpty(first))
				AssetDatabase.ImportAsset(first);
		}

		public static string[] ParseRspFile(string path)
		{
			if (!File.Exists(path))
				return new string[0];

			string[] lines = File.ReadAllLines(path);
			List<string> defs = new List<string>();

			foreach (var line in lines)
			{
				if (line.StartsWith("-define:"))
					defs.AddRange(line.Replace("-define:", "").Split(';'));
			}

			return defs.ToArray();
		}

		public static void WriteDefines(string path, string[] defs)
		{
			if (defs == null || (defs.Length < 1 && File.Exists(path)))
			{
				File.Delete(path);
				File.Delete(path + ".meta");
				AssetDatabase.Refresh();
				return;
			}

			StringBuilder sb = new StringBuilder();

			sb.Append("-define:");

			for (int i = 0; i < defs.Length; i++)
			{
				sb.Append(defs[i]);
				if (i < defs.Length - 1) sb.Append(";");
			}

			using (StreamWriter writer = new StreamWriter(path, false))
			{
				writer.Write(sb.ToString());
			}
		}
	}
}
