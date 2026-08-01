using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

using RadarEntry = NebRadarAPI.API.NebRadarAPI.RadarEntry;

namespace NebLock
{
    public class TerminalActions
    {
        public static TerminalActions I = new TerminalActions();
        public List<RadarEntry> RadarEntries = new List<RadarEntry>();
        public void AddActions(IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
            if(block is IMyLargeTurretBase)
            {
                var actionLock = MyAPIGateway.TerminalControls.CreateAction<IMyLargeTurretBase>("NebLock_LockTarget");
                actionLock.Name = new StringBuilder("Focus on Locked Radar Target");
                actionLock.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
                actionLock.Action = OnLockButtonPressed;
                actionLock.Enabled = (_) => true;
                actions.Add(actionLock);
                //MyAPIGateway.TerminalControls.AddAction<IMyLargeTurretBase>(actionLock);
                
                var actionUnlock = MyAPIGateway.TerminalControls.CreateAction<IMyLargeTurretBase>("NebLock_UnlockTarget");
                actionUnlock.Name = new StringBuilder("Stop Focusing on Radar Target");
                actionUnlock.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
                actionUnlock.Action = OnUnlockLockButtonPressed;
                actionUnlock.Enabled = (_) => true;
                actions.Add(actionUnlock);
                //MyAPIGateway.TerminalControls.AddAction<IMyLargeTurretBase>(actionUnlock);
            }
        }

        private void OnLockButtonPressed(IMyTerminalBlock block)
        {
            try
            {
                List<IMyFunctionalBlock> radarBlocks = new List<IMyFunctionalBlock>();
                NebRadarAPI.API.NebRadarAPI.GetAllRadarBlocks(block.CubeGrid, radarBlocks);

                if (radarBlocks.Count == 0) { MyAPIGateway.Utilities.ShowNotification("No radars found.", 2000); }

                I.RadarEntries.Clear();
                NebRadarAPI.API.NebRadarAPI.GetAllRadarEntries(block.CubeGrid, I.RadarEntries);

                MyAPIGateway.Utilities.ShowNotification($"Found {I.RadarEntries.Count} entries", 2000);

                RadarEntry target = default(RadarEntry);
                foreach (var entry in I.RadarEntries)
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
        private void OnUnlockLockButtonPressed(IMyTerminalBlock block)
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