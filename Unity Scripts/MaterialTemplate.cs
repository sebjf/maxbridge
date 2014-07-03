using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Messaging;


public class MaterialTemplate : MonoBehaviour {

	[Serializable]
	public class ParameterMap
	{
		public string m_sourceName;
		public string m_destinationName;
	}

	public Material m_template;
	public List<ParameterMap> m_parameterMapping;

	public MaterialTemplate()
	{
		m_parameterMapping = new List<ParameterMap>();
	}

	public Material CreateNewInstance(MaterialInformation settings)
	{
		/* Create the new material object */
		Material material = new Material(m_template);

		/* Set the parameters */
		foreach(var p in m_parameterMapping){

			if(!p.m_sourceName.IsValid() | !p.m_destinationName.IsValid()){
				continue;
			}

			SetGenericMaterialProperty(material, 
			                           p.m_destinationName, 
			                           settings.MaterialProperties.GetProperty(p.m_sourceName));
		}

		return material;
	}

	protected void SetGenericMaterialProperty(Material destination, string property_name, object value)
	{
		try{

			ShaderUtil.ShaderPropertyType type = GetPropertyType(destination, property_name);
			switch(type)
			{
			case ShaderUtil.ShaderPropertyType.Color:
				destination.SetColor(property_name, value.UnityBridgeObjectToColor());
				break;
			case ShaderUtil.ShaderPropertyType.Range:
			case ShaderUtil.ShaderPropertyType.Float:
				destination.SetFloat(property_name, value.UnityBridgeObjectToFloat());
				break;
			case ShaderUtil.ShaderPropertyType.Vector:
				destination.SetVector(property_name, value.UnityBridgeObjectToVector());
				break;
			case ShaderUtil.ShaderPropertyType.TexEnv:
				destination.SetTexture(property_name, TextureManager.Instance.ResolveMap( destination.name + "_" + property_name, value));
				break;
			default:
				Debug.Log("Unknown shader type " + type.ToString());
				break;
			}

		}
		catch(KeyNotFoundException e)
		{
			Debug.Log(e.Message);
		}
	}
	
	protected ShaderUtil.ShaderPropertyType GetPropertyType(Material destination, string property_name)
	{
		var count = ShaderUtil.GetPropertyCount(destination.shader);
		for(int i = 0; i < count; i++)
		{
			if(ShaderUtil.GetPropertyName(destination.shader, i) == property_name){
				return ShaderUtil.GetPropertyType(destination.shader, i);
			}
		}

		throw new KeyNotFoundException("Could not find property " + property_name + " in " + destination.shader.name);
	}

}
