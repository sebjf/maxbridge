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
