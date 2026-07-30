using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game;
using VRage.Utils;
using System;

namespace NebLock
{
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation)]
    public class NebLockSession : MySessionComponentBase
    {
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
                if (NebLockTerminalControls.TurretTargets.Count > 0)
                {
                    foreach (var turretTarget in NebLockTerminalControls.TurretTargets)
                    {
                        turretTarget.Key.TrackTarget(turretTarget.Value.Position, turretTarget.Value.Velocity);
                    }
                }
            } catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole(e.ToString());
            }
        }

        protected override void UnloadData()
        {
            NebLockTerminalControls.RadarEntries = null;
        }
    }
}