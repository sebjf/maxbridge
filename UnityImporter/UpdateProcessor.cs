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

            if (Update.TextureCoordinates.Count > 0)
            {
                if(Update.TextureCoordinates[0].Length != Update.Vertices.Length){
                    throw new Exception("Mesh must have the same number of texcoordinates as vertices.");
                }

                var uvs = new Vector2[Update.TextureCoordinates[0].Length];
                for(int i = 0; i < Update.TextureCoordinates[0].Length; i++)
                {
                    uvs[i] = new Vector2(Update.TextureCoordinates[0][i].x, Update.TextureCoordinates[0][i].y);
                }

                mesh.uv = uvs;
                mesh.uv2 = uvs;
            }
            
            /* If there are two UV channels, copy the second one into the second uv set. If there are more, ignore them. If there are less, uv2 will be populated with duplicates from above anyway */

            if (Update.TextureCoordinates.Count > 1)
            {
                if (Update.TextureCoordinates[1].Length != Update.Vertices.Length)
                {
                    throw new Exception("Mesh must have the same number of texcoordinates as vertices.");
                }

                var uvs = new Vector2[Update.TextureCoordinates[1].Length];
                for (int i = 0; i < Update.TextureCoordinates[1].Length; i++)
                {
                    uvs[i] = new Vector2(Update.TextureCoordinates[1][i].x, Update.TextureCoordinates[1][i].y);
                }

                mesh.uv2 = uvs;
            }

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
            TangentSolver.Solve(mesh);

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

    /* 
     * Posted by noontz at
     * http://forum.unity3d.com/threads/38984-How-to-Calculate-Mesh-Tangents/ 
     */

    public class TangentSolver 
    {
        /*
        Derived from
        Lengyel, Eric. "Computing Tangent Space Basis Vectors for an Arbitrary Mesh". Terathon Software 3D Graphics Library, 2001.
        [url]http://www.terathon.com/code/tangent.html[/url]
        */

        public static void Solve(Mesh theMesh) 
        {
            int vertexCount = theMesh.vertexCount;
            Vector3[] vertices = theMesh.vertices;
            Vector3[] normals = theMesh.normals;
            Vector2[] texcoords = theMesh.uv;
            int[] triangles = theMesh.triangles;
            int triangleCount = triangles.Length/3;

            Vector4[] tangents = new Vector4[vertexCount];
            Vector3[] tan1 = new Vector3[vertexCount];
            Vector3[] tan2 = new Vector3[vertexCount];

            int tri = 0;

            for (int i = 0; i < (triangleCount); i++) {

                int i1 = triangles[tri];
                int i2 = triangles[tri+1];
                int i3 = triangles[tri+2];

                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];

                Vector2 w1 = texcoords[i1];
                Vector2 w2 = texcoords[i2];
                Vector2 w3 = texcoords[i3];

                float x1 = v2.x - v1.x;
                float x2 = v3.x - v1.x;
                float y1 = v2.y - v1.y;
                float y2 = v3.y - v1.y;
                float z1 = v2.z - v1.z;
                float z2 = v3.z - v1.z;

                float s1 = w2.x - w1.x;
                float s2 = w3.x - w1.x;
                float t1 = w2.y - w1.y;
                float t2 = w3.y - w1.y;

                float r = 1.0f / (s1 * t2 - s2 * t1);
                Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;

                tri += 3;

            }

            for (int i = 0; i < (vertexCount); i++) 
            {
                Vector3 n = normals[i];
                Vector3 t = tan1[i];

                // Gram-Schmidt orthogonalize
                Vector3.OrthoNormalize(ref n, ref t);

                tangents[i].x  = t.x;
                tangents[i].y  = t.y;
                tangents[i].z  = t.z;

                // Calculate handedness
                tangents[i].w = ( Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f ) ? -1.0f : 1.0f;

            }       
            theMesh.tangents = tangents;
        }
    }
}
