using Digi.NetworkLib;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NebLock
{
    [ProtoContract]
    public class PacketTurretTrack : PacketBase
    {
        public PacketTurretTrack() { }

        [ProtoMember(1)]
        public long TurretId;

        [ProtoMember(2)]
        public bool Locking; //true if lock called, false if unlock called.

        public void Setup(long turretId, bool locking)
        {
            TurretId = turretId;
            Locking = locking;
        }

        public static event ReceiveDelegate<PacketTurretTrack> OnReceive;

        public override void Received(ref PacketInfo packetInfo, ulong senderSteamId)
        {
            OnReceive?.Invoke(this, ref packetInfo, senderSteamId);
        }

    }
}
