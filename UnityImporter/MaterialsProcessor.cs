using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Messages;

namespace MaxUnityBridge
{
    public partial class UpdateProcessor
    {
        protected Material ImportMaterial(MaterialInformation m)
        {
            if (m.Class == "Standard")
            {
                return ImportMaterial_Standard(m);
            }

            if (m.Class == "Arch & Design")
            {
                return (new MentalRayArchDesignMaterial(new MentalRayArchDesignMaterialAccessor(m))).CreateUnityMaterial();
            }

            throw new Exception("The material type: " + m.Class + " is not supported.");
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
            public MapInformation bump_map { get { return source.MaterialProperties["bump_map"] as MapInformation; } }
            public MapInformation diff_color_map { get { return source.MaterialProperties["diff_color_map"] as MapInformation; } }

        }

        protected class MentalRayArchDesignMaterial
        {
            public MentalRayArchDesignMaterial(MentalRayArchDesignMaterialAccessor mat)
            {
                DiffuseColour = ToColor(mat.diff_color) * mat.diff_weight;
                DiffuseMap = ToTexture2D(mat.diff_color_map);
                Glossiness = mat.refl_gloss;
                ReflectionColour = ToColor(mat.refl_color) * mat.refl_weight;
                NormalMap = ToTexture2D(mat.bump_map);
            }

            public static Color ToColor(fRGBA c)
            {
                return new Color(c.r, c.g, c.b, c.a);
            }

            public static Texture2D ToTexture2D(MapInformation m)
            {
                if (m == null)
                    return null;

                if (m.Filename == null)
                    return null;

                return Resources.Load(m.Filename, typeof(Texture2D)) as Texture2D;
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
