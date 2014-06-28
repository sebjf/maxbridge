using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Messaging;

public static class TypeConversionExtensions {

	public static bool IsValid(this string str)
	{
		if(str == null)
			return false;
		if(str.Length <= 0)
			return false;

		return true;
	}

	public static object GetProperty(this IList<MaterialProperty> list, string property_name)
	{
		foreach(var m in list)
		{
			if( m.m_name == property_name || m.m_alias == property_name)
			{
				return m.m_value;
			}
		}
		
		throw new KeyNotFoundException("Property " + property_name + " not found.");
	}

	public static string PrintInfo(this MaterialInformation settings)
	{
		string s = "";
		s += "Class Name: " + settings.m_className + System.Environment.NewLine;
		foreach(var p in settings.MaterialProperties)
		{
			s += (p.m_name + " (" + p.m_alias + ") " + p.m_value + System.Environment.NewLine);
		}
		return s;
	}

	public static Vector4 UnityBridgeObjectToVector(this object value)
	{
		if(value is fRGBA)
		{
			fRGBA color_value = (fRGBA)value;
			return new Vector4(color_value.r, color_value.g, color_value.b, color_value.a);
		}
		if(!float.IsNaN((float)value))
		{
			return new Vector4((float)value, (float)value, (float)value, (float)value);
		}
		
		throw new UnityException("Could not convert " + value.GetType().Name + " to Vector");
	}
	
	public static float UnityBridgeObjectToFloat(this object value)
	{
		if(!float.IsNaN((float)value)){
			return (float)value;
		}
		
		throw new UnityException("Could not convert " + value.GetType().Name + " to Float");
	}
	
	public static Color UnityBridgeObjectToColor(this object value)
	{
		if(value is fRGBA)
		{
			fRGBA color_value = (fRGBA)value;
			return new Color(color_value.r, color_value.g, color_value.b, color_value.a);
		}
		
		throw new UnityException("Could not convert " + value.GetType().Name + " to Color");
	}
}
