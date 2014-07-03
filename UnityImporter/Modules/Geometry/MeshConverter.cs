using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Messaging;

namespace MaxUnityBridge.Geometry
{
    /* This is the mesh reformatted to mimic the unity mesh structure */
    internal class UnityStyleMesh
    {
        public VertexComponents components;
        public List<FaceIndicesGroup> face_groups;
    }

    internal class FaceIndicesGroup
    {
        public int MaterialId;
        public int[] FaceIndices;
    }

    internal class VertexComponents
    {
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector2[] uvs;
        public Vector2[] uvs2;

        public VertexComponents(int count)
        {
            vertices = new Vector3[count];
            normals = new Vector3[count];
            uvs = new Vector2[count];
            uvs2 = new Vector2[count];
        }
    }



    internal partial class GeometryCore
    {
        /* One third of a face */
        protected struct Triplet
        {
            public int v;

            public int n;

            public int t;
            public int y;
        }

        /// <summary>
        /// Processes update with heterogenous vertex component arrays into a single set of homogenous arrays based on face data, and calculates tangents
        /// </summary>
        /// <param name="Update"></param>
        /// <returns></returns>
        protected UnityStyleMesh ConvertMeshToUnityMesh(GeometryNode Update)
        {
            var faces = Update.Faces;
            int facecount = faces.Length;

            /* First, split the faces into face groups (submeshes) based on material ids. we will do that now, because soon we will deal with faces as index triplets
             * and we do not want to have to store materialids along with each face. */

            var faceGroups = CreateFaceGroupsByMaterialId(faces);


            /* In Max, and the portable mesh, each triangle is made of a number of faces superimposed on eachother. Those faces contain indices into different vertex arrays.
             * The vertex arrays contain positions, texture coords, normals, etc. This function flattens these, so each face is made up of 3 sets of 4 indices. One set for
             * each corner, and the sets containing the indices into the various vertex arrays referenced by the original 'sub'-faces. */

            var faceTripletGroups = CreateFaceTripletGroups(faceGroups, 
                Update.Faces.Cast<ITripleIndex>().ToArray(), 
                Update.NormalFaces.Cast<ITripleIndex>().ToArray(), 
                Update.Faces.Cast<ITripleIndex>().ToArray(), 
                Update.Faces.Cast<ITripleIndex>().ToArray());


            /* For each face triple (set of 4 indices), dereference it so they all become indices into a single master list of triplets. This master list can then
             * be used to create a vertex array. */

            var triplets = new FaceTriplets();

            var faceindexgroups = DereferenceFaceTripletGroups(faceTripletGroups, triplets).ToList();


            /* Build the vertex component arrays based on the master triplet array created above */

            var vertexcomponents = BuildVertexComponents(triplets, Update.Vertices, Update.Normals, Update.Vertices, Update.Vertices);


            return new UnityStyleMesh { components = vertexcomponents, face_groups = faceindexgroups };
        }

        protected class FaceGroup
        {
            public int[] FaceIndices;
            public int MaterialId;
        }

        protected IEnumerable<FaceGroup> CreateFaceGroupsByMaterialId(Face[] faces)
        {
            Dictionary<short, List<int>> faceLists = new Dictionary<short, List<int>>();

            for (int i = 0; i < faces.Length; i++)
            {
                short materialid = (short)((faces[i].flags & 0xFFFF0000) >> 16); //in Max the high word of the flags member contains the material id.

                if (!faceLists.ContainsKey(materialid))
                {
                    faceLists.Add(materialid, new List<int>());
                }

                faceLists[materialid].Add(i);
            }

            foreach (var p in faceLists)
            {
                yield return new FaceGroup { MaterialId = p.Key, FaceIndices = p.Value.ToArray() };
            }
        }



        protected class FaceTripletGroup
        {
            public Triplet[] Triplets;
            public int MaterialId;
        }

        protected IEnumerable<FaceTripletGroup> CreateFaceTripletGroups(IEnumerable<FaceGroup> facegroups, ITripleIndex[] fp, ITripleIndex[] fn, ITripleIndex[] tn, ITripleIndex[] yn)
        {
            foreach (var fg in facegroups)
            {
                yield return CreateFaceTripletGroup(fg, fp, fn, tn, yn);
            }
        }

        protected FaceTripletGroup CreateFaceTripletGroup(FaceGroup faces, ITripleIndex[] fp, ITripleIndex[] fn, ITripleIndex[] tn, ITripleIndex[] yn)
        {
            List<Triplet> triplets = new List<Triplet>();

            if (ChangeCoordinateSystem)
            {

                foreach (var i in faces.FaceIndices)
                {   
                    triplets.Add(new Triplet { v = fp[i].i3, n = fn[i].i3, t = tn[i].i3, y = yn[i].i3 });
                    triplets.Add(new Triplet { v = fp[i].i2, n = fn[i].i2, t = tn[i].i2, y = yn[i].i2 });
                    triplets.Add(new Triplet { v = fp[i].i1, n = fn[i].i1, t = tn[i].i1, y = yn[i].i1 });
                }
            }
            else
            {
                /* If the coordinate system has changed, swap the winding order */
                foreach (var i in faces.FaceIndices)
                {
                    triplets.Add(new Triplet { v = fp[i].i1, n = fn[i].i1, t = tn[i].i1, y = yn[i].i1 });
                    triplets.Add(new Triplet { v = fp[i].i2, n = fn[i].i2, t = tn[i].i2, y = yn[i].i2 });
                    triplets.Add(new Triplet { v = fp[i].i3, n = fn[i].i3, t = tn[i].i3, y = yn[i].i3 });    
                }
            }

            return new FaceTripletGroup { MaterialId = faces.MaterialId, Triplets = triplets.ToArray() };
        }



        protected IEnumerable<FaceIndicesGroup> DereferenceFaceTripletGroups(IEnumerable<FaceTripletGroup> facetripletgroups, FaceTriplets triplets)
        {
            foreach (var fg in facetripletgroups)
            {
                yield return DereferenceFaceTriplets(fg, triplets);
            }
        }

        protected FaceIndicesGroup DereferenceFaceTriplets(FaceTripletGroup faces, FaceTriplets triplets)
        {
            List<int> indices = new List<int>();

            foreach (var f in faces.Triplets)
            {
                indices.Add(triplets.GetTripletIndex(f));
            }

            return new FaceIndicesGroup { MaterialId = faces.MaterialId, FaceIndices = indices.ToArray() };
        }

        protected class FaceTriplets
        {
            public    List<Triplet>            uniqueTriplets          = new List<Triplet>();
            protected Dictionary<Triplet, int> uniqueTripletIndices    = new Dictionary<Triplet, int>();

            public int GetTripletIndex(Triplet ft)
            {
                if (!uniqueTripletIndices.ContainsKey(ft))
                {
                    uniqueTripletIndices.Add(ft, uniqueTriplets.Count);
                    uniqueTriplets.Add(ft);
                }

                return uniqueTripletIndices[ft];
            }
        }



        protected VertexComponents BuildVertexComponents(FaceTriplets triplets, Point3[] positions, Point3[] normals, Point3[] tex1, Point3[] tex2 )
        {
            int vertexcount = triplets.uniqueTriplets.Count;

            var vc = new VertexComponents(vertexcount);

            for (int i = 0; i < vertexcount; i++)
            {
                Triplet triplet = triplets.uniqueTriplets[i];

                vc.vertices[i] = ToVector3(positions[triplet.v]);
                vc.normals[i] = ToVector3(normals[triplet.n]);
                vc.uvs[i] = ToVector2(tex1[triplet.t]);
                vc.uvs2[i] = ToVector2(tex2[triplet.y]);
            }

            return vc;

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
            int triangleCount = triangles.Length / 3;

            Vector4[] tangents = new Vector4[vertexCount];
            Vector3[] tan1 = new Vector3[vertexCount];
            Vector3[] tan2 = new Vector3[vertexCount];

            int tri = 0;

            for (int i = 0; i < (triangleCount); i++)
            {

                int i1 = triangles[tri];
                int i2 = triangles[tri + 1];
                int i3 = triangles[tri + 2];

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

                tangents[i].x = t.x;
                tangents[i].y = t.y;
                tangents[i].z = t.z;

                // Calculate handedness
                tangents[i].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f) ? -1.0f : 1.0f;

            }
            theMesh.tangents = tangents;
        }
    }
}
