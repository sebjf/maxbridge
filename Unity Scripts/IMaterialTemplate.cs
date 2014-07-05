using UnityEngine;
using System.Collections;
using MaxUnityBridge;
using Messaging;

public interface IMaterialTemplate
{
	string Name { get; }
	Material CreateNewInstance(MaterialInformation settings);
}
