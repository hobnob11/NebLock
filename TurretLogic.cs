using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace NebLock
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TurretBase), false)]
    public class TurretLogic : MyGameLogicComponent
    {
        private IMyLargeTurretBase block;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            block = (IMyLargeTurretBase)Entity;
        }

        public override void UpdateOnceBeforeFrame()
        {
            NebLockTerminalControls.DoOnce();
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            if (NebLockSession.Tracks.ContainsKey(block))
            {
                var targetGrid = MyAPIGateway.Entities.GetEntityById(NebLockSession.Tracks[block].MainGridEntityId) as MyCubeGrid;
                if (!NebRadarAPI.API.NebRadarAPI.CanSee(block.CubeGrid, targetGrid))
                {
                    NebLockSession.Tracks.Remove(block);
                    MyAPIGateway.Utilities.ShowNotification("Radar Track Lost!", 2000);
                }
            }
        }
    }
}