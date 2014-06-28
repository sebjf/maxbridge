using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(MaterialTemplate))]
public class MaterialTemplateEditor : Editor {

	protected MaterialTemplate template { get { return (MaterialTemplate)target; }}

	public override void OnInspectorGUI()
	{
		template.m_template = EditorGUILayout.ObjectField("Material", template.m_template, typeof(Material), false) as Material;

		foreach(var p in template.m_parameterMapping){
			EditorGUILayout.BeginHorizontal();
			p.m_sourceName = EditorGUILayout.TextField(p.m_sourceName);
			p.m_destinationName = EditorGUILayout.TextField(p.m_destinationName);
			EditorGUILayout.EndHorizontal();
		}


		Rect buttonrect = EditorGUILayout.BeginHorizontal("Button");
		if(GUI.Button(buttonrect,GUIContent.none))
		{
			template.m_parameterMapping.Add(new MaterialTemplate.ParameterMap());
		}
		GUILayout.Label("Add");
		EditorGUILayout.EndHorizontal();



	}
	
}
