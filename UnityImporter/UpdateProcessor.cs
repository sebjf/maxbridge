using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using Messages;
using System.Runtime.InteropServices;

namespace MaxUnityBridge
{
    public interface IUpdateProcessor
    {
        void ProcessUpdate(GeometryUpdate Update);
    }

    public partial class UpdateProcessor : MonoBehaviour, IUpdateProcessor
    {
        //Unity is a left handed Y up coordinate system, whereas Max is a right handed Z up coordinate system.
        //To convert points between the two we must swap Z & Y, then mirror (invert) along the X and Z (unity) axis
        protected bool SwapZY = true;

        public void ProcessUpdate(GeometryUpdate Update)
        {
            GameObject node = GetCreateObject(Update);

         //   SetTransform(node, Update.Transform);


            MeshFilter meshFilter = node.GetComponent<MeshFilter>();
            if(meshFilter == null)
            {
                meshFilter = node.AddComponent<MeshFilter>();
            }
            

            MeshRenderer meshRenderer = node.GetComponent<MeshRenderer>();
            if(meshRenderer == null)
            {
                meshRenderer = node.AddComponent<MeshRenderer>();
            }

            if (meshFilter.sharedMesh == null)
            {
                meshFilter.mesh = new Mesh();
            }


            ProcessedMesh meshsource = PreprocessMesh(Update);

            SetMesh(meshFilter.sharedMesh, meshsource);


            SetMaterials(meshRenderer, meshsource, Update);
       
        }

        protected void SetTransform(GameObject node, TRS components)
        {
            float r_multiplier = 1.0f;
            if (SwapZY)
            {
                r_multiplier *= -1.0f;
            }
#pragma warning disable 0618 //Max passes angles in radians, so this *is* the one we want 
            node.transform.localRotation = Quaternion.EulerAngles(ToVector3(components.EulerRotation));
#pragma warning restore 0618
            node.transform.localScale = ToVector3S(components.Scale);
            node.transform.localPosition = ToVector3(components.Translate);

        }

        protected GameObject GetCreateObject(GeometryUpdate update)
        {
            GameObject node = GameObject.Find(update.Name);

            if (node == null)
            {
                node = new GameObject(update.Name);
            }

            if (update.Parent != null && update.Parent != "Scene Root")
            {
                Debug.Log("Hierarchical processing not implemented yet.");
            }

            return node;
        }

        protected void SetMaterials(MeshRenderer renderer, ProcessedMesh meshsrc, GeometryUpdate update)
        {
            Dictionary<int, MaterialInformation> materialinfo = new Dictionary<int, MaterialInformation>();
            for (int i = 0; i < update.Materials.Count; i++)
            {
                materialinfo.Add(i, update.Materials[i]);
            }

            Material[] materials = new Material[meshsrc.faces.Count];

            for (int i = 0; i < meshsrc.faces.Count; i++)
            {
                Material m = null;
                if (update.Materials.Count > 0)
                {
                    m = ImportMaterial(update.Materials[0]);
                }

                if (update.Materials.Count > meshsrc.faces[i].MaterialId)
                {
                    materials[i] = ImportMaterial(update.Materials[meshsrc.faces[i].MaterialId]);
                }
                else
                {
                    materials[i] = m;
                }
            }

            renderer.materials = materials;
        }


        protected void SetMesh(Mesh mesh, ProcessedMesh meshsrc)
        {
            mesh.vertices = meshsrc.components.vertices;
            mesh.normals = meshsrc.components.normals;
            mesh.uv1 = meshsrc.components.uvs;
            mesh.uv2 = meshsrc.components.uvs2;

            mesh.subMeshCount = meshsrc.faces.Count;

            for (int i = 0; i < meshsrc.faces.Count; i++)
            {
                mesh.SetTriangles(meshsrc.faces[i].FaceIndices, i);
            }

            mesh.RecalculateBounds();
        }

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

        public Vector3 ToVector3(Point3 p)
        {
            if (SwapZY)
            {
                return new Vector3() { x = -p.x, y = p.z, z = p.y };
            }
            else
            {
                return new Vector3() { x = p.x, y = p.y, z = p.z };
            }
        }

        public Vector3 ToVector3S(Point3 p)
        {
            return new Vector3() { x = p.x, y = p.y, z = p.z };
        }

        public Vector2 ToVector2(Point3 p)
        {
            return new Vector3() { x = p.x, y = p.y };
        }
    }
}
