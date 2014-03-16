using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Winterdom.IO.FileMap;

namespace Messages
{
    [Serializable]
    public class MaxPing
    {
        public string msg = "Hello from Max";
    }

    [Serializable]
    public enum MessageTypes : int
    {
        Ping,
        RequestGeometry,
        GeometryUpdateStream,
        GeometryUpdateMemory,
        Error,
    }

    [Serializable]
    public class UnityMessage
    {
        public UnityMessage(MessageTypes type)
        {
            MessageType = type;
        }

        public MessageTypes MessageType;
    }

    [Serializable]
    public class MessageError : UnityMessage
    {
        public string message;

        public MessageError(string msg) : base(MessageTypes.Error)
        {
            message = msg;
        }
    }

    [Serializable]
    public class MessagePing : UnityMessage
    {
        public string message;

        public MessagePing(string msg) : base(MessageTypes.Ping)
        {
            message = msg;
        }
    }

    [Serializable]
    public class MessageGeometryUpdateMemory : UnityMessage
    {
        public SharedMemoryInfo sharedMemory;

        public long geometryOffset;
        public long length;

        public MessageGeometryUpdateMemory(MemoryMappedFile sharedMemory, long offset, long length) : base(MessageTypes.GeometryUpdateMemory)
        {
            this.length = length;
            this.geometryOffset = offset;
            this.sharedMemory = new SharedMemoryInfo(sharedMemory);
        }

        public MessageGeometryUpdateMemory(SharedMemoryInfo sharedMemory, long offset, long length) : base(MessageTypes.GeometryUpdateMemory)
        {
            this.length = length;
            this.geometryOffset = offset;
            this.sharedMemory = sharedMemory;
        }
    }

    [Serializable]
    public class SharedMemoryInfo
    {
        public string name;
        public int size;

        public SharedMemoryInfo(MemoryMappedFile sharedMemory)
        {
            this.name = sharedMemory.Name;
            this.size = (int)sharedMemory.Size;
        }
    }

    [Serializable]
    public class MessageGeometryUpdateStream : UnityMessage
    {
        public MessageGeometryUpdateStream(IEnumerable<GeometryUpdate> updates)
            : base(MessageTypes.GeometryUpdateStream)
        {
            this.Geometries = updates.ToArray();
        }

        public GeometryUpdate[] Geometries;
    }

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
    public class GeometryUpdate
    {
        public string Name;
        public string Parent;

        public TRS Transform;

        public Point3[] Vertices;
        public Face[] Faces;

        public Point3[] Normals;

        public List<MapChannel> Channels = new List<MapChannel>();
        
        public List<MaterialInformation> Materials = new List<MaterialInformation>();
    }

    [Serializable]
    public class MaterialInformation
    {
        public string Class;
        public Dictionary<string, object> MaterialProperties = new Dictionary<string, object>();
    }

    [Serializable]
    public class MapInformation
    {
        public string Filename;
    }

    [Serializable]
    public struct fRGBA
    {
        public float r, g, b, a;

        public fRGBA(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3}", r, g, b, a);
        }
    }

}
