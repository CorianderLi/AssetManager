using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UAsset
{ 
	public class AssetEditorUtility
	{
		[MenuItem ("Assets/Copy Asset Path")]
		static void CopyAssetPath ()
		{
			if (EditorApplication.isCompiling) {
				return;
			}
			string path = AssetDatabase.GetAssetPath (Selection.activeInstanceID);   
			GUIUtility.systemCopyBuffer = path;
			Debug.Log (string.Format ("systemCopyBuffer: {0}", path));
		}

		[MenuItem ("Assets/Build Manifest")]
		static void BuildAssetManifest ()
		{
			if (EditorApplication.isCompiling) {
				return;
			}
			AssetManifest.Instance.Build ();
		}
	}
}