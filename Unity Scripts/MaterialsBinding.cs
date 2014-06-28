using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Messaging;
using MaxUnityBridge;

public class MaterialsBinding {
	
	protected UnityImporter m_importer;

	public MaterialsBinding (UnityImporter importer)
	{
		m_importer = importer; //the max side of the code is fully asynchronous and reentrant
	}
	
	public IEnumerable<string> GetSelectedNames()
	{
		foreach(var o in Selection.gameObjects)
		{
			yield return o.name;
		}
	}

	public void ShowMaterialProperties(IEnumerable<GameObject> scene_nodes)
	{
		foreach(var n in scene_nodes)
		{
			var m = m_importer.GetMaterials(n.name).GetEnumerator();
			if(m.MoveNext())
			{
				if(m.Current != null) //this means the object was found, but no material is applied in Max
				{
					EditorUtility.DisplayDialog(
						"Material for " + n.name,
						m.Current.MaterialProperties.ContentsToString(),
					    "OK"                   );

				}
			}
		}
	}

	public void UpdateNodeMaterials(IEnumerable<GameObject> scene_nodes)
	{
		foreach(var n in scene_nodes)
		{
			var m = m_importer.GetMaterials(n.name).GetEnumerator();
			if(m.MoveNext())
			{
				if(m.Current == null) //this means the object was found, but no material is applied in Max
				{
					SetMaterial(n, null);
					continue;
				}

				SetMaterial(n, CreateMaterialFromTemplate(GetTemplate(n), m.Current) );
			}
		}
	}

	protected MaterialTemplate GetTemplate(GameObject node)
	{
		return node.GetComponent<MaterialTemplate>();
	}

	protected void SetMaterial(GameObject node, Material material)
	{
		Renderer renderer = node.GetComponent<Renderer>();
		if(renderer != null)
		{
			renderer.material = material;
		}
	}

	public Material CreateMaterialFromTemplate(MaterialTemplate template, MaterialInformation settings)
	{
		Material material = template.CreateNewInstance(settings);
		return material;
	}



}
