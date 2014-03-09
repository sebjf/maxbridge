using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages;
using Autodesk.Max;


namespace MaxExporter
{
    public partial class MaxUnityExporter
    {
        IEnumerable<MaterialInformation> GetNodeMaterials(IMtl material)
        {
            if (material == null)
            {
                yield break;
            }

            if (material.ClassID.EqualsClassID(597,0))
            {
                foreach (var m in GetNodeMaterials(material.GetSubMtl(2)))      //when there is a shell material always return the baked one.
                {
                    yield return m;
                }
                yield break;
            }

            if (material.ClassID.EqualsClassID(512, 0))
            {
                for (int i = 0; i < material.NumSubMtls; i++)                   //process all submaterials in a submtl object
                {
                    foreach (var m in GetNodeMaterials(material.GetSubMtl(i))) 
                    {
                        yield return m;
                    }
                }
                yield break;
            }

            yield return GetMaterialProperties(material);
        }

        MaterialInformation GetMaterialProperties(IMtl material)
        {

            MaterialInformation m = new MaterialInformation();
            m.Class = material.ClassName;

            foreach (var p in EnumerateProperties(material).Where(p => p.IsMapType))
            {
                m.MaterialMaps.Add(p.Name, p.GetValueAsMap());
            }

            foreach (var p in EnumerateProperties(material).Where(p => p.IsValueType))
            {
                m.MaterialProperties.Add(p.Name, p.GetValueAsString());
            }

            return m;
        }

    }
}
