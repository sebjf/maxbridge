using System;
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

    /* These are the only types that Unity supports for now */
    [Serializable]
    public enum VertexChannelType //these values map to the max sdk so we can cast from the map numbers in the max plugin, careful not to change them without modifying that section as well.
    {
        Positions       = -3,
        Normals         = -4,
        VertexColours   = 0,    //these values map to the max sdk so we can cast from the map numbers in the max plugin, careful not to change them without modifying that section as well.
        Texture1        = 1,
        Texture2        = 2,
        Texture3        = 3,
        Texture4        = 4,
        Texture5        = 5,
        Texture6        = 6,
        Texture7        = 7     //(anyone who needs more than 7 mapping channels can rebuild this themselves!)
    }

    [Serializable]
    public class VertexChannel
    {
        public VertexChannelType m_type;
        public Indices3[] m_faces;
        public Point3[] m_vertices;
    }

    [Serializable]
    public class FaceGroup
    {
        public int[] m_faceIndices;
        public int m_materialId;
    }

    [Serializable]
    public class GeometryNode
    {
        public string Name;
        public string Parent;

        public TRS Transform;

        public List<FaceGroup> FaceGroups = new List<FaceGroup>();
        public List<VertexChannel> Channels = new List<VertexChannel>();

        public short[] faceFlags;

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

    /* We should look into how often anything other than ITexMaps are used in max. ITexMap is an IAnimatble, so we could actually pass a handle direct to that
     * object instead of going through the owner as in here. */

    [Serializable]
    public class ParameterReference
    {
        public ulong m_ownerAnimHandle; //Again, consider this valid for *milliseconds* only!
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

        /* This is the handle of the native object in max. It can be used to cache the resolved value locally. 
        It cannot be used to retreieve an actual object from max, you need an anim(atable) handle for that. */
        public long m_nativeHandle;    
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
