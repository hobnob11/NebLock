using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game;

namespace NebLock
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
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

        protected override void UnloadData()
        {
            NebLockTerminalControls.RadarEntries = null;
        }
    }
}