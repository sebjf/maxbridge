using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MaxUnityBridge;
using Messaging;

/*Object to maintain caches of whatever we want in the plugin. These objects can be created or deleted as often as needed (probably every invocation).*/
public class Caching {
	
	protected Dictionary<IMaterialTemplate, Dictionary<ulong, Material>> m_materialsCache = new Dictionary<IMaterialTemplate, Dictionary<ulong, Material>>();

	public Material ResolveCachedMaterial(IMaterialTemplate template, MaterialInformation settings)
	{
		if(!m_materialsCache.ContainsKey(template))
		{
			m_materialsCache.Add(template, new Dictionary<ulong, Material>());
		}

		var sd = m_materialsCache[template];
		if(!sd.ContainsKey(settings.m_handle))
		{
			sd.Add(settings.m_handle, template.CreateNewInstance(settings));
		}

		return sd[settings.m_handle];
	}


	protected Dictionary<long, byte[]> m_texturesCache = new Dictionary<long, byte[]>();

	public byte[] GetTexture(long handle)
	{
		if(m_texturesCache.ContainsKey(handle))
		{
			return m_texturesCache[handle];
		}
		return null;
	}

	public void SetTexture(long handle, byte[] texture)
	{
		m_texturesCache.Add(handle, texture);
	}
}
