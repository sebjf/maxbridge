using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Messaging;
using UnityEngine;

namespace MaxUnityBridge
{
    public partial class UnityImporter
    {
        public void DoImport()
        {
            ProcessIsochronous(new MessageGeometryRequest());
        }


    }
}
