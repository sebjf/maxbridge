using System;
using System.Collections.Generic;
using Autodesk.Max;
using Messaging;
using System.Runtime.InteropServices;

namespace MaxSceneServer
{
    public static class Extensions
    {
        public static Point3 ToPoint3(this IPoint3 v)
        {
            return new Point3() { x = v.X, y = v.Y, z = v.Z };
        }
    }

    public partial class MaxSceneServer
    {
        protected const uint  TRIOBJ_CLASS_ID 	 	= 0x0009;	    //!< TriObject class ID
        protected const uint  EDITTRIOBJ_CLASS_ID	= 0xe44f10b3;	    //!< Base triangle mesh (Edit class ID
        protected const uint  POLYOBJ_CLASS_ID		= 0x5d21369a;	//!< Polygon mesh (PolyObject) class ID
        protected const uint  PATCHOBJ_CLASS_ID  	= 0x1030;      //!< Patch object
        protected const uint  NURBSOBJ_CLASS_ID		= 0x4135;      //!< Nurbs object 

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);



        protected IEnumerable<GeometryNode> CreateUpdates()
        {
            foreach (var n in SceneNodes)
            {
                var u = CreateGeometryUpdate(n);
                if(u != null){
                    yield return u;
                }
            }
        }

        protected unsafe GeometryNode CreateGeometryUpdate(IINode node)
        {
            IObject obj = node.EvalWorldState(0, false).Obj;
            if (obj.CanConvertToType(_gi.Class_ID.Create(TRIOBJ_CLASS_ID, 0)) > 0)
            {
                ITriObject triobj = obj.ConvertToType(0, _gi.Class_ID.Create(TRIOBJ_CLASS_ID, 0)) as ITriObject;

                GeometryNode u = CreateGeometryUpdate(node, triobj);

                if (obj.Handle != triobj.Handle) /*If the triobject was created by the call above instead of existing prior to this*/
                {
                    triobj.DeleteMe();
                }

                return u;
            }

            return null;
        }

        protected unsafe GeometryNode CreateGeometryUpdate(IINode node, ITriObject maxGeometry)
        {
            GeometryNode update = new GeometryNode();


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

            update.NormalFaces = new Indices3[numnormalfaces];
            for (int i = 0; i < numnormalfaces; i++)
            {
                IMeshNormalFace f = normalspec.Face(i);
                update.NormalFaces[i] = *(Indices3*)f.NormalIDArray;
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


            /*
             * Scene objects have one, or none, material. This member will be that materials name, even if that material is a container like a Composite 
             * or Shell material. 
             * The client will attach the Material ID of the face when/if the materials are split out, which will allow the materials
             * processing code to identify and import the correct material properties later. (In practice, we dont even need to store this - since knowing
             * the node name will allow us to find it - but sending it allows us to match the functionality of the FBX importer.
             */

            if(node.Mtl != null){
                update.MaterialName = node.Mtl.Name;
            }

            return update;
        }

        unsafe protected TRS GetTransform(IINode node)
        {
            TRS t = new TRS();

            IMatrix3 tm = node.GetObjectTM(0, _gi.Interval.Create());


            IAffineParts affine = _gi.AffineParts.Create();
            _gi.DecompAffine( tm, affine);


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
