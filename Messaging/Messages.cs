using System;
using System.Collections.Generic;
using System.Linq;

namespace Messaging
{
    [Serializable]
    public class UnityMessage
    {
    }

    [Serializable]
    public class MessagePing : UnityMessage
    {
        public string message;

        public MessagePing(string msg)
        {
            message = msg;
        }
    }

    [Serializable]
    public class MessageError : UnityMessage
    {
        public string message;

        public MessageError(string msg)
        {
            message = msg;
        }
    }

    [Serializable]
    public class MessageGeometryRequest : UnityMessage
    {
    }

    [Serializable]
    public class MessageGeometryUpdate : UnityMessage
    {
        public MessageGeometryUpdate(IEnumerable<GeometryNode> updates)
        {
            this.Geometries = updates.ToArray();
        }

        public GeometryNode[] Geometries;
    }

    [Serializable]
    public class MessageMaterialRequest : UnityMessage
    {
        /* It is valid to use both the handle and nodename items simultaneously when requesting 
         * materials. The matching materials from each will be returned sequentially in the 
         * enumerable. */

        public MessageMaterialRequest(string nodeName)
        {
            m_nodeName = nodeName;
        }

        public MessageMaterialRequest(ulong handle)
        {
            m_handle = handle;
        }

        public MessageMaterialRequest(string nodeName, int materialIndex)
        {
            m_nodeName = nodeName;
            m_materialIndex = materialIndex;
        }

        public MessageMaterialRequest(ulong handle, int materialIndex)
        {
            m_handle = handle;
            m_materialIndex = materialIndex;
        }

        public readonly string m_nodeName = null;
        public readonly int m_materialIndex = -1;
        public readonly ulong m_handle = 0;
    }

    [Serializable]
    public class MessageMaterials : UnityMessage
    {
        public MessageMaterials(IEnumerable<MaterialInformation> matching_materials)
        {
            m_matchingMaterials = new List<MaterialInformation>(matching_materials);
        }

        public readonly List<MaterialInformation> m_matchingMaterials;
    }

    [Serializable]
    public class MessageMapRequest : UnityMessage
    {
        public MessageMapRequest(MapReference map, string filename)
        {
            m_map = map;
            m_filename = filename;
        }

        public MessageMapRequest(MapReference map, string filename, int width, int height)
        {
            m_map = map;
            m_filename = filename;
            m_width = (ushort)width;
            m_height = (ushort)height;
        }

        public readonly MapReference m_map;

        public readonly bool m_writeToFile = true;
        public readonly string m_filename;
        public readonly ushort m_width = 1024;
        public readonly ushort m_height = 1024;
        public readonly bool m_filter = false;
    }

    [Serializable]
    public class MessageMapContent : UnityMessage
    {
        public MapReference m_mapReference;
    }

    [Serializable]
    public class MessageMapFilename : MessageMapContent
    {
        public MessageMapFilename(string filename)
        {
            m_filename = filename;
        }

        public string m_filename;
    }


}
