﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Messaging
{
    /* The Quaternion class defined by Max. Note that in the Max SDK [x,y,z] is the axis of rotation and w is the angle. Rotation convention is left hand for the API. */
    
    [Serializable]
    public unsafe struct Quat
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    /* These definitions mirror the unmanaged types used within the 3ds max Mesh class - http://download.autodesk.com/global/docs/3dsmaxsdk2012/en_us/index.html */

    /* The Point3 type is used to store vertices and normals, the managed equivalent is IPoint3 
     * http://download.autodesk.com/global/docs/3dsmaxsdk2012/en_us/index.html */

    [Serializable]
    public unsafe struct Point3
    {
        public float x;
        public float y;
        public float z;
    }

    /* The TVFace type is used to store offsets into the texture vertex array so that the texture and geometric surfaces can be defined independently, the managed equivalent is ITVFace
     * http://download.autodesk.com/global/docs/3dsmaxsdk2012/en_us/index.html */

    [Serializable]
    public unsafe struct TVFace : ITripleIndex
    {
        public UInt32 t1;   //the correct defintion is a DWORD[3] array but we split it out to make creating the face easier
        public UInt32 t2;
        public UInt32 t3;

        #region ITripleIndex Implementation 

        public int i1
        {
            get { return (int)t1; }
        }

        public int i2
        {
            get { return (int)t2; }
        }

        public int i3
        {
            get { return (int)t3; }
        }

        #endregion
    }

    /* so we can do the copy with one assignment */
    [Serializable]
    public unsafe struct Indices3 : ITripleIndex
    {
        public UInt32 v1;
        public UInt32 v2;
        public UInt32 v3;


        #region ITripleIndex Implementation
        
        public int i1
        {
            get { return (int)v1; }
        }

        public int i2
        {
            get { return (int)v2; }
        }

        public int i3
        {
            get { return (int)v3; }
        }

        #endregion
    }

    /* The Face type stores the offets into the vertex array to create the geometric surface. It also stores smoothing group information, edge visibility flags and material information.
     * http://download.autodesk.com/global/docs/3dsmaxsdk2012/en_us/index.html */

    [Serializable]
    public unsafe struct Face : ITripleIndex
    {
        public Indices3 v;
        public UInt32 smGroup;
        public UInt32 flags;


        #region ITripleIndex Implementation
        
        public int i1
        {
            get { return (int)v.v1; }
        }

        public int i2
        {
            get { return (int)v.v2; }
        }

        public int i3
        {
            get { return (int)v.v3; }
        }

        #endregion
    }

    public interface ITripleIndex
    {
        int i1 { get; }
        int i2 { get; }
        int i3 { get; }
    }
}
