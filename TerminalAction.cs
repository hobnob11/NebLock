using System;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Utils;

namespace NebLock
{
    public class TerminalActions
    {
        public static TerminalActions I = new TerminalActions();
        public bool actionsAdded = false;        
        public void AddActions(MyEntity e)
        {
            if(e is IMyLargeTurretBase && !actionsAdded)
            {
                actionsAdded = true;

                var actionLock = MyAPIGateway.TerminalControls.CreateAction<IMyLargeTurretBase>("NebLock_LockTarget");
                actionLock.Name = new StringBuilder("Focus on Locked Radar Target");
                actionLock.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
                actionLock.Action = OnLockButtonPressed;
                actionLock.Enabled = (_) => true;
                MyAPIGateway.TerminalControls.AddAction<IMyLargeTurretBase>(actionLock);
                
                var actionUnlock = MyAPIGateway.TerminalControls.CreateAction<IMyLargeTurretBase>("NebLock_UnlockTarget");
                actionUnlock.Name = new StringBuilder("Stop Focusing on Radar Target");
                actionUnlock.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
                actionUnlock.Action = OnUnlockLockButtonPressed;
                actionUnlock.Enabled = (_) => true;
                MyAPIGateway.TerminalControls.AddAction<IMyLargeTurretBase>(actionUnlock);

                MyAPIGateway.Utilities.ShowNotification("NebLock Actions Added.", 5000);
            }
        }
        private void OnLockButtonPressed(IMyTerminalBlock block)
        {
            try
            {
                var turret = block as IMyLargeTurretBase;
                NebLock.I.PacketTurretTrack.Setup(turret.EntityId, true);
                NebLock.I.Net.SendToServer(NebLock.I.PacketTurretTrack);
                //MyAPIGateway.Utilities.ShowNotification($"Lock button pressed clientside", 2000);
            }
            catch (Exception e)
            {
                //MyAPIGateway.Utilities.ShowNotification("NebLock Error Logged on clientside lock", 5000);
                //MyAPIGateway.Utilities.ShowNotification(e.Message, 5000);
                MyLog.Default.WriteLineAndConsole($"NebLock Error Logged on clientside lock!\n{e.Message}\n{e.TargetSite}\n{e.StackTrace}");
                MyLog.Default.WriteLineAndConsole(e.ToString());
            }
        }
        private void OnUnlockLockButtonPressed(IMyTerminalBlock block)
        {
            try
            {
                var turret = block as IMyLargeTurretBase;
                NebLock.I.PacketTurretTrack.Setup(turret.EntityId, false);
                NebLock.I.Net.SendToServer(NebLock.I.PacketTurretTrack);
                //MyAPIGateway.Utilities.ShowNotification($"Unlock button pressed clientside", 2000);
            }
            catch (Exception e)
            {
                //MyAPIGateway.Utilities.ShowNotification("NebLock Error Logged on clientside unlock!", 5000);
                //MyAPIGateway.Utilities.ShowNotification(e.Message, 5000);
                MyLog.Default.WriteLineAndConsole($"NebLock Error Logged on clientside unlock!\n{e.Message}\n{e.TargetSite}\n{e.StackTrace}");
                MyLog.Default.WriteLineAndConsole(e.ToString());
            }
        }
    }
}