using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Max;
using Messages;
using System.Runtime.InteropServices;

namespace MaxExporter
{


    public partial class MaxUnityExporter
    {
        protected IEnumerable<GeometryUpdate> CreateUpdates()
        {
            foreach (var n in TriGeometryNodes)
            {
                yield return CreateGeometryUpdate(n);
            }
        }

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        protected unsafe GeometryUpdate CreateGeometryUpdate(IINode node)
        {
            GeometryUpdate update = new GeometryUpdate();

            if (!(node.ObjectRef is ITriObject))
            {
                throw new Exception("Can only process editable meshes.");
            }

            update.Name = node.Name;
            update.Parent = node.ParentNode.Name;

            update.Transform = GetTransform(node);

            ITriObject maxGeometry = (node.ObjectRef as ITriObject);
            IMesh maxMesh = maxGeometry.Mesh;

            update.Vertices = new Point3[maxMesh.NumVerts];

            fixed (Point3* vertexData = update.Vertices)
            {
                CopyMemory((IntPtr)vertexData, maxMesh.GetVertPtr(0).Handle,(uint)(sizeof(Point3) * maxMesh.NumVerts));
            }

            for (int i = 0; i < maxMesh.NumMaps; i++ )
            {
                int count = maxMesh.GetNumMapVerts(i);
                if (count > 0)
                {
                    Point3[] data = new Point3[count];
                    fixed (Point3* dataptr = data)
                    {
                        CopyMemory((IntPtr)dataptr, maxMesh.MapVerts(i)[0].Handle, (uint)(sizeof(Point3) * count));
                    }
                    update.TextureCoordinates.Add(data);
                }
            }

            update.Faces = new Face[maxMesh.NumFaces];

            fixed (Face* faceData = update.Faces)
            {
                CopyMemory((IntPtr)faceData, maxMesh.Faces[0].Handle, (uint)(sizeof(Face) * maxMesh.NumFaces));
            }

            update.Materials.AddRange(GetNodeMaterials(node.Mtl));

            return update;
        }

        protected Point3 Point3(IPoint3 v)
        {
            Point3 p;
            p.x = v.X;
            p.y = v.Y;
            p.z = v.Z;
            return p;
        }

        protected Quat Quat(IQuat v)
        {
            Quat q;
            q.x = v.X;
            q.y = v.Y;
            q.z = v.Z;
            q.w = v.W;
            return q;
        }

        protected TransformComponents GetTransform(IINode node)
        {
            TransformComponents t = new TransformComponents();

            //http://forums.cgsociety.org/archive/index.php/t-1033384.html
            //http://docs.autodesk.com/3DSMAX/16/ENU/3ds-Max-SDK-Programmer-Guide/index.html?url=cpp_ref/struct_affine_parts.html,topicNumber=cpp_ref_struct_affine_parts_htmlcb6d8cc6-928f-4650-baf7-b7efa4bb4eb6
            //http://forum.unity3d.com/threads/121966-How-to-assign-Matrix4x4-to-Transform

            IAffineParts affine = globalInterface.AffineParts.Create();
            globalInterface.DecompAffine( node.GetNodeTM(0, globalInterface.Interval.Create()), affine);

            t.Translate = Point3(affine.T);
            t.Rotation = Quat(affine.Q);
            t.ScaleRotation = Quat(affine.U);
            t.Scale = Point3(affine.K);

            return t;

        }

    }
}
