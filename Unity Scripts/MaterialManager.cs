using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MaxUnityBridge;
using Messaging;

/* The MaterialManager is responsible for retrieving material settings from Max and invoking the template
 to construct the material. It is also responsible for caching where possible. Special cases such as 
 the shell material or the multi-material classes are handled here. */
public class MaterialManager {

	protected UnityImporter m_importer;
	
	public MaterialManager (UnityImporter importer)
	{
		m_importer = importer;
	}
	
	/* This method navigates the graph of material nodes to find */
	public MaterialInformation ResolveMaterialSettings(string node_name, int index)
	{
		MaterialInformation root_material = GetMaterial(node_name);
		
		if(root_material == null)
		{
			return null;
		}
		
		if(root_material.m_className == "Shell Material")
		{
			root_material = GetMaterial(root_material, 1);
		}
		
		if(root_material.m_className == "Multi/Sub-Object")
		{
			root_material = GetMaterial(root_material, index);
		}
		
		return root_material;
	}

	protected bool MaterialHasSubmaterials(MaterialInformation settings)
	{
		switch(settings.m_className)
		{
		case "Shell Material":
		case "Multi/Sub-Object":
			return true;
		default:
			return false;
		}
	}
	
	/* It is possible to match multiple materials, though unlikely. In this version, we will support only one material per object. */
	protected MaterialInformation GetMaterial(string node_name)
	{
		var m = m_importer.GetMaterials(node_name).GetEnumerator();
		if(m.MoveNext())
		{
			return m.Current; //this can be null, if no material has been set in max.
		}
		return null;
	}
	
	protected MaterialInformation GetMaterial(MaterialInformation root_material, int sub_index)
	{
		var m = m_importer.GetSubMaterials(root_material, sub_index).GetEnumerator();
		if(m.MoveNext())
		{
			return m.Current;
		}
		return null;
	}


}
