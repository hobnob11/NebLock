using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;
using System;
using System.Collections.Generic;
using VRageMath;

namespace NebLock
{
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation)]
    public class NebLockSession : MySessionComponentBase
    {
        static public Dictionary<IMyLargeTurretBase, NebRadarAPI.API.NebRadarAPI.RadarEntry> Tracks = new Dictionary<IMyLargeTurretBase, NebRadarAPI.API.NebRadarAPI.RadarEntry>();

        public override void LoadData()
        {
            NebRadarAPI.API.NebRadarAPI.Load(OnRadarAPIReady);
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
                        var e = MyAPIGateway.Entities.GetEntityById(track.Value.MainGridEntityId);
                        var vel = e.Physics?.LinearVelocity ?? Vector3.Zero;
                        track.Key.TrackTarget(e.GetPosition(), vel);
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
        }
    }
}