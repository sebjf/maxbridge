using UnityEngine;
using System.Collections;
using System.IO;
using MaxUnityBridge;
using Messaging;

/* The texture manager is responsible for retrieving texture maps and providing them to 
 * the material templates in the most efficient way possible */
public class TextureManager {

	protected UnityImporter m_importer;
	
	public int width = 1024;
	public int height = 1024;

	private static TextureManager m_instance;
	public static TextureManager Instance
	{
		get{
			return m_instance;
		}
	}

	public TextureManager (UnityImporter importer)
	{
		m_importer = importer;
		m_instance = this;
	}

	public Texture2D ResolveMap(string map_name, object map_reference_value)
	{
		//for now we just get the map

		//create new map filename
		if(!(map_reference_value is MapReference)){
			throw new UnityException("Property is not a map!");
		}

		long handle = (map_reference_value as MapReference).m_nativeHandle;

		byte[] data = MaterialsBinding.m_cache.GetTexture(handle);

		if(data == null)
		{
			data = m_importer.GetMap(map_reference_value as MapReference, width, height);
			MaterialsBinding.m_cache.SetTexture(handle, data);
		}

		Texture2D texture = new Texture2D(width,height);
		texture.LoadImage(data);

		return texture;

	}
}
