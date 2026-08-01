using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;
using System;
using System.Collections.Generic;
using VRageMath;
using RadarEntry = NebRadarAPI.API.NebRadarAPI.RadarEntry;
using Sandbox.Game.Entities;
using System.Net.Security;
using System.Linq;
using VRage.Game.Entity;
using Sandbox.Game.Screens.Helpers;
using System.Reflection;

namespace NebLock
{
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation)]
    public class NebLock : MySessionComponentBase
    {
        public static NebLock I = new NebLock();
        private bool mpActive;
        private bool server;
        private bool client;
        public Dictionary<IMyLargeTurretBase, RadarEntry> Tracks = new Dictionary<IMyLargeTurretBase, RadarEntry>();
        private List<IMyLargeTurretBase> deadTracks = new List<IMyLargeTurretBase>();
        public override void LoadData()
        {
            mpActive = MyAPIGateway.Multiplayer.MultiplayerActive;
            server = (mpActive && MyAPIGateway.Multiplayer.IsServer) || !mpActive;
            client = (mpActive && !MyAPIGateway.Utilities.IsDedicated) || !mpActive;

            if(server)
            {
                NebRadarAPI.API.NebRadarAPI.Load(OnRadarAPIReady);
            }
            if(client)
            {
                MyAPIGateway.TerminalControls.CustomActionGetter += TerminalActions.I.AddActions;
            }
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
                    deadTracks.Clear();
                    foreach (var track in Tracks)
                    {
                        var turret = track.Key;
                        var e = MyAPIGateway.Entities.GetEntityById(track.Value.MainGridEntityId) as MyCubeGrid;
                        if (e == null || !NebRadarAPI.API.NebRadarAPI.CanSee(turret.CubeGrid, e))
                        {
                            deadTracks.Add(turret);
                            MyAPIGateway.Utilities.ShowNotification("Radar Track Lost!", 2000);
                            continue;
                        }
                        //todo: add errors
                        var pos = e.Physics?.CenterOfMassWorld ?? track.Value.Position;
                        var vel = e.Physics?.LinearVelocity ?? Vector3.Zero;
                        track.Key.TrackTarget(pos , vel);
                    }
                    //remove dead tracks
                    foreach (var track in deadTracks) { Tracks.Remove(track); }
                }
            } catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("NebLock Error Logged!", 2000);
                MyLog.Default.WriteLineAndConsole(e.ToString());
            }
        }

        protected override void UnloadData()
        {
            TerminalActions.I.RadarEntries = null;
            Tracks = null;
            deadTracks = null;
        }
    }
}