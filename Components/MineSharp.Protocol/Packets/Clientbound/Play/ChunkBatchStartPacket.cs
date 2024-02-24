using MineSharp.Core.Common;
using MineSharp.Data;
using MineSharp.Data.Protocol;

namespace MineSharp.Protocol.Packets.Clientbound.Play;

/// <summary>
/// The ChunkBatchStart Packet, used since 1.20.2
/// https://wiki.vg/Protocol#Chunk_Batch_Start
/// </summary>
public class ChunkBatchStartPacket : IPacket
{
    /// <inheritdoc />
    public PacketType Type => PacketType.CB_Play_ChunkBatchStart;

    /// <inheritdoc />
    public void Write(PacketBuffer buffer, MinecraftData version)
    { }

    /// <inheritdoc />
    public static IPacket Read(PacketBuffer buffer, MinecraftData version)
        => new ChunkBatchStartPacket();
}
