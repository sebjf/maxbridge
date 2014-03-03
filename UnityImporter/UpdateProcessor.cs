﻿using System;
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

            UpdateMesh(meshFilter.sharedMesh, Update);

            meshRenderer.materials = new Material[meshFilter.sharedMesh.subMeshCount];

        }

        protected void SetTransform(GameObject node, TransformComponents components)
        {
            node.transform.localPosition = Vector3.zero;
            node.transform.localRotation = Quaternion.identity;
            node.transform.localScale = Vector3.one;

            Quaternion scaleRotation = new Quaternion(components.ScaleRotation.x, components.ScaleRotation.y, components.ScaleRotation.z, components.ScaleRotation.w);
            
            if (scaleRotation != Quaternion.identity)
            {
                Debug.Log("Nonidentity scale rotation from Max. The scale may be incorrectly applied. Consider baking in Max.");
            }

            Quaternion Rotation = new Quaternion(components.Rotation.x, components.Rotation.y, components.Rotation.z, components.Rotation.w);
            Vector3 angles = Quaternion.ToEulerAngles(Rotation);


            Vector3 axis = new Vector3(components.Rotation.x, components.Rotation.z, components.Rotation.y);
            axis.Normalize();

      //      axis = Quaternion.AngleAxis(90, Vector3.left) * axis;
            
            //Quaternion Rotation = Quaternion.AngleAxis(components.Rotation.w * Mathf.Rad2Deg, axis);// *Quaternion.AngleAxis(90, Vector3.left);

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

        unsafe protected void UpdateMesh(Mesh mesh, GeometryUpdate Update)
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
                short materialid = (short)((Update.Faces[i].flags & 0xFFFF0000) >> 16);

                if (!faceLists.ContainsKey(materialid))
                {
                    faceLists.Add(materialid, new List<int>());
                }

                faceLists[materialid].Add((int)Update.Faces[i].v.v1);
                faceLists[materialid].Add((int)Update.Faces[i].v.v2);
                faceLists[materialid].Add((int)Update.Faces[i].v.v3);
            }

            mesh.subMeshCount = faceLists.Values.Count;

            for (int i = 0; i < faceLists.Values.Count; i++)
            {
                mesh.SetTriangles(faceLists.Values.ElementAt(i).ToArray(), i);
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
