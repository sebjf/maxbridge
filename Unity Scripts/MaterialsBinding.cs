using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Messaging;
using MaxUnityBridge;

public class MaterialsBinding {
	
	protected UnityImporter m_importer;
	protected MaterialManager m_materialManager;
	protected TemplateManager m_templateManager;
	protected TextureManager m_textureManager;

	public MaterialsBinding (UnityImporter importer)
	{
		m_importer = importer; //the max side of the code is fully asynchronous and reentrant
		m_materialManager = new MaterialManager(importer);
		m_templateManager = new TemplateManager(importer);
		m_textureManager = new TextureManager(importer);
	}

	public void DoMapTest(IEnumerable<GameObject> scene_nodes)
	{
		foreach(var n in scene_nodes)
		{
			var m = m_importer.GetMaterials(n.name).GetEnumerator();
			if(m.MoveNext())
			{
				if(m.Current != null) //this means the object was found, but no material is applied in Max
				{
					var obj = m.Current.MaterialProperties.GetProperty("diffuseMap");
					MapReference map = obj as MapReference;
					m_importer.GetMap(map, 512, 512, @"D:\map_test.bmp");
				}
			}
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
					System.IO.StreamWriter fs = new System.IO.StreamWriter(string.Format("{0}\\{1} properties.txt", Application.dataPath, n.name));
					fs.Write(m.Current.PrintInfo());
					fs.Close();
				}
			}
		}
	}

	public void UpdateNodeMaterials(IEnumerable<GameObject> scene_nodes)
	{
		foreach(var n in scene_nodes)
		{
			UpdateNodeMaterials(n);
		}
	}

	protected void UpdateNodeMaterials(GameObject node)
	{
		Renderer renderer = node.GetComponent<Renderer>();
		if(renderer != null)
		{
			UpdateNodeMaterials(renderer);
		}
	}

	protected void UpdateNodeMaterials(Renderer node_renderer)
	{
		/* use shared materials, because using materials results in copies of the materials being made which add '(instance)' 
		 * to their name, breaking sub material indexing for those that want to use 3rd party importers for geometry */
		Material[] materials = node_renderer.sharedMaterials; 

		for(int i = 0; i < materials.Length; i++)
		{
			int index = -1;

			Match index_match = Regex.Match(materials[i].name, @":[\d]+$");
			if(index_match.Success)
			{
				index = int.Parse(index_match.Value.Substring(1));
			}

			MaterialInformation settings = m_materialManager.ResovleMaterial(node_renderer.gameObject.name, index);
			MaterialTemplate template = m_templateManager.ResolveTemplate(node_renderer.gameObject);

			materials[i] = CreateFromTemplate(materials[i], template, settings);
		}

		node_renderer.sharedMaterials = materials;
	}

	protected Material CreateFromTemplate(Material existing, MaterialTemplate template, MaterialInformation settings)
	{
		if(template == null){
			return existing;
		}
		if(settings == null){
			return existing;
		}
		Material m = template.CreateNewInstance(settings);
		if(existing != null){
			m.name = existing.name;
		}
		return m;
	}

}
