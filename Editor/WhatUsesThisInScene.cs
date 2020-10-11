using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor.Experimental.SceneManagement;

public class WhatUsesThisInScene {
	[MenuItem("GameObject/What uses this in the scene?", false, 10)]
	[MenuItem("CONTEXT/Component/What uses this in the scene?", false, 10)]
	[MenuItem("Assets/What uses this in the scene?")]
	static void Execute() {
		Component[] allComponents =
			(PrefabStageUtility.GetCurrentPrefabStage() != null ?
			PrefabStageUtility.GetCurrentPrefabStage().scene :
			EditorSceneManager.GetActiveScene()).GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Component>()).ToArray();

		int iCount = 0;

		var selectedObj = Selection.activeObject;
		string selected = selectedObj.name;

		List<Object> thisObj = new List<Object>() { selectedObj };

		Debug.Log($"<color=#5C93B9>What uses <b>{selected}</b>?</color>", selectedObj);

		if (selectedObj is GameObject go) {
			Debug.Log($"<color=#5C93B9>The object is a GameObject. Will search for the components it contains too.</color>");
			thisObj.AddRange(go.GetComponents<Component>());
		}

		bool cancelled = false;

		try {
			for (int i = 0; i < allComponents.Length; i++) {
				if (Progress(i, allComponents.Length, 0, iCount)) {
					cancelled = true;
					goto End;
				}

				try {
					SerializedObject obj = new SerializedObject(allComponents[i]);

					SerializedProperty props = obj.GetIterator();
					int j = 0;
					while (props.Next(true)) {
						Object objRef = null;
						switch (props.propertyType) {
							case SerializedPropertyType.ExposedReference:
								objRef = props.exposedReferenceValue;
								break;
							case SerializedPropertyType.ObjectReference:
								objRef = props.objectReferenceValue;
								break;
						}
						if (objRef == null) continue;

						if (Progress(i, allComponents.Length, j, iCount)) {
							cancelled = true;
							goto End;
						}

						if (thisObj.Contains(objRef)) {
							string d = allComponents[i].gameObject.name + "/" + allComponents[i].GetType();
							Debug.Log($"<color=#8CA166>  {d}</color>", allComponents[i]);
							iCount++;
						}
						j++;
					}
				}
				catch { }
			}
		}
		catch { }

		End:
		EditorUtility.ClearProgressBar();

		string status = cancelled ? "cancalled" : "complete";
		Debug.Log($"<color=#5C93B9>Search {status}, found <b>{iCount}</b> result{(iCount == 1 ? "" : "s")}</color>");
	}

	[MenuItem("CONTEXT/Component/What uses this component?", false, 10)]
	public static void WhatUsesThisComponent(MenuCommand command) {
		Component c = (Component) command.context;
		var typeToSearch = c.GetType();
		var typeString = c.GetType().Name;

		Debug.Log("Finding all Prefabs and scenes that have the component " + c.GetType() + "...");

		string[] guids = AssetDatabase.FindAssets("t:scene t:prefab");

		int count = 0;

		for (int i = 0; i < guids.Length; i++) {
			string guid = guids[i];
			//Debug.Log(AssetDatabase.GUIDToAssetPath(guid));
			string myObjectPath = AssetDatabase.GUIDToAssetPath(guid);
			var asset = AssetDatabase.LoadAssetAtPath(myObjectPath, typeof(Object));
			Object[] myObjs;
			Scene s = default;
			if (asset is SceneAsset) {
				s = EditorSceneManager.OpenPreviewScene(myObjectPath);
				myObjs = s.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren(typeToSearch, true)).ToArray();
			}
			else {
				myObjs = AssetDatabase.LoadAllAssetsAtPath(myObjectPath);
			}

			if (EditorUtility.DisplayCancelableProgressBar("Searching in scenes/prefabs...", count + " matches. Progress: " + i + " / " + guids.Length + ". Current: " + asset.name, i / (guids.Length - 1f))) {
				goto End;
			}

			foreach (Object thisObject in myObjs) {
				if (typeToSearch.IsAssignableFrom(thisObject.GetType())) {
					Debug.Log("<color=red>" + typeString + "</color> Found in <color=green>" + thisObject.name + "</color> at " + myObjectPath, asset);
					count++;
					break;
				}
			}

			if (asset is SceneAsset) {
				EditorSceneManager.ClosePreviewScene(s);
			}
		}

		End:
		EditorUtility.ClearProgressBar();
	}

	static bool Progress(int i, int length, int j, int f) {
		return EditorUtility.DisplayCancelableProgressBar("Searching...", "Found in " + f + " fields. Components inspected: " + i + " / " + length + " (Fields inspected " + j + ")", i / (length - 1f));
	}
}
