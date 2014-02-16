using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Max;

namespace MaxExporter
{
    public partial class MaxUnityExporter
    {
        public IEnumerable<IINode> GetGeometryNode(string Name)
        {
            return (GeometryNodes.Where(n => (n.Name == Name)));
        }

        public IEnumerable<IINode> GeometryNodes
        {
            get
            {
                return (SceneNodes.Where(n => (n.ObjectRef is ITriObject)));
            }
        }

        public IEnumerable<IINode> SceneNodes
        {
            get
            {
                return GetChildNodesRecursive(globalInterface.COREInterface.RootNode);
            }
        }

        private IEnumerable<IINode> GetChildNodesRecursive(IINode start)
        {
            for (int i = 0; i < start.NumChildren; i++)
            {
                yield return start.GetChildNode(i);

                foreach (var n in GetChildNodesRecursive(start.GetChildNode(i)))
                    yield return n;
            }
        }

    }
}
