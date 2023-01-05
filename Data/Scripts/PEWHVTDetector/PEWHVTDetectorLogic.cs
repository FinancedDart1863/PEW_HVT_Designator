using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;

using VRage;
using VRage.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace PEWHVTDetector
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), true, "LargeBlockPEWKOTH")]
    class PEWHVTDetectorLogic : MyGameLogicComponent
    {
        public static Dictionary<IMyEntity, MyTuple<IMyEntity, DateTime>> LastPEWHVTDetectorUpdate
        {
            get
            {
                return m_lastUpdate;
            }
        }

        private MyObjectBuilder_EntityBase m_objectBuilder = null;
        private static Dictionary<IMyEntity, MyTuple<IMyEntity, DateTime>> m_lastUpdate = null;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (m_lastUpdate == null)
                m_lastUpdate = new Dictionary<IMyEntity, MyTuple<IMyEntity, DateTime>>();

            IMyBeacon beacon = (IMyBeacon)Entity;
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

            // Since our object builder is null, use Entity.
            if (beacon == null)
            {
                //Logging.Instance.WriteLine("Entity is null");
            }
            else if (beacon.BlockDefinition.SubtypeName.Contains("PEWKOTH"))
            {
                if (!m_lastUpdate.ContainsKey(Entity))
                {
                    m_lastUpdate.Add(Entity, new MyTuple<IMyEntity, DateTime>(Entity, DateTime.Now));
                }
            }
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return m_objectBuilder;
        }

        public override void Close()
        {
            Logging.Instance.WriteLine(string.Format("Close PEWHVTDetector Logic"));

            if (Entity == null)
                return;

            if (m_lastUpdate.ContainsKey(Entity))
                m_lastUpdate.Remove(Entity);
        }
    }
}
