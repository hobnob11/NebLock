using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;

using RadarEntry = NebRadarAPI.API.NebRadarAPI.RadarEntry;

namespace NebLock
{
    public static class NebLockTerminalControls
    {
        static bool Done = false;

        static public List<RadarEntry> RadarEntries = new List<RadarEntry>();

        public static void DoOnce()
        {
            if (Done)
                return;
            Done = true;

            var actionLock = MyAPIGateway.TerminalControls.CreateAction<IMyLargeTurretBase>("NebLock_LockTarget");
            actionLock.Name = new StringBuilder("Focus on Locked Radar Target");
            actionLock.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            actionLock.Action = OnLockButtonPressed;
            actionLock.Enabled = (block) => true;

            MyAPIGateway.TerminalControls.AddAction<IMyLargeTurretBase>(actionLock);

            var actionUnlock = MyAPIGateway.TerminalControls.CreateAction<IMyLargeTurretBase>("NebLock_UnlockTarget");
            actionUnlock.Name = new StringBuilder("Stop Focusing on Radar Target");
            actionUnlock.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            actionUnlock.Action = OnUnlockLockButtonPressed;
            actionUnlock.Enabled = (block) => true;

            MyAPIGateway.TerminalControls.AddAction<IMyLargeTurretBase>(actionUnlock);
        }

        private static void OnLockButtonPressed(IMyTerminalBlock block)
        {
            try
            {
                List<IMyFunctionalBlock> radarBlocks = new List<IMyFunctionalBlock>();
                NebRadarAPI.API.NebRadarAPI.GetAllRadarBlocks(block.CubeGrid, radarBlocks);

                if (radarBlocks.Count == 0) { MyAPIGateway.Utilities.ShowNotification("No radars found.", 2000); }

                RadarEntries.Clear();
                NebRadarAPI.API.NebRadarAPI.GetAllRadarEntries(block.CubeGrid, RadarEntries);

                MyAPIGateway.Utilities.ShowNotification($"Found {RadarEntries.Count} entries", 2000);

                RadarEntry target = default(RadarEntry);
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
                NebLock.Tracks[turret] = target;
                MyAPIGateway.Utilities.ShowNotification($"Tracks Count: {NebLock.Tracks.Count}", 2000);

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
                NebLock.Tracks.Remove(turret);
                MyAPIGateway.Utilities.ShowNotification($"Track Removed, Count: {NebLock.Tracks.Count}", 2000);
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("NebLock Error Logged!", 2000);
                MyLog.Default.WriteLineAndConsole(e.ToString());
            }
        }
    }
}