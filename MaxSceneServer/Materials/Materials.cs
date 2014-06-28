using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messaging;
using Autodesk.Max;


namespace MaxSceneServer
{
    public static class DictionaryExtensions
    {
        public static void TryAddUnique<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (key == null)
            {
                return;
            }

            if (dictionary.ContainsKey(key))
            {
                return;
            }

            dictionary.Add(key, value);
        }
    }

    public partial class MaxSceneServer
    {

        IEnumerable<MaterialInformation> GetMaterials(MessageMaterialRequest request)
        {
            foreach (var node in GetNode(request.m_nodeName))
            {
                yield return GetMaterialProperties(node.Mtl, request);
            }           
        }

        MaterialInformation GetMaterialProperties(IMtl material, MessageMaterialRequest request)
        {
            if (material == null)
            {
                return null;
            }

            if (request.m_materialIndex < 0)
            {
                return GetMaterialProperties(material);
            }

            return GetMaterialProperties(material.GetSubMtl(request.m_materialIndex));
        }

        MaterialInformation GetMaterialProperties(IMtl material)
        {
            MaterialInformation m = new MaterialInformation();
            m.m_className = material.ClassName;

            var prps = EnumerateProperties(material).ToList();

            foreach (var p in EnumerateProperties(material))
            {
                m.MaterialProperties.TryAddUnique(p.m_parameterName, p.GetValue());
                m.MaterialProperties.TryAddUnique(p.m_internalName, p.GetValue());
            }

            return m;
        }

        MapReference GetMap(MapReference map)
        {
            Parameter mapParam = new Parameter(map.m_parameterReference);



            return new MapReference();
        }

    }
}
