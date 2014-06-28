using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messaging;
using Autodesk.Max;


namespace MaxSceneServer
{
    public partial class MaxSceneServer
    {
        IEnumerable<MaterialInformation> GetMaterials(MessageMaterialRequest request)
        {
            if (request.m_nodeName != null)
            {
                foreach (var node in GetNode(request.m_nodeName))
                {
                    yield return GetMaterialProperties(node.Mtl, request);
                }
            }
            if (request.m_handle > 0)
            {
                yield return GetMaterialProperties(_gi.Animatable.GetAnimByHandle(new UIntPtr(request.m_handle)) as IMtl, request);
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
            if (material == null)
            {
                return null;
            }

            MaterialInformation m = new MaterialInformation();
            m.m_className = material.ClassName;
            m.m_materialName = material.Name;
            m.m_handle = _gi.Animatable.GetHandleByAnim(material).ToUInt64();

            var prps = EnumerateProperties(material).ToList();

            foreach (var p in EnumerateProperties(material))
            {
                m.MaterialProperties.Add(new MaterialProperty(p.m_parameterName, p.m_internalName, p.GetValue()));
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
