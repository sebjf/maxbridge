using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MaxUnityBridge
{
    public class UnityImporterWindow : EditorWindow
    {
        [MenuItem("Assets/MaxUnityBridge")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(UnityImporterWindow));
        }

        public UnityImporterWindow()
        {
            importer = new UnityImporter();
        }

        protected UnityImporter importer;

        void OnGUI()
        {
            EditorGUILayout.LabelField("Max Unity Bridge Importer");
            if (GUILayout.Button("Button"))
            {
                if (!doUpdate) { doUpdate = true; }
                importer.DoImport();
            }
        }

        bool doUpdate = false;

        void Update()
        {
            if (doUpdate)
            {
               
                doUpdate = false;
            }
        }
    }
}
