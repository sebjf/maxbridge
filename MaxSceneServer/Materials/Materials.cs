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

            foreach (var p in EnumerateProperties(material))
            {
                m.MaterialProperties.Add(p.m_parameterName, p.GetValue());
                if (p.m_internalName != null)
                {
                    m.MaterialProperties.Add(p.m_internalName, p.GetValue());
                }
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
