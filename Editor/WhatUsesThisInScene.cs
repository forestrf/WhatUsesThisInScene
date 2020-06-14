using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor.Experimental.SceneManagement;

public class WhatUsesThisInScene {
	[MenuItem("GameObject/What Uses This In The Scene", false, 10)]
	[MenuItem("CONTEXT/Component/What Uses This In The Scene", false, 10)]
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
		}
		finally { }

		End:
		EditorUtility.ClearProgressBar();

		string status = cancelled ? "cancalled" : "complete";
		Debug.Log($"<color=#5C93B9>Search {status}, found <b>{iCount}</b> result{(iCount == 1 ? "" : "s")}</color>");
	}

	static bool Progress(int i, int length, int j, int f) {
		return EditorUtility.DisplayCancelableProgressBar("Searching...", "Found in " + f + " fields. Components inspected: " + i + " / " + length + " (Fields inspected " + j + ")", i / (length - 1f));
	}
}
