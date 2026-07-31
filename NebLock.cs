using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;
using System;
using System.Collections.Generic;
using VRageMath;
using RadarEntry = NebRadarAPI.API.NebRadarAPI.RadarEntry;
using Sandbox.Game.Entities;
using System.Net.Security;

namespace NebLock
{
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation)]
    public class NebLock : MySessionComponentBase
    {
        static public Dictionary<IMyLargeTurretBase, RadarEntry> Tracks = new Dictionary<IMyLargeTurretBase, RadarEntry>();

        public override void LoadData()
        {
            NebRadarAPI.API.NebRadarAPI.Load(OnRadarAPIReady);
            NebLockTerminalControls.DoOnce();
        }
        private void OnRadarAPIReady()
        {
            MyAPIGateway.Utilities.ShowNotification("NebRadar API connected", 2000);
        }

        public override void Simulate()
        {
            try
            {
                if (Tracks.Count > 0)
                {
                    foreach (var track in Tracks)
                    {
                        var e = MyAPIGateway.Entities.GetEntityById(track.Value.MainGridEntityId) as MyCubeGrid;
                        if (e == null || !NebRadarAPI.API.NebRadarAPI.CanSee(track.Key.CubeGrid, e))
                        {
                            Tracks.Remove(track.Key);
                            MyAPIGateway.Utilities.ShowNotification("Radar Track Lost!", 2000);
                            continue;
                        }
                        //todo: add errors
                        var pos = e.Physics.CenterOfMassWorld;
                        var vel = e.Physics?.LinearVelocity ?? Vector3.Zero;
                        track.Key.TrackTarget(pos , vel);
                    }
                }
            } catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("NebLock Error Logged!", 2000);
                MyLog.Default.WriteLineAndConsole(e.ToString());
            }
        }

        protected override void UnloadData()
        {
            NebLockTerminalControls.RadarEntries = null;
            Tracks = null;
        }
    }
}