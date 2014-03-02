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
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        unsafe GeometryUpdate createGeometryUpdate()
        {
            GeometryUpdate update = new GeometryUpdate();

            IINode node = TriGeometryNodes.FirstOrDefault();

            if (node != null)
            {
                ITriObject maxGeometry = (node.ObjectRef as ITriObject);
                IMesh maxMesh = maxGeometry.Mesh;

                update.Vertices = new Point3[maxMesh.NumVerts];

                fixed (Point3* vertexData = update.Vertices)
                {
                    CopyMemory((IntPtr)vertexData, maxMesh.GetVertPtr(0).Handle,(uint)(sizeof(Point3) * maxMesh.NumVerts));
                }

                update.Faces = new Face[maxMesh.NumFaces];

                fixed (Face* faceData = update.Faces)
                {
                    CopyMemory((IntPtr)faceData, maxMesh.Faces[0].Handle, (uint)(sizeof(Face) * maxMesh.NumFaces));
                }
                
            }

            return update;
        }

        void buildGeometryUpdate()
        {

        }

    }
}
