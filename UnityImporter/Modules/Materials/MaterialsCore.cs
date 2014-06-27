using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Messaging;

namespace MaxUnityBridge
{
    public class MaterialsCore
    {


        protected Material ImportMaterial(MaterialInformation m)
        {
            if (m.m_className == "Standard")
            {
                return ImportMaterial_Standard(m);
            }

            if (m.m_className == "Arch & Design")
            {
                return (new MentalRayArchDesignMaterial(new MentalRayArchDesignMaterialAccessor(m))).CreateUnityMaterial();
            }

            throw new Exception("The material type: " + m.m_className + " is not supported.");
        }

        protected Material ImportMaterial_Standard(MaterialInformation m)
        {
            throw new Exception("Do not support Standard materials yet.");
        }

        protected class MentalRayArchDesignMaterialAccessor
        {
            public MentalRayArchDesignMaterialAccessor(MaterialInformation m)
            {
                this.source = m;
            }

            protected MaterialInformation source;

            public fRGBA diff_color { get { return (fRGBA)source.MaterialProperties["diff_color"]; } }
            public float diff_weight { get { return (float)source.MaterialProperties["diff_weight"]; } }
            public fRGBA refl_color { get { return (fRGBA)source.MaterialProperties["refl_color"]; } }
            public float refl_gloss { get { return (float)source.MaterialProperties["refl_gloss"]; } }
            public float refl_weight { get { return (float)source.MaterialProperties["refl_weight"]; } }
            public MapReference bump_map { get { return source.MaterialProperties["bump_map"] as MapReference; } }
            public MapReference diff_color_map { get { return source.MaterialProperties["diff_color_map"] as MapReference; } }

        }

        protected class MentalRayArchDesignMaterial
        {
            public MentalRayArchDesignMaterial(MentalRayArchDesignMaterialAccessor mat)
            {
                DiffuseColour = ToColor(mat.diff_color) * mat.diff_weight;

                Glossiness = mat.refl_gloss;
                ReflectionColour = ToColor(mat.refl_color) * mat.refl_weight;

            }

            public static Color ToColor(fRGBA c)
            {
                return new Color(c.r, c.g, c.b, c.a);
            }


            public Color DiffuseColour;
            public Texture2D DiffuseMap;
            public float Glossiness;

            public Color ReflectionColour;
            public Texture2D NormalMap;

            public Material CreateUnityMaterial()
            {
                Material material = new Material(Shader.Find("MentalRayArchDesign"));

                material.SetColor("_DiffuseColour", DiffuseColour);

                if (DiffuseMap != null)
                {
                    material.SetTexture("_DiffuseMap", DiffuseMap);
                }

                material.SetFloat("_Glossiness", Glossiness);

                if (NormalMap != null)
                {
                    material.SetTexture("_NormalMap", NormalMap);
                }

                return material;
            }
        }


    }
}
