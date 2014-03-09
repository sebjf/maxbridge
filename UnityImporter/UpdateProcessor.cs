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

    public class UpdateProcessor : MonoBehaviour, IUpdateProcessor
    {
        public void ProcessUpdate(GeometryUpdate Update)
        {
            GameObject node = GetCreateObject(Update);

            SetTransform(node, Update.Transform);

            //http://docs.unity3d.com/Documentation/ScriptReference/MeshFilter.html
            //https://docs.unity3d.com/Documentation/ScriptReference/MeshFilter.html

            MeshFilter meshFilter = node.GetComponent<MeshFilter>();

            if(meshFilter == null)
            {
                meshFilter = node.AddComponent<MeshFilter>();
            }
            
            //http://docs.unity3d.com/Documentation/Components/class-MeshRenderer.html
            //https://docs.unity3d.com/Documentation/ScriptReference/MeshRenderer.html

            MeshRenderer meshRenderer = node.GetComponent<MeshRenderer>();

            if(meshRenderer == null)
            {
                meshRenderer = node.AddComponent<MeshRenderer>();
            }

            if (meshFilter.sharedMesh == null)
            {
                meshFilter.mesh = new Mesh();
            }

            var materialsMap = UpdateMesh(meshFilter.sharedMesh, Update);

            Material[] materials = new Material[meshFilter.sharedMesh.subMeshCount];
            foreach (var p in materialsMap)
            {
                materials[p.Key] = ImportMaterial(Update.Materials[0]);
            }

            meshRenderer.materials = materials;
       
        }

        protected void SetTransform(GameObject node, TransformComponents components)
        {
            node.transform.localPosition = Vector3.zero;
            node.transform.localRotation = Quaternion.identity;
            node.transform.localScale = Vector3.one;

            Quaternion scaleRotation = new Quaternion(components.ScaleRotation.x, components.ScaleRotation.y, components.ScaleRotation.z, components.ScaleRotation.w);
            
            if (scaleRotation != Quaternion.identity)
            {
                Debug.Log("Nonidentity scale rotation from Max. Consider applying Reset XForm modifier and re-importing.");
            }

            Quaternion Rotation = new Quaternion(components.Rotation.x, components.Rotation.y, components.Rotation.z, components.Rotation.w);
            Vector3 angles = Quaternion.ToEulerAngles(Rotation);
            Rotation = Quaternion.EulerAngles(angles.x, angles.z, angles.y);
            
            Vector3 Scale = new Vector3(components.Scale.x, components.Scale.z, components.Scale.y);
            Vector3 Translation = new Vector3(components.Translate.x, components.Translate.z, components.Translate.y);

            node.transform.localRotation = Rotation;
            node.transform.localScale = Scale;
            node.transform.localPosition = Translation;

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

        unsafe protected Dictionary<int,int> UpdateMesh(Mesh mesh, GeometryUpdate Update)
        {

            //http://docs.unity3d.com/Documentation/ScriptReference/Mesh.html

            var vertices = new Vector3[Update.Vertices.Length];

            for (int i = 0; i < Update.Vertices.Length; i++)
            {
                //vertices[i] = new Vector3(Update.Vertices[i].x, Update.Vertices[i].y, Update.Vertices[i].z);
                vertices[i] = new Vector3(Update.Vertices[i].x, Update.Vertices[i].z, Update.Vertices[i].y);
            }

            mesh.vertices = vertices; //the array must be populated and then assigned to mesh (it probably copies it in its set accessor..)

            var uvs = new Vector2[Update.Vertices.Length];

            mesh.uv = uvs;


            Dictionary<short, List<int>> faceLists = new Dictionary<short, List<int>>();

            var triangles = new int[Update.Faces.Length * 3];

            for (int i = 0; i < Update.Faces.Length; i++ )
            {
                short materialid = (short)((Update.Faces[i].flags & 0xFFFF0000) >> 16); //in Max the high word of the flags member contains the material id.

                if (!faceLists.ContainsKey(materialid))
                {
                    faceLists.Add(materialid, new List<int>());
                }

                faceLists[materialid].Add((int)Update.Faces[i].v.v1);
                faceLists[materialid].Add((int)Update.Faces[i].v.v2);
                faceLists[materialid].Add((int)Update.Faces[i].v.v3);
            }

            Dictionary<int, int> materialsMap = new Dictionary<int, int>();

            mesh.subMeshCount = faceLists.Values.Count;

            for (int i = 0; i < faceLists.Values.Count; i++)
            {
                mesh.SetTriangles(faceLists.Values.ElementAt(i).ToArray(), i);

                materialsMap.Add(i, faceLists.Keys.ElementAt(i));
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return materialsMap;
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
    }
}
