using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Max;
using Autodesk.Max.Plugins;
using System.Threading;


namespace MaxSceneServer
{
    public static class AssemblyFunctions
    {
        public static void AssemblyMain()
        {
            var g = Autodesk.Max.GlobalInterface.Instance;
            var i = g.COREInterface13;
            i.AddClass(new MaxSceneServerUtilityDescriptor(g));
        }

        public static void AssemblyShutdown()
        {
        }
    }

    /// <summary>
    /// Provides information about our plugin without having to create an instance of it
    /// </summary>
    public class MaxSceneServerUtilityDescriptor : ClassDesc2
    {
        IGlobal global;
        internal static IClass_ID classID;

        public MaxSceneServerUtilityDescriptor(IGlobal global)
        {
            this.global = global;

            // The two numbers used for class id have to be unique/random
            classID = global.Class_ID.Create(681601651, 1321680997);
        }

        public override string Category
        {
            get { return "Bridge Server"; }
        }

        public override IClass_ID ClassID
        {
            get { return classID; }
        }

        public override string ClassName
        {
            get { return "Max-Unity Scene Server"; }
        }

        public override object Create(bool loading)
        {
            return new MaxExporterUtility();
        }

        public override bool IsPublic
        {
            // true to make our plugin visible in 3dsmax interface
            get { return false; }
        }

        public override SClass_ID SuperClassID
        {
            get { return SClass_ID.Gup; }
        }
    }

    public class MaxExporterUtility : Autodesk.Max.Plugins.GUP
    {
        public MaxExporterUtility()
        {
            global = Autodesk.Max.GlobalInterface.Instance;
            Log.logger = global.COREInterface.Log;
            Log.EnableLog = true;
        }

        protected IGlobal global;
        protected MaxSceneServer plugin;

        public override void Dispose()
        {
            base.Dispose();
        }

        protected const uint GUPRESULT_KEEP = 0x00;
        protected const uint GUPRESULT_NOKEEP = 0x01;
        protected const uint GUPRESULT_ABORT = 0x03;

        public override uint Start
        {
            get 
            {
                beginUnityServer();
                return GUPRESULT_KEEP; 
            }
        }

        public override void Stop()
        {
            plugin.StopServer();
        }

        protected void beginUnityServer()
        {
            Log.Add("[m] Starting MaxUnityBridge Server.");
            plugin = new MaxSceneServer(global);
            plugin.StartServer();
        }
    }
}
