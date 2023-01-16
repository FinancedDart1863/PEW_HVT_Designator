using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.ModAPI;

using VRage.Game.Components;
using VRage.Game;
using VRage.Game.ModAPI;

namespace PEWHVTDetector
{
    public struct HVTDetectorSettings
    {
        public static int CheckForHVTInterval = 5; // How often to scan for HVT grids. This parameter is mod level but is overriden via in-game custom data.
        public static int ModLogicInterval = 1; // How often to execute mod logic. Leave this as 1 for once per second. This parameter is mod level and is not customizable via in-game custom data.
    }


    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class PEWHVTDetectorCore : MySessionComponentBase
    {
        private static DateTime m_lastUpdate;
        private bool m_initialized;

        private void Initialize()
        {
            m_initialized = true;

            if (!MyAPIGateway.Multiplayer.IsServer) //Do not proceed if this mod isn't being run on a server
            {
                Logging.Instance.WriteLine("Phobos Engineered Weaponry HVT Detection Subsystem: Client-sided execution is disabled."); //Clients init message
                MyAPIGateway.Utilities.ShowMessage("Phobos Engineered Weaponry HVT Detection Subsystem","Client-sided execution is disabled...");
                return;
            }
            MyAPIGateway.Utilities.ShowMessage("Phobos Engineered Weaponry HVT Detection Subsystem", "Initialization...");
            Logging.Instance.WriteLine("Phobos Engineered Weaponry HVT Detection Subsystem Initialization"); //Server init message

            m_lastUpdate = DateTime.Now;

        }

        public override void UpdateBeforeSimulation()
        {
            // Sanity check
            if (MyAPIGateway.Session == null)
                return;

            if (!m_initialized)
                Initialize();

            // Check our radar blocks once every second
            if (DateTime.Now - m_lastUpdate > TimeSpan.FromSeconds(HVTDetectorSettings.ModLogicInterval))
            {

                if (!MyAPIGateway.Multiplayer.IsServer) //Do not proceed if this mod isn't being run on a server
                {
                    return;
                }

                // Process detector blocks
                HVTDetectorProcess.Process();


                m_lastUpdate = DateTime.Now;
            }

            base.UpdateBeforeSimulation();
        }

        //Handle mod unloading
        protected override void UnloadData()
        {
            try
            {
                if (Logging.Instance != null)
                    Logging.Instance.Close();
            }
            catch { }

            HVTDetectorProcess.clearTimer.Stop();

            base.UnloadData();
        }
    }
}
