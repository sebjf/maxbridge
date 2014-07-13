using UnityEngine;
using System.Collections;
using MaxUnityBridge;
using Messaging;

/* The template manager is responsible for selecting the best template to use to create a new material, based on what is, or
 what is not present, on each gameobject. Modify this class to control material selection. */
public class TemplateManager  {

	UnityImporter m_importer;

	public TemplateManager (UnityImporter importer)
	{
		m_importer = importer;
	}

	public IMaterialTemplate ResolveTemplate(GameObject node, MaterialInformation settings)
	{
		MaterialTemplate t = node.GetComponent<MaterialTemplate>();
		if(t == null)
		{
			Debug.LogError(string.Format("No template could be found for node {0}",node.name));
			t = AddDefault(node);
		}
		return t;
	}

	protected MaterialTemplate AddDefault(GameObject node)
	{
		MaterialTemplate template = node.AddComponent<MaterialTemplate>();
		template.m_template = Resources.Load<Material>("Diffuse");
		template.m_parameterMapping.Add(new MaterialTemplate.ParameterMap() { m_sourceName = "diffuse", m_destinationName = "_Color"});
		template.m_parameterMapping.Add(new MaterialTemplate.ParameterMap() { m_sourceName = "diffuseMap", m_destinationName = "_MainTex"});
		return template;
	}

}
