using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Max;
using Messages;

namespace MaxExporter
{
    public partial class MaxUnityExporter
    {
        GeometryUpdate createGeometryUpdate()
        {
            GeometryUpdate update = null;

            IINode geom = GeometryNodes.FirstOrDefault();

            if (geom != null)
            {
                

            }

            return update;
        }

        void buildGeometryUpdate()
        {

        }

    }
}
