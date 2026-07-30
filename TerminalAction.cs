using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;

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

            var actionLock = MyAPIGateway.TerminalControls.CreateAction<Sandbox.ModAPI.IMyLargeTurretBase>("NebLock_LockTarget");
            actionLock.Name = new StringBuilder("Focus on Locked Radar Target");
            actionLock.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            actionLock.Action = OnLockButtonPressed;
            actionLock.Enabled = (block) => true;

            MyAPIGateway.TerminalControls.AddAction<Sandbox.ModAPI.IMyLargeTurretBase>(actionLock);

            var actionUnlock = MyAPIGateway.TerminalControls.CreateAction<Sandbox.ModAPI.IMyLargeTurretBase>("NebLock_UnlockTarget");
            actionUnlock.Name = new StringBuilder("Stop Focusing on Radar Target");
            actionUnlock.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            actionUnlock.Action = OnUnlockLockButtonPressed;
            actionUnlock.Enabled = (block) => true;

            MyAPIGateway.TerminalControls.AddAction<Sandbox.ModAPI.IMyLargeTurretBase>(actionUnlock);
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
                NebLockSession.Tracks[turret] = target;
                MyAPIGateway.Utilities.ShowNotification($"Tracks Count: {NebLockSession.Tracks.Count}", 2000);

            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("NebLock Error Logged!", 2000);
                MyLog.Default.WriteLineAndConsole(e.ToString());
            }
        }
        private static void OnUnlockLockButtonPressed(IMyTerminalBlock block)
        {
            try
            {
                var turret = block as IMyLargeTurretBase;
                NebLockSession.Tracks.Remove(turret);
                MyAPIGateway.Utilities.ShowNotification($"Track Removed, Count: {NebLockSession.Tracks.Count}", 2000);
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("NebLock Error Logged!", 2000);
                MyLog.Default.WriteLineAndConsole(e.ToString());
            }
        }
    }
}