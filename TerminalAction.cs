using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace NebLock
{
    public static class NebLockTerminalControls
    {
        static bool Done = false;

        public static void DoOnce()
        {
            if (Done)
                return;
            Done = true;

            var action = MyAPIGateway.TerminalControls.CreateAction<IMyCockpit>("NebLock_LockTarget");
            action.Name = new StringBuilder("Lock Radar Target");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Action = OnLockButtonPressed;
            action.Enabled = (block) => true;

            MyAPIGateway.TerminalControls.AddAction<IMyCockpit>(action);
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

                List<NebRadarAPI.API.NebRadarAPI.RadarEntry> entries = new List<NebRadarAPI.API.NebRadarAPI.RadarEntry>();
                
                NebRadarAPI.API.NebRadarAPI.GetAllRadarEntries(block.CubeGrid, entries);
                
                //MyAPIGateway.Utilities.ShowNotification($"Found {entries.Count} entries", 2000);
                /*
                bool foundLocked = false;
                foreach (var entry in entries)
                {
                    if (entry.IsLocked)
                    {
                        foundLocked = true;
                        MyAPIGateway.Utilities.ShowNotification($"Locked: {entry.Name}", 2000);
                        break;
                    }
                }
                if (!foundLocked)
                {
                    MyAPIGateway.Utilities.ShowNotification("No locked entry found", 2000);
                }
                */


            } catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("Spahget", e.Message);
                MyAPIGateway.Utilities.ShowMessage("Spahget", e.Source);
                MyAPIGateway.Utilities.ShowMessage("Spahget", e.StackTrace);
            }
        }
    }
}