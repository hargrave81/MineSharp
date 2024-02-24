using MineSharp.Core.Common;
using MineSharp.Data;
using MineSharp.Data.Protocol;

namespace MineSharp.Protocol.Packets.Clientbound.Play;
#pragma warning disable CS1591
public class KeepAlivePacket : IPacket
{
    public PacketType Type => PacketType.CB_Play_KeepAlive;

    public long KeepAliveId { get; set; }

    public KeepAlivePacket(long id)
    {
        this.KeepAliveId = id;
    }

    public void Write(PacketBuffer buffer, MinecraftData version)
    {
        buffer.WriteLong(this.KeepAliveId);
    }

    public static IPacket Read(PacketBuffer buffer, MinecraftData version)
    {
        var id = buffer.ReadLong();
        return new KeepAlivePacket(id);
    }
}
#pragma warning restore CS1591
