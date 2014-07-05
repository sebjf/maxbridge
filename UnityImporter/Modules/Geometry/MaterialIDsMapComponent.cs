using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Maps between the material slot in Unity and the material id in Max. Updated with the geometry, or set manually.
 There should be one entry for every material index in the materials[]/sharedMaterials[] array of the renderer. */

namespace MaxUnityBridge
{
    public class MaterialIDsMap : MonoBehaviour
    {
        public List<int> m_materialIds = new List<int>();

        public int GetIdForMaterialSlot(int slot_number)
        {
            if (m_materialIds.Count > slot_number)
            {
                return m_materialIds[slot_number];
            }
            return -1;
        }
    }
}