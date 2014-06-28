using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Messaging;

public static class TypeConversionExtensions {

	public static string ContentsToString<TKey, TValue>(this Dictionary<TKey,TValue> dictionary)
	{
		string s = "";
		foreach(var k in dictionary.Keys)
		{
			s += (k.ToString() + " : " + dictionary[k] + System.Environment.NewLine);
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
