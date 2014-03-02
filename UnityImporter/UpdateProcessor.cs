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
            GameObject node = GetCreateObject();

            MeshFilter meshFilter = node.AddComponent<MeshFilter>();
            node.AddComponent<MeshRenderer>();

            meshFilter.mesh = CreateMesh(Update);
        }

        protected GameObject GetCreateObject()
        {
            return new GameObject("New Object");
        }

        unsafe protected Mesh CreateMesh(GeometryUpdate Update)
        {
            Mesh mesh = new Mesh();

            //http://docs.unity3d.com/Documentation/ScriptReference/Mesh.html

            var vertices = new Vector3[Update.Vertices.Length];

            for (int i = 0; i < Update.Vertices.Length; i++)
            {
                vertices[i] = new Vector3(Update.Vertices[i].x, Update.Vertices[i].y, Update.Vertices[i].z);
            }

            mesh.vertices = vertices; //the array must be populated and then assigned to mesh (it probably copies it in its set accessor..)

            var uvs = new Vector2[Update.Vertices.Length];

            mesh.uv = uvs;

            var triangles = new int[Update.Faces.Length * 3];

            for (int i = 0; i < Update.Faces.Length; i++ )
            {
                triangles[(i * 3) + 0] = (int)Update.Faces[i].v.v1;
                triangles[(i * 3) + 1] = (int)Update.Faces[i].v.v2;
                triangles[(i * 3) + 2] = (int)Update.Faces[i].v.v3;
            }

            mesh.triangles = triangles;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
