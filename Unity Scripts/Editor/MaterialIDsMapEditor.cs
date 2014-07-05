using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MaxUnityBridge;

[CustomEditor(typeof(MaterialIDsMap))]
public class MaterialIDsMapEditor : Editor {

	protected MaterialIDsMap map { get { return (MaterialIDsMap)target; }}

	public override void OnInspectorGUI()
	{
		for(int i = 0; i < map.m_materialIds.Count; i++)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(i.ToString());
			int value;
			if(int.TryParse(EditorGUILayout.TextField(map.m_materialIds[i].ToString()), out value)){
				map.m_materialIds[i] = value;
			}
			EditorGUILayout.EndHorizontal();
		}

		Rect buttonrect = EditorGUILayout.BeginHorizontal("Button");
		if(GUI.Button(buttonrect,GUIContent.none))
		{
			int material_slots = map.gameObject.renderer.sharedMaterials.Length;

			/* Create a new list, of the exact length of the number of materials, with the content of the old one as much as possible */
			List<int> newList = new List<int>();
			for(int i = 0; i < material_slots; i++)
			{
				if(map.m_materialIds.Count > i){
					newList.Add( map.m_materialIds[i]);
				}else
				{
					newList.Add(0);
				}
			}
			map.m_materialIds = newList;
		}
		GUILayout.Label("Update Material Slots");
		EditorGUILayout.EndHorizontal();
	}
}
