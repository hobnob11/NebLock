using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace NebLock
{
    public static class NebLockTerminalControls
    {
        static bool Done = false;

        static public List<NebRadarAPI.API.NebRadarAPI.RadarEntry> RadarEntries = new List<NebRadarAPI.API.NebRadarAPI.RadarEntry>();

        public static void DoOnce()
        {
            if (Done)
                return;
            Done = true;

            var action = MyAPIGateway.TerminalControls.CreateAction<Sandbox.ModAPI.IMyLargeTurretBase>("NebLock_LockTarget");
            action.Name = new StringBuilder("Focus on Locked Radar Target");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Action = OnLockButtonPressed;
            action.Enabled = (block) => true;

            MyAPIGateway.TerminalControls.AddAction<Sandbox.ModAPI.IMyLargeTurretBase>(action);
        }

        private static void OnLockButtonPressed(IMyTerminalBlock block)
        {
            try
            {
                List<IMyFunctionalBlock> radarBlocks = new List<IMyFunctionalBlock>();
                NebRadarAPI.API.NebRadarAPI.GetAllRadarBlocks(block.CubeGrid, radarBlocks);

                var firstRadar = radarBlocks.FirstOrDefault<IMyFunctionalBlock>();
                if (firstRadar == null)
                {
                    MyAPIGateway.Utilities.ShowNotification("No Radar Found", 2000); return;
                }
                RadarEntries.Clear();
                NebRadarAPI.API.NebRadarAPI.GetAllRadarEntries(block.CubeGrid, RadarEntries);
                
                MyAPIGateway.Utilities.ShowNotification($"Found {RadarEntries.Count} entries", 2000);

                NebRadarAPI.API.NebRadarAPI.RadarEntry target = default(NebRadarAPI.API.NebRadarAPI.RadarEntry);
                foreach (var entry in RadarEntries)
                {
                    if (entry.IsLocked)
                    {
                        target = entry;
                        MyAPIGateway.Utilities.ShowNotification($"Locked: {entry.Name}", 2000);
                        break;
                    }
                }
                if (!target.IsLocked)
                {
                    MyAPIGateway.Utilities.ShowNotification("No locked entry found", 2000); return;
                }

                var turret = block as IMyLargeTurretBase;
                turret.TrackTarget(target.Position, target.Velocity);

            } catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("Spahget", e.Message);
                MyAPIGateway.Utilities.ShowMessage("Spahget", e.Source);
                MyAPIGateway.Utilities.ShowMessage("Spahget", e.StackTrace);
            }
        }
    }
}