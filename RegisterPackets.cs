using NebLock;
using ProtoBuf;

namespace Digi.NetworkLib
{
    [ProtoInclude(10, typeof(PacketTurretTrack))]
    public abstract partial class PacketBase
    {
    }
}