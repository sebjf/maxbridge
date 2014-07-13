using UnityEngine;
using UnityEditor;
using System.Collections;
using MaxUnityBridge;

public class ImporterWindow : EditorWindow {

	UnityImporter importer = new UnityImporter();
	MaterialsBinding binding;

	public ImporterWindow ()
	{
		binding = new MaterialsBinding(importer);
	}

	[MenuItem ("MaxUnityBridge/ImporterWindow")]

	public static void  ShowWindow () {
		EditorWindow.GetWindow(typeof(ImporterWindow));
	}
	
	void OnGUI () {
		// The actual window code goes here
		if(GUILayout.Button("Do Import"))
		{
			importer.DoImport();
		}

		if(GUILayout.Button("Get Materials"))
		{
			binding.UpdateNodeMaterials(Selection.gameObjects);
		}

		if(GUILayout.Button("Print Material Properties"))
		{
			binding.ShowMaterialProperties(Selection.gameObjects);
		}

	}
}
