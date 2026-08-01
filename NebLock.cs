using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;
using System;
using System.Collections.Generic;
using VRageMath;
using RadarEntry = NebRadarAPI.API.NebRadarAPI.RadarEntry;
using Sandbox.Game.Entities;
using Digi.NetworkLib;

namespace NebLock
{
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation)]
    public class NebLock : MySessionComponentBase
    {
        public static NebLock I;

        private bool mpActive;
        private bool server;
        private bool client;

        public const ushort NetworkID = (ushort)(3775342659 % ushort.MaxValue);
        public Network Net;
        public PacketTurretTrack PacketTurretTrack;

        private List<RadarEntry> radarEntries = new List<RadarEntry>();
        private Dictionary<IMyLargeTurretBase, RadarEntry> turretTracks = new Dictionary<IMyLargeTurretBase, RadarEntry>();
        private List<IMyLargeTurretBase> deadTracks = new List<IMyLargeTurretBase>();

        public NebLock()
        {
            I = this;
        }
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
                MyEntities.OnEntityCreate += TerminalActions.I.AddActions;
            }
            //shared
            Net = new Network(NetworkID, ModContext.ModName);
            Net.ExceptionHandler = (e) => { //MyAPIGateway.Utilities.ShowNotification("NebLock Networking Exception!", 5000);
                MyLog.Default.WriteLineAndConsole($"NebLock Networking Exception!\n{e.Message}\n{e.TargetSite}\n{e.StackTrace}");
            };
            Net.ErrorHandler = (e) => { //MyAPIGateway.Utilities.ShowNotification("NebLock Networking Error!", 5000);
                MyLog.Default.WriteLineAndConsole($"NebLock Networking Error!\n{e}");
            };

            Net.SerializeTest = true;

            PacketTurretTrack = new PacketTurretTrack();
            PacketTurretTrack.OnReceive += PacketTurretTrack_OnReceive;
        }
        private void OnRadarAPIReady()
        {
            MyAPIGateway.Utilities.ShowNotification("NebRadar API connected", 2000);
        }
        private void PacketTurretTrack_OnReceive(PacketTurretTrack packet, ref PacketInfo packetInfo, ulong senderSteamId)
        {
            try
            {
                //get turret from network packet
                var turret = MyAPIGateway.Entities.GetEntityById(packet.TurretId) as IMyLargeTurretBase;
                if (turret == null) 
                { 
                    //MyAPIGateway.Utilities.ShowNotification($"Turret not found serverside", 2000);
                    return; 
                }
                
                //if true, locking target, if false, unlocking target.
                if (packet.Locking)
                {
                    List<IMyFunctionalBlock> radarBlocks = new List<IMyFunctionalBlock>();
                    NebRadarAPI.API.NebRadarAPI.GetAllRadarBlocks(turret.CubeGrid, radarBlocks);
                    if (radarBlocks.Count == 0) 
                    { 
                        //MyAPIGateway.Utilities.ShowNotification("No radars found.", 2000);
                        return;
                    }

                    radarEntries.Clear();
                    NebRadarAPI.API.NebRadarAPI.GetAllRadarEntries(turret.CubeGrid, radarEntries);

                    //MyAPIGateway.Utilities.ShowNotification($"Found {radarEntries.Count} entries", 2000);

                    RadarEntry target = default(RadarEntry);
                    foreach (var entry in radarEntries)
                    {
                        if (entry.IsLocked)
                        {
                            target = entry;
                            //MyAPIGateway.Utilities.ShowNotification($"Locked: {entry.Name}", 2000);
                            break;
                        }
                    }
                    if (!target.IsLocked)
                    {
                        //MyAPIGateway.Utilities.ShowNotification("No locked entry found", 2000); return;
                    }
                    turretTracks[turret] = target;
                    //MyAPIGateway.Utilities.ShowNotification($"Tracks Count: {turretTracks.Count}", 2000);
                } else {
                    turretTracks.Remove(turret);
                    //MyAPIGateway.Utilities.ShowNotification($"Track Removed, Count: {turretTracks.Count}", 2000);
                }
            }
            catch (Exception e)
            {
                //MyAPIGateway.Utilities.ShowNotification("NebLock Error Logged on Action!", 5000);
                //MyAPIGateway.Utilities.ShowNotification(e.Message, 5000);
                MyLog.Default.WriteLineAndConsole($"NebLock Error Logged on Action!\n{e.Message}\n{e.TargetSite}\n{e.StackTrace}");
                MyLog.Default.WriteLineAndConsole(e.ToString());
            }
        }
        public override void Simulate()
        {
            try
            {
                if (turretTracks.Count > 0 && server)
                {
                    deadTracks.Clear();
                    foreach (var track in turretTracks)
                    {
                        var turret = track.Key;
                        var e = MyAPIGateway.Entities.GetEntityById(track.Value.MainGridEntityId) as MyCubeGrid;
                        if (e == null || !NebRadarAPI.API.NebRadarAPI.CanSee(turret.CubeGrid, e))
                        {
                            deadTracks.Add(turret);
                            //MyAPIGateway.Utilities.ShowNotification("Radar Track Lost!", 2000);
                            continue;
                        }
                        //todo: add errors
                        var pos = e.Physics?.CenterOfMassWorld ?? track.Value.Position;
                        var vel = e.Physics?.LinearVelocity ?? Vector3.Zero;
                        track.Key.TrackTarget(pos , vel);
                    }
                    //remove dead tracks
                    foreach (var track in deadTracks) { turretTracks.Remove(track); }
                }
            } catch (Exception e)
            {
                //MyAPIGateway.Utilities.ShowNotification("NebLock Error Logged on Session Serverside!", 5000);
                //MyAPIGateway.Utilities.ShowNotification(e.Message, 5000);
                MyLog.Default.WriteLineAndConsole($"NebLock Error Logged on Serverside!\n{e.Message}\n{e.TargetSite}\n{e.StackTrace}");
            }
        }

        protected override void UnloadData()
        {
            radarEntries = null;
            turretTracks = null;
            deadTracks = null;
            Net?.Dispose();
            Net = null;

            PacketTurretTrack.OnReceive -= PacketTurretTrack_OnReceive;
        }
    }
}