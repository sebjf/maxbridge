using UnityEngine;
using System.Collections;
using System.IO;
using MaxUnityBridge;
using Messaging;

/* The texture manager is responsible for retrieving texture maps and providing them to 
 * the material templates in the most efficient way possible */
public class TextureManager {

	protected UnityImporter m_impoter;

	public string m_textureDirectory = "imported_textures";
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
		m_impoter = importer;
		m_instance = this;
	}

	public Texture2D ResolveMap(string map_name, object map_reference_value)
	{
		//for now we just get the map

		//create new map filename
		string directory = Application.dataPath;
		string subdirectory = m_textureDirectory;
		string map_extension = ".png";

		string filepath = directory + "\\" + subdirectory + "\\";
		string filename = map_name + map_extension;

		if(!(map_reference_value is MapReference)){
			throw new UnityException("Property is not a map!");
		}

		m_impoter.GetMap(map_reference_value as MapReference, width, height, filepath + filename);

		byte[] data = File.ReadAllBytes(filepath + filename);

		Texture2D texture = new Texture2D(width,height);
		texture.LoadImage(data);

		return texture;

	}
}
