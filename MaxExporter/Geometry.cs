using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Max;
using Messages;
using System.Runtime.InteropServices;

namespace MaxExporter
{
    public static class Extensions
    {
        public static Point3 ToPoint3(this IPoint3 v)
        {
            return new Point3() { x = v.X, y = v.Y, z = v.Z };
        }
    }

    public partial class MaxUnityExporter
    {
        protected const uint  TRIOBJ_CLASS_ID 	 	= 0x0009;	    //!< TriObject class ID
        protected const uint  EDITTRIOBJ_CLASS_ID	= 0xe44f10b3;	    //!< Base triangle mesh (Edit class ID
        protected const uint  POLYOBJ_CLASS_ID		= 0x5d21369a;	//!< Polygon mesh (PolyObject) class ID
        protected const uint  PATCHOBJ_CLASS_ID  	= 0x1030;      //!< Patch object
        protected const uint  NURBSOBJ_CLASS_ID		= 0x4135;      //!< Nurbs object 

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);



        protected IEnumerable<GeometryUpdate> CreateUpdates()
        {
            foreach (var n in SceneNodes)
            {
                var u = CreateGeometryUpdate(n);
                if(u != null){
                    yield return u;
                }
            }
        }

        protected unsafe GeometryUpdate CreateGeometryUpdate(IINode node)
        {
            IObject obj = node.EvalWorldState(0, false).Obj;
            if (obj.CanConvertToType(gi.Class_ID.Create(TRIOBJ_CLASS_ID, 0)) > 0)
            {
                ITriObject triobj = obj.ConvertToType(0, gi.Class_ID.Create(TRIOBJ_CLASS_ID, 0)) as ITriObject;

                GeometryUpdate u = CreateGeometryUpdate(node, triobj);

                if (obj.Handle != triobj.Handle)
                {
                    triobj.DeleteMe();
                }

                return u;
            }

            return null;
        }

        protected unsafe GeometryUpdate CreateGeometryUpdate(IINode node, ITriObject maxGeometry)
        {
            GeometryUpdate update = new GeometryUpdate();


            update.Name = node.Name;
            update.Parent = node.ParentNode.Name;

            update.Transform = GetTransform(node);

            IMesh mesh = maxGeometry.Mesh;



            update.Vertices = new Point3[mesh.NumVerts];

            fixed (Point3* vertexData = update.Vertices)
            {
                CopyMemory((IntPtr)vertexData, mesh.GetVertPtr(0).Handle,(uint)(sizeof(Point3) * mesh.NumVerts));
            }




            foreach (var map in mesh.Maps)
            {
                if (map.IsUsed)
                {
                    MapChannel channel = new MapChannel();


                    channel.Coordinates = new Point3[map.Vnum];
                    fixed (Point3* dataptr = channel.Coordinates)
                    {
                        CopyMemory((IntPtr)dataptr, map.Tv.Handle, (uint)(sizeof(Point3) * map.Vnum));
                    }

                    channel.Faces = new TVFace[map.Fnum];
                    fixed (TVFace* dataptr = channel.Faces)
                    {
                        CopyMemory((IntPtr)dataptr, map.Tf.Handle, (uint)(sizeof(TVFace) * map.Fnum));
                    }

                    update.Channels.Add(channel);
                }
            }


            //for (int i = 0; i < mesh.NumMaps; i++ )
            //{
            //    int count = mesh.GetNumMapVerts(i);
            //    if (count > 0)
            //    {
            //        MapChannel channel = new MapChannel();
            //        channel.Id = i;

            //        channel.Coordinates = new Point3[count];
            //        fixed (Point3* dataptr = channel.Coordinates)
            //        {
            //            CopyMemory((IntPtr)dataptr, mesh.MapVerts(i)[0].Handle, (uint)(sizeof(Point3) * count));
            //        }

            //        update.Channels.Add(channel);
            //    }
            //}

            
            /* The MeshNormalSpec class is intended to store user specified normals. We can use it however to have max calculate the normals in the typical way and provide access to them in an easy way. */

            IMeshNormalSpec normalspec = mesh.SpecifiedNormals;
            bool normalsAlreadySpecified = (normalspec.NormalArray != null);
            if (!normalsAlreadySpecified)
            {
                mesh.SpecifyNormals();
            }

            normalspec.CheckNormals();

            update.Normals = new Point3[normalspec.NumNormals];

            fixed (Point3* normalData = update.Normals)
            {
                CopyMemory((IntPtr)normalData, normalspec.NormalArray.Handle, (uint)(sizeof(Point3) * normalspec.NumNormals));
            }

            int numnormalfaces = normalspec.NumFaces;

            for (int i = 0; i < numnormalfaces; i++)
            {
                IMeshNormalFace f = normalspec.Face(i);

                
            }

            if (!normalsAlreadySpecified)
            {
                mesh.ClearSpecifiedNormals();
            }




            update.Faces = new Face[mesh.NumFaces];

            fixed (Face* faceData = update.Faces)
            {
                CopyMemory((IntPtr)faceData, mesh.Faces[0].Handle, (uint)(sizeof(Face) * mesh.NumFaces));
            }

            //update.TextureFaces = new TVFace[mesh.NumFaces];

            //fixed (TVFace* faceData = update.TextureFaces)
            //{
            //    CopyMemory((IntPtr)faceData, mesh.TvFace[0].Handle, (uint)(sizeof(TVFace) * mesh.NumFaces));
            //}

            update.Materials.AddRange(GetNodeMaterials(node.Mtl));




            return update;
        }

        unsafe protected TRS GetTransform(IINode node)
        {
            TRS t = new TRS();

            IMatrix3 tm = node.GetObjectTM(0, gi.Interval.Create());


            IAffineParts affine = gi.AffineParts.Create();
            gi.DecompAffine( tm, affine);


            t.Translate = affine.T.ToPoint3();
            t.Scale = affine.K.ToPoint3();


            float roll = 0;
            float pitch = 0;
            float yaw = 0;

            GCHandle hRoll = GCHandle.Alloc(roll, GCHandleType.Pinned);
            GCHandle hPitch = GCHandle.Alloc(pitch, GCHandleType.Pinned);
            GCHandle hYaw = GCHandle.Alloc(yaw, GCHandleType.Pinned);

            tm.GetYawPitchRoll(hYaw.AddrOfPinnedObject(), hPitch.AddrOfPinnedObject(), hRoll.AddrOfPinnedObject());

            t.EulerRotation.x = (float)hPitch.Target;
            t.EulerRotation.y = (float)hYaw.Target;
            t.EulerRotation.z = (float)hRoll.Target;

            hRoll.Free();
            hYaw.Free();
            hPitch.Free();

            return t;
        }

    }
}
