using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messaging;
using UnityEngine;

namespace MaxUnityBridge
{
    public interface IMaterialTemplate
    {
        string Name { get; }
        Material CreateNewInstance(MaterialInformation settings);
    }

}
