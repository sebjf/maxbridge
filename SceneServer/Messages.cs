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
        Ping                = 0,
        RequestGeometry     = 1,
        GeometryUpdate      = 2,
        Error               = 3,
    }

    [Serializable]
    public class UnityMessage
    {
        public UnityMessage(MessageTypes type)
        {
            MessageType = type;
        }

        public MessageTypes MessageType;
        public UnityMessageParams Content;
    }

    [Serializable]
    public abstract class UnityMessageParams
    {
    }

    [Serializable]
    public class MessageErrorParams : UnityMessageParams
    {
        public string message;

        public MessageErrorParams(string msg)
        {
            message = msg;
        }
    }

    [Serializable]
    public class MessagePingParams : UnityMessageParams
    {
        public string message;

        public MessagePingParams(string msg)
        {
            message = msg;
        }
    }

    [Serializable]
    public class MessageGeometryUpdateParams : UnityMessageParams
    {
        public SharedMemoryInfo sharedMemory;

        public long geometryOffset;
        public long length;

        public MessageGeometryUpdateParams(MemoryMappedFile sharedMemory, long offset, long length)
        {
            this.length = length;
            this.geometryOffset = offset;
            this.sharedMemory = new SharedMemoryInfo(sharedMemory);
        }

        public MessageGeometryUpdateParams(SharedMemoryInfo sharedMemory, long offset, long length)
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
    public class GeometryInfo
    {
        SharedMemoryInfo Memory;

        public string Name;

        public int VertexCount;

        public IntPtr PositionPtr;
        public IntPtr UVCoordPtr;
        public IntPtr NormalPtr;


    }
}
