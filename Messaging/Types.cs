﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Messaging
{
    [Serializable]
    public struct TRS
    {
        public Point3 Translate;
        public Point3 Scale;
        public Point3 EulerRotation;
    }

    [Serializable]
    public class MapChannel
    {
        public int Id;
        public Point3[] Coordinates;
        public TVFace[] Faces;
    }

    [Serializable]
    public class GeometryNode
    {
        public string Name;
        public string Parent;

        public TRS Transform;

        public Point3[] Vertices;
        public Face[] Faces;

        public Point3[] Normals;
        public Indices3[] NormalFaces;

        public List<MapChannel> Channels = new List<MapChannel>();

        public string MaterialName;
    }

    [Serializable]
    public class MaterialInformation
    {
        public string m_className;
        public string m_materialName;

        /* Like the map reference, this is analogous to a pointer to the actual material object. Be careful with it, 
         * Max (or more likely the user) could change it at any moment. Use it, for example, to get submaterials
         * and then forget it.*/
        public ulong m_handle;

        public List<MaterialProperty> MaterialProperties = new List<MaterialProperty>();
    }

    [Serializable]
    public class MaterialProperty
    {
        public string m_name;
        public string m_alias;
        public object m_value;

        public MaterialProperty(string name, string alias, object value)
        {
            m_name = name;
            m_alias = name;
            if (alias != null) { m_alias = alias; }
            m_value = value;
        }
    }

    /* The MaterialReference and MapReference are used to identify a specific object, that can be retrieved later, because to process it may be expensive.
     * These references are highly volatile. Not only should they not be stored, but every time the user wants, say, a map for a particular material slot,
     * that material should be retrieved along with the latest reference, in case the user in max has changed the map since the GetMaterial() method was last
     * called. */

    /* The ParameterReference objects contains the information required to find and acquire any property value with no starting reference. */

    [Serializable]
    public class ParameterReference
    {
        public ulong m_ownerAnimHandle;
        public short m_paramBlockId;
        public short m_paramId;
        public int m_tableId;
    }

    [Serializable]
    public class MaterialReference
    {
        public ParameterReference m_parameterReference;
    }

    [Serializable]
    public class MapReference
    {
        public ParameterReference m_parameterReference;
        public string m_mapType;
        public string m_mapName;
    }

    [Serializable]
    public class fRGBA
    {
        public float r, g, b, a;

        public fRGBA(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public fRGBA() //for the deserialiser
        {
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3}", r, g, b, a);
        }
    }
}
