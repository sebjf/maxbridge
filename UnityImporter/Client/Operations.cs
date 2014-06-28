﻿using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Messaging;
using UnityEngine;
using System.Collections.Generic;

namespace MaxUnityBridge
{
    public partial class UnityImporter
    {
        public void DoImport()
        {
            ProcessIsochronous(new MessageGeometryRequest());
        }

        public IEnumerable<MaterialInformation> GetMaterials(string node)
        {
            var m = ExchangeIsochronous(new MessageMaterialRequest(node)) as MessageMaterials;
            return m.m_matchingMaterials;
        }

        public IEnumerable<MaterialInformation> GetSubMaterials(MaterialInformation material, int index)
        {
            var m = ExchangeIsochronous(new MessageMaterialRequest(material.m_handle, index)) as MessageMaterials;
            return m.m_matchingMaterials;
        }
    }
}
