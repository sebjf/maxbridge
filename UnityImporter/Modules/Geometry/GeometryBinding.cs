using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messaging;
using UnityEditor;
using UnityEngine;

namespace MaxUnityBridge
{
    internal partial class GeometryBinding
    {
        //Unity is a left handed Y up coordinate system, whereas Max is a right handed Z up coordinate system.
        //To convert points between the two we must swap Z & Y, then mirror (invert) along the X and Z (unity) axis.
        //(Remember to swap the winding order as well)
        protected bool ChangeCoordinateSystem = true;

        public void ProcessMessage(MessageGeometryUpdate message)
        {
            foreach (var g in message.Geometries)
            {
                ProcessUpdate(g);
            }
        }

        protected void ProcessUpdate(GeometryNode Update)
        {
            GameObject node = GetCreateObject(Update);

            SetTransform(node, Update.Transform);


            MeshFilter meshFilter = node.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = node.AddComponent<MeshFilter>();
            }


            MeshRenderer meshRenderer = node.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = node.AddComponent<MeshRenderer>();
            }

            if (meshFilter.sharedMesh == null)
            {
                meshFilter.mesh = new Mesh();
            }


            UnityStyleMesh meshsource = ConvertMeshToUnityStyle(Update);
            SetMeshContent(meshFilter.sharedMesh, meshsource);


            SetMaterials(meshRenderer, meshsource, Update);

        }

        protected void SetTransform(GameObject node, TRS components)
        {
#pragma warning disable 0618 //Max passes angles in radians, so this *is* the one we want
            node.transform.localRotation = Quaternion.EulerAngles(-ToVector3(components.EulerRotation));
#pragma warning restore 0618
            node.transform.localScale = ToVector3S(components.Scale);
            node.transform.localPosition = ToVector3(components.Translate);

        }

        protected GameObject GetCreateObject(GeometryNode update)
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

        protected void SetMaterials(MeshRenderer renderer, UnityStyleMesh meshsrc, GeometryNode update)
        {
            string material_name = update.MaterialName;

            if (material_name == null)
            {
                material_name = "no name";
            }

            Material[] materials = new Material[meshsrc.face_groups.Count];

            for (int i = 0; i < meshsrc.face_groups.Count; i++)
            {
                Material m = new Material(Shader.Find("Diffuse"));
                m.name = material_name + ":" + meshsrc.face_groups[i].MaterialId;
                materials[i] = m;
            }

            renderer.materials = materials;
        }


        protected void SetMeshContent(Mesh mesh, UnityStyleMesh meshsrc)
        {
            mesh.vertices = meshsrc.components.vertices;
            mesh.normals = meshsrc.components.normals;
            mesh.uv1 = meshsrc.components.uvs;
            mesh.uv2 = meshsrc.components.uvs2;

            mesh.subMeshCount = meshsrc.face_groups.Count;

            for (int i = 0; i < meshsrc.face_groups.Count; i++)
            {
                mesh.SetTriangles(meshsrc.face_groups[i].FaceIndices, i);
            }

            mesh.RecalculateBounds();
        }


        protected Vector3 ToVector3(Point3 p)
        {
            if (ChangeCoordinateSystem)
            {
                return new Vector3() { x = -p.x, y = p.z, z = -p.y };
            }
            else
            {
                return new Vector3() { x = p.x, y = p.y, z = p.z };
            }
        }

        protected Vector3 ToVector3S(Point3 p)
        {
            if (ChangeCoordinateSystem)
            {
                return new Vector3() { x = p.x, y = p.z, z = p.y };
            }
            else
            {
                return new Vector3() { x = p.x, y = p.y, z = p.z };
            }

        }

        protected Vector2 ToVector2(Point3 p)
        {
            return new Vector3() { x = p.x, y = p.y };
        }
    }
}
