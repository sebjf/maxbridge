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

}
