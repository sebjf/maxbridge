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

            /*
             * Scene objects have one, or none, material. This member will be that materials name, even if that material is a container like a Composite 
             * or Shell material. 
             * The client will attach the Material ID of the face when/if the materials are split out, which will allow the materials
             * processing code to identify and import the correct material properties later. (In practice, we dont even need to store this - since knowing
             * the node name will allow us to find it - but sending it allows us to match the functionality of the FBX importer.
             */

            if (node.Mtl != null)
            {
                update.MaterialName = node.Mtl.Name;
            }

            IMesh mesh = maxGeometry.Mesh;

            /* Get the master face array and vertex positions. We split by material id here also for convenience. */

            var facesAndPositions = GetTriMeshFacesAndPositions(mesh);

            update.FaceGroups.AddRange(MakeFaceGroupsFromMaterialIds(facesAndPositions.face_materialIds));
            update.faceFlags = facesAndPositions.face_flags;
            update.Channels.Add(facesAndPositions.positions_channel);

            /* Get the remaining properties, such as normals and texture coordinates */

            update.Channels.Add(GetTriMeshNormals(mesh));
            update.Channels.AddRange(GetTriMeshMapChannels(mesh));

            return update;
        }

        protected IEnumerable<FaceGroup> MakeFaceGroupsFromMaterialIds(short[] face_materialIds)
        {
            Dictionary<short, List<int>> faceLists = new Dictionary<short, List<int>>();

            for (int i = 0; i < face_materialIds.Length; i++)
            {
                short materialid = face_materialIds[i];

                if (!faceLists.ContainsKey(materialid))
                {
                    faceLists.Add(materialid, new List<int>());
                }

                faceLists[materialid].Add(i);
            }

            foreach (var p in faceLists)
            {
                yield return new FaceGroup { m_materialId = p.Key, m_faceIndices = p.Value.ToArray() };
            }
        }

        protected unsafe IEnumerable<VertexChannel> GetTriMeshMapChannels(IMesh mesh)
        {


            /* 3ds Max SDK Programmer's Guide > 3ds Max SDK Features > Rendering > Textures Maps > Mapping Channels */
            /* There are up to 99 user maps in each mesh:
             * -2 Alpha
             * -1 Illum 
             *  0 vertex colour
             *  1 default texcoords 
             *  1 - 99 user uvw maps
             * Maps can be inspected for a mesh using the Edit > Channel Info... dialog in max.
             * When maps are added using the modifier stack they are always sequential. I.e. if you add just one uvw unwrap, and set it to map 5, four other preceeding channels will be created.
             */

            List<VertexChannel> channels = new List<VertexChannel>(); //no unsafe code in iterators

            /* The two hidden maps are not counted in the getNumMaps() return value */

            for (int i = 0; i < mesh.NumMaps; i++)
            {
                var map = mesh.Map(i);

                if (map.IsUsed)
                {
                    VertexChannel channel = new VertexChannel();

                    channel.m_vertices = new Point3[map.Vnum];
                    fixed (Point3* dataptr = channel.m_vertices)
                    {
                        CopyMemory((IntPtr)dataptr, map.Tv.Handle, (uint)(sizeof(Point3) * map.Vnum));
                    }

                    //           channel.m_faces = new TVFace[map.Fnum];  //the indices3 struct is identical to the tvface struct but they are different in the sdk. for efficiency we just indices3 directly here.
                    channel.m_faces = new Indices3[map.Fnum];
                    fixed (Indices3* dataptr = channel.m_faces)
                    {
                        CopyMemory((IntPtr)dataptr, map.Tf.Handle, (uint)(sizeof(Indices3) * map.Fnum));
                    }

                    channel.m_type = (VertexChannelType)i;

                    channels.Add(channel);
                }
            }

            return channels;
        }

        protected unsafe VertexChannel GetTriMeshNormals(IMesh mesh)
        {
            VertexChannel channel = new VertexChannel();
            channel.m_type = VertexChannelType.Normals;

            /* The MeshNormalSpec class is intended to store user specified normals. We can use it however to have max calculate the 
             * normals in the typical way and provide easy access to them. */

            IMeshNormalSpec normalspec = mesh.SpecifiedNormals;
            bool normalsAlreadySpecified = (normalspec.NormalArray != null);
            if (!normalsAlreadySpecified)
            {
                mesh.SpecifyNormals();
            }

            normalspec.CheckNormals();

            channel.m_vertices = new Point3[normalspec.NumNormals];

            fixed (Point3* normalData = channel.m_vertices)
            {
                CopyMemory((IntPtr)normalData, normalspec.NormalArray.Handle, (uint)(sizeof(Point3) * normalspec.NumNormals));
            }

            int numnormalfaces = normalspec.NumFaces;

            channel.m_faces = new Indices3[numnormalfaces];
            for (int i = 0; i < numnormalfaces; i++)
            {
                IMeshNormalFace f = normalspec.Face(i);
                channel.m_faces[i] = *(Indices3*)f.NormalIDArray;
            }

            if (!normalsAlreadySpecified)
            {
                mesh.ClearSpecifiedNormals();
            }

            return channel;
        }

        public struct TriMeshFacesAndPositions
        {
            public VertexChannel positions_channel;
            public short[] face_materialIds;
            public short[] face_flags;
        }

        protected unsafe TriMeshFacesAndPositions GetTriMeshFacesAndPositions(IMesh mesh)
        {
            VertexChannel channel = new VertexChannel();
            channel.m_type = VertexChannelType.Positions;

            channel.m_vertices = new Point3[mesh.NumVerts];
            fixed (Point3* vertexData = channel.m_vertices)
            {
                CopyMemory((IntPtr)vertexData, mesh.GetVertPtr(0).Handle, (uint)(sizeof(Point3) * mesh.NumVerts));
            }

            var faces = new Face[mesh.NumFaces];
            fixed (Face* faceData = faces)
            {
                CopyMemory((IntPtr)faceData, mesh.Faces[0].Handle, (uint)(sizeof(Face) * mesh.NumFaces));
            }

            /* Split the face data into seperate arrays. Put the face indices in one with the channel, the others to one side to filter into groups later if the user wants */

            TriMeshFacesAndPositions faces_data = new TriMeshFacesAndPositions();

            faces_data.face_flags = new short[faces.Length];
            faces_data.face_materialIds = new short[faces.Length];

            channel.m_faces = new Indices3[faces.Length];

            for (int i = 0; i < faces.Length; i++)
            {
                short materialid = (short)((faces[i].flags & 0xFFFF0000) >> 16); //in Max the high word of the flags member contains the material id.
                short flags = (short)(faces[i].flags & 0x0000FFFF);

                faces_data.face_flags[i] = flags;
                faces_data.face_materialIds[i] = materialid;

                channel.m_faces[i] = faces[i].v;
            }

            faces_data.positions_channel = channel;

            return faces_data;
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
