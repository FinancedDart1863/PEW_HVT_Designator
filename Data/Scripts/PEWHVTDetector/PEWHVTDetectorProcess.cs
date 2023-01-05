using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;


using VRage;
using VRageMath;
using VRage.Common;
using VRage.Definitions;
using VRage.ModAPI;
using VRage.Serialization;
using VRage.Game.Entities;
using VRage.Game.Components;
using VRage.Game;
using VRage.Game.ModAPI;

namespace PEWHVTDetector
{
    public class HVTDetectorProcess
    {
        private static List<IMyEntity> m_updateList;
        private static List<RadarObjects> m_radarObjectList;
        private static bool m_init = false;
        private static bool m_LCDClear = false;

        //private static List<IMyEntity> m_LCDParentList = new List<IMyEntity>();
        private static List<RadarOutputItem> m_radarOutputList = new List<RadarOutputItem>();
        internal static System.Timers.Timer clearTimer = new System.Timers.Timer();

        /// <summary>
        /// Process Radar Blocks
        /// </summary>
        public static void Process()
        {
            if (MyAPIGateway.Session == null)
                return;
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;
            if (PEWHVTDetectorLogic.LastPEWHVTDetectorUpdate == null)
                return;
            if (!m_init)
            {
                m_init = true;
                Initialize();
            }

            // Check radar block once every second
            foreach (KeyValuePair<IMyEntity, MyTuple<IMyEntity, DateTime>> p in PEWHVTDetectorLogic.LastPEWHVTDetectorUpdate)
            {
                if (DateTime.Now - p.Value.Item2 > TimeSpan.FromSeconds(PEWHVTDetector.HVTDetectorSettings.CheckForHVTInterval))
                {
                    ProcessRadarItem(p.Value.Item1);
                    m_updateList.Add(p.Key);
                }
            }

            // Update timer on radar block
            foreach (IMyEntity updatedItem in m_updateList)
            {
                if (PEWHVTDetectorLogic.LastPEWHVTDetectorUpdate.ContainsKey(updatedItem))
                    PEWHVTDetectorLogic.LastPEWHVTDetectorUpdate[updatedItem] = new MyTuple<IMyEntity, DateTime>(updatedItem, DateTime.Now);
            }

            m_updateList.Clear();
        }

        private static void Initialize()
        {
            //MyAPIGateway.Utilities.ShowMessage("PEW HVT Subsystem", "Detector Initialization...");
            MyVisualScriptLogicProvider.SendChatMessageColored("PEW HVT Subsystem: Detector Initialization...", VRageMath.Color.White);
            m_updateList = new List<IMyEntity>();

            m_radarObjectList = new List<RadarObjects>();

            AddScanItem("Massive", 1f, 1f, 500f);
            AddScanItem("Huge", 1f, 1f, 250f);
            AddScanItem("Large", 1f, 1f, 100f);
            AddScanItem("Medium", 1f, 1f, 50f);
            AddScanItem("Small", 1f, 1f, 25f);
            AddScanItem("Tiny", 1f, 1f, 0f);

        }

        private static void AddScanItem(string name, float min, float max, float size)
        {
            RadarObjects p = new RadarObjects();
            p.SizeName = name;
            p.MinimumDistance = min;
            p.MaximumDistance = max;
            p.Size = size;
            m_radarObjectList.Add(p);
        }

        private static bool ProcessRadarItem(IMyEntity entity)
        {
            // Sanity check
            if (!(entity is IMyBeacon))
                return false;

            IMyEntity parent = entity.GetTopMostParent();

            // Handle for beacon object
            IMyBeacon beacon = (IMyBeacon)entity;

            // Needs to be on and working
            if (!beacon.IsWorking || !beacon.IsFunctional)
                return false;

            Vector3D position = parent.GetPosition();
            double radius = (double)1000000;
            BoundingSphereD sphere = new BoundingSphereD(position, radius);
            List<IMyEntity> entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

            foreach (IMyEntity foundEntity in entities)
            {
                // Projection or invalid object
                if (foundEntity.Physics == null)
                    continue;

                // Waypoints and other things that are free of physics
                if (!foundEntity.Physics.Enabled)
                    continue;

                // Ignore our own ship
                if (foundEntity == entity.GetTopMostParent())
                    break;

                if (foundEntity is IMyCubeGrid)
                {
                    double distance = Vector3D.DistanceSquared(foundEntity.GetPosition(), position);
                    distance = Math.Sqrt(distance);
                    if (distance < radius)
                    {
                        FlagAsHVTDeterminer(foundEntity, parent, distance, radius, beacon);
                    }
                }
            }
            return true;
        }

        private static void FlagAsHVTDeterminer(IMyEntity entity, IMyEntity parent, double distance, double radius, IMyBeacon detectorBeacon)
		{
			try 
            {
				RadarObjectTypes radarType = GetEntityObjectType(entity);
				RadarObjects radarItem = GetEntityObjectSize(entity);
				
				float maxdist = radarItem.MaximumDistance;
				double objectsize = (entity.PositionComp.LocalAABB.Max - entity.PositionComp.LocalAABB.Min).Volume;
				
				Vector3D position = entity.GetPosition();

                int blockCountThreshold = (int)7500;

                String customdata = detectorBeacon.CustomData;
			    if (customdata.Length<2) // empty string, so let's get some defaults in there first
			    {
                    detectorBeacon.CustomData = "HVTBlockCountThreshold:3000\n";
			    }
			    customdata = customdata.ToLowerInvariant();
                //MyAPIGateway.Utilities.ShowMessage("Debug", "break1");
                if (customdata.Contains("hvtblockcountthreshold:")) 
                {
                    //MyAPIGateway.Utilities.ShowMessage("Debug", "break2");
                    int pos = customdata.IndexOf("hvtblockcountthreshold:") + 23;
                    string minString = customdata.Substring(pos, customdata.Length - pos);
                    string corrected = "";
                    for (int r = 0; r < minString.Length; r++) 
                    {
                        int test = 0;
                        string numTest = "";
                        numTest += minString[r];
                        if (!int.TryParse(numTest, out test))
                            break;

                        corrected += minString[r];
                    }
                    //MyAPIGateway.Utilities.ShowMessage("Corrected", corrected);
                    int failInterval = (int)7500;
                    blockCountThreshold = int.TryParse(corrected, out failInterval) ? failInterval : (int)7500;
                }
                else
                {
                    blockCountThreshold = (int)7500;
                }

                //We perform the corresponding HVT functions on the grid's beacons
                MyCubeGrid grid = entity as MyCubeGrid; //Convert entity to grid
                IMyCubeGrid mygrid = entity as IMyCubeGrid;
                IMyGridGroupData mygridGroup = MyAPIGateway.GridGroups.GetGridGroup(GridLinkTypeEnum.Physical, mygrid);
                List<IMyCubeGrid> mygridGroupGrids = new List<IMyCubeGrid>(); //Define list grids within the gridGroup
                mygridGroup.GetGrids(mygridGroupGrids);
                int gridGroupTotalBlocks = 0;
                foreach (IMyCubeGrid gridX in mygridGroupGrids)
                {
                    MyCubeGrid gridXCast = gridX as MyCubeGrid;
                    gridGroupTotalBlocks += gridXCast.BlocksCount;
                    //MyAPIGateway.Utilities.ShowMessage("DebugGridGroupCount", "AddingSubgridCount");
                }

                if ((grid.BlocksCount > blockCountThreshold) || (gridGroupTotalBlocks > blockCountThreshold))
                {
                    MyVisualScriptLogicProvider.SendChatMessageColored("A high value target has been spotted!", VRageMath.Color.White);
                    if (!grid.IsPowered)
                    {
                        grid.SwitchPower();
                    }

                    List<MyCubeBlock> gridBeaconBlocks = new List<MyCubeBlock>(); //Define list of MyCubeBlocks
                    foreach(MyCubeBlock block in grid.GetFatBlocks())
                    {
                        var battery = block as IMyBatteryBlock;
                        if (battery != null)
                        {
                           if (!battery.Enabled)
                           {
                               battery.Enabled = true;
                           }
                        }
                        var beacon = block as IMyBeacon;
                        if(beacon != null)
                        {
                            //MyAPIGateway.Utilities.ShowMessage("Debug","Beacon detected");
                            IMyFaction HVTFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag("SPRT");
                            long HVTFactionOwner = HVTFaction.FounderId;
                            if(!beacon.IsFunctional)
                            {
                                ((IMySlimBlock)beacon.SlimBlock).IncreaseMountLevel(((IMySlimBlock)beacon.SlimBlock).MaxIntegrity,HVTFactionOwner);
                            }
                        
                            if(!beacon.Enabled)
                            {
                                beacon.Enabled = true;
                            }

                            block.ChangeOwner(HVTFactionOwner, MyOwnershipShareModeEnum.Faction);
                            //MyAPIGateway.Utilities.ShowMessage("Debug", "Confirm");
                            //MyAPIGateway.Utilities.ShowMessage("Debug", block.GetOwnerFactionTag());
                            beacon.Radius = 500000f;
                            beacon.HudText = "HIGH VALUE TARGET";
                        }
                    }
                }
			} catch (Exception ex) 
            {
                Logging.Instance.WriteLine(string.Format("FlagAsHVTDeterminer Error: {0}", ex.ToString()));
			}
		}

        private static RadarObjectTypes GetEntityObjectType(IMyEntity entity)
        {
            if (entity is IMyVoxelMap)
                return RadarObjectTypes.Asteroid;

            if (entity is IMyCharacter)
            {
                return RadarObjectTypes.Astronaut;
            }

            if (entity is IMyCubeGrid && ((IMyCubeGrid)entity).IsStatic)
                return RadarObjectTypes.Station;

            return RadarObjectTypes.Ship;
        }

        private static RadarObjects GetEntityObjectSize(IMyEntity entity)
        {

            double entitySize = entity.PositionComp.WorldAABB.Size.AbsMax();

            foreach (RadarObjects item in m_radarObjectList)
            {
                if (entitySize >= item.Size)
                {
                    return item;
                }
            }

            return null;
        }
    }

    public class RadarObjects
    {
        public float MinimumDistance { get; set; }
        public float MaximumDistance { get; set; }
        public string SizeName { get; set; }
        public float Size { get; set; }
    }

    public class RadarFilter
    {
        public bool NoAsteroids { get; set; }
        public bool NoShips { get; set; }
        public bool NoStations { get; set; }
        public bool NoCharacters { get; set; }
        public bool NoHud { get; set; }
        public bool PassengerHud { get; set; }
        public bool OnlyPowered { get; set; }
        public bool SonarMode { get; set; }
        public float MinimumSize { get; set; }
        public float MinimumDistance { get; set; }
        public string OutputLCDName { get; set; }
        public string TriggerSoundName { get; set; }
    }

    public enum RadarObjectTypes
    {
        Astronaut,
        Asteroid,
        Station,
        Ship
    }

    public class RadarOutputItem
    {
        public IMyEntity Parent { get; set; }
        public string Name { get; set; }
    }
}
