using fNbt;
using MineSharp.Core.Common.Blocks;
using System.Text;
using System.Text.RegularExpressions;

namespace MineSharp.Core.Common;

/// <summary>
/// Read and write values from and to a byte buffer.
/// </summary>
public class PacketBuffer : IDisposable, IAsyncDisposable
{
    private readonly MemoryStream _buffer;

    /// <summary>
    /// The total size of the buffer
    /// </summary>
    public long Size => _buffer.Length;

    /// <summary>
    /// The number of readable bytes in the buffer
    /// </summary>
    public long ReadableBytes => _buffer.Length - _buffer.Position;

    /// <summary>
    /// The position in the buffer.
    /// </summary>
    public long Position => _buffer.Position;

    /// <summary>
    /// Whether to use anonymous nbt compounds. This is used since
    /// Minecraft Java 1.20.2
    /// </summary>
    public bool UseAnonymousNbt { get; set; } = false;

    /// <summary>
    /// Create a new empty, writeable PacketBuffer
    /// </summary>
    /// <param name="useAnonymousNbt"></param>
    public PacketBuffer(bool useAnonymousNbt)
    {
        this._buffer         = new MemoryStream();
        this.UseAnonymousNbt = useAnonymousNbt;
    }

    /// <summary>
    /// Create a new readable PacketBuffer with <paramref name="bytes"/> as input.
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="useAnonymousNbt"></param>
    public PacketBuffer(byte[] bytes, bool useAnonymousNbt)
    {
        this._buffer         = new MemoryStream(bytes);
        this.UseAnonymousNbt = useAnonymousNbt;
    }

    /// <summary>
    /// Return the buffer's byte array
    /// </summary>
    /// <returns></returns>
    public byte[] GetBuffer()
    {
        return this._buffer.ToArray();
    }

    /// <summary>
    /// Returns the buffer as hex values as a string
    /// </summary>
    /// <param name="cutToPosition"></param>
    /// <returns></returns>
    public string HexDump(bool cutToPosition = false)
    {
        var hex = Convert.ToHexString(this.GetBuffer().Skip(cutToPosition ? (int)this.Position : 0).ToArray());
        return Regex.Replace(hex, ".{2}", "$0 ").TrimEnd();
    }

    private void EnsureEnoughReadableBytes(int count)
    {
        if (this.ReadableBytes < count)
        {
            throw new EndOfStreamException();
        }
    }

#pragma warning disable CS1591

    #region Reading

    public int ReadBytes(Span<byte> bytes)
    {
        EnsureEnoughReadableBytes(bytes.Length);

        return this._buffer.Read(bytes);
    }

    public byte[] ReadBytes(int count)
    {
        EnsureEnoughReadableBytes(count);

        int    read  = 0;
        byte[] bytes = new byte[count];
        while (read < count)
            read += this._buffer.Read(bytes, read, count - read);
        return bytes;
    }

    public byte Peek()
    {
        EnsureEnoughReadableBytes(1);

        byte peek = (byte)this._buffer.ReadByte();
        this._buffer.Position -= 1;
        return peek;
    }

    public byte ReadByte()
    {
        EnsureEnoughReadableBytes(1);

        return (byte)this._buffer.ReadByte();
    }

    public sbyte ReadSByte()
        => (sbyte)this.ReadByte();

    public bool ReadBool()
    {
        return this.ReadByte() == 1;
    }

    public ushort ReadUShort()
    {
        EnsureEnoughReadableBytes(2);

        int b0 = this._buffer.ReadByte();
        int b1 = this._buffer.ReadByte();

        if (BitConverter.IsLittleEndian)
            return (ushort)(b0 << 8 | b1);

        return (ushort)(b0 | b1 << 8);
    }

    public short ReadShort() =>
        (short)this.ReadUShort();

    public uint ReadUInt()
    {
        EnsureEnoughReadableBytes(4);

        int b0 = this._buffer.ReadByte();
        int b1 = this._buffer.ReadByte();
        int b2 = this._buffer.ReadByte();
        int b3 = this._buffer.ReadByte();

        if (BitConverter.IsLittleEndian)
            return (uint)(b0 << 24 | b1 << 16 | b2 << 8 | b3);

        return (uint)(b0 | b1 << 8 | b2 << 16 | b3 << 24);
    }

    public int ReadInt()
        => (int)this.ReadUInt();

    public ulong ReadULong()
    {
        EnsureEnoughReadableBytes(8);

        long b0 = this._buffer.ReadByte();
        long b1 = this._buffer.ReadByte();
        long b2 = this._buffer.ReadByte();
        long b3 = this._buffer.ReadByte();
        long b4 = this._buffer.ReadByte();
        long b5 = this._buffer.ReadByte();
        long b6 = this._buffer.ReadByte();
        long b7 = this._buffer.ReadByte();

        if (BitConverter.IsLittleEndian)
            return (ulong)(b0 << 56 | b1 << 48 | b2 << 40 | b3 << 32 | b4 << 24 | b5 << 16 | b6 << 8 | b7);

        return (ulong)(b0 | b1 << 8 | b2 << 16 | b3 << 24 | b4 << 32 | b5 << 40 | b6 << 48 | b7 << 56);
    }

    public long ReadLong()
        => (long)this.ReadULong();

    public float ReadFloat()
    {
        EnsureEnoughReadableBytes(4);

        Span<byte> bytes = stackalloc byte[sizeof(float)];
        this.ReadBytes(bytes);

        if (BitConverter.IsLittleEndian)
            bytes.Reverse();

        return BitConverter.ToSingle(bytes);
    }

    public double ReadDouble()
    {
        EnsureEnoughReadableBytes(8);

        Span<byte> bytes = stackalloc byte[sizeof(double)];
        this.ReadBytes(bytes);

        if (BitConverter.IsLittleEndian)
            bytes.Reverse();

        return BitConverter.ToDouble(bytes);
    }

    public int ReadVarInt()
    {
        int value = 0;
        int shift = 0;

        while (true)
        {
            byte b = this.ReadByte();
            value |= ((b & (int)0x7f) << shift);
            if ((b & 0x80) == 0x00)
                break;

            shift += 7;
            if (shift >= 32)
                throw new Exception("varint is too big");
        }

        return value;
    }

    public long ReadVarLong()
    {
        long value = 0;
        int  shift = 0;

        while (true)
        {
            byte b = this.ReadByte();
            value |= ((b & (long)0x7f) << shift);
            if ((b & 0x80) == 0x00)
                break;

            shift += 7;
            if (shift >= 64) throw new Exception("varlong is too big");
        }

        return value;
    }

    public string ReadString(Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        int        length = this.ReadVarInt();
        Span<byte> bytes  = stackalloc byte[length];
        this.ReadBytes(bytes);

        return encoding.GetString(bytes);
    }

    public UUID ReadUuid()
    {
        long l1 = this.ReadLong();
        long l2 = this.ReadLong();
        return new UUID(l1, l2);
    }

    public T[] ReadVarIntArray<T>(Func<PacketBuffer, T> reader)
    {
        T[] array = new T[this.ReadVarInt()];

        for (int i = 0; i < array.Length; i++)
        {
            array[i] = reader(this);
        }

        return array;
    }

    public byte[] RestBuffer()
    {
        byte[] bytes = new byte[this.ReadableBytes];
        this._buffer.Read(bytes, 0, bytes.Length);
        return bytes;
    }

    public NbtCompound? ReadNbtCompound()
    {
        return (NbtCompound?)this.ReadNbt();
    }

    public NbtTag? ReadNbt()
    {
        var t = (NbtTagType)this.ReadByte();
        if (t == NbtTagType.End)
            return null;
        _buffer.Position--;

        var file = new NbtFile() { BigEndian = true, Anonymous = this.UseAnonymousNbt };

        file.LoadFromStream(_buffer, NbtCompression.None);

        return file.RootTag;
    }

    public BlockEntity ReadBlockEntity()
    {
        var packedXZ = this.ReadByte();
        var x        = (byte)(packedXZ >> 4 & 0xF);
        var z        = (byte)(packedXZ      & 0xF);
        var y        = this.ReadShort();
        var type     = this.ReadVarInt();
        var nbt      = this.ReadNbtCompound();

        return new BlockEntity(x, y, z, type, nbt);
    }

    public T? ReadOptional<T>() where T : class
    {
        var available = this.ReadBool();
        if (!available)
            return null;

        return Read<T>();
    }

    public T? ReadOptional<T>(bool _ = false) where T : struct
    {
        var available = this.ReadBool();
        if (!available)
            return null;

        return Read<T>();
    }


    public T Read<T>()
    {
        var type = Type.GetTypeCode(typeof(T));

        object value = type switch
        {
            TypeCode.Boolean => this.ReadBool(),
            TypeCode.SByte   => this.ReadSByte(),
            TypeCode.Byte    => this.ReadByte(),
            TypeCode.Int16   => this.ReadShort(),
            TypeCode.Int32   => this.ReadInt(),
            TypeCode.Int64   => this.ReadLong(),

            TypeCode.UInt16 => this.ReadUShort(),
            TypeCode.UInt32 => this.ReadUInt(),
            TypeCode.UInt64 => this.ReadULong(),

            TypeCode.String => this.ReadString(),
            TypeCode.Single => this.ReadFloat(),
            TypeCode.Double => this.ReadBool(),
            _               => throw new NotSupportedException()
        };
        return (T)value;
    }

    #endregion


    #region Writing

    public void WriteBytes(Span<byte> bytes)
    {
        this._buffer.Write(bytes);
    }

    public void WriteBool(bool value)
    {
        this.WriteByte((byte)(value ? 1 : 0));
    }

    public void WriteByte(byte b)
        => this._buffer.WriteByte(b);

    public void WriteSByte(sbyte b) => this.WriteByte((byte)b);

    public void WriteUShort(ushort value)
    {
        if (BitConverter.IsLittleEndian)
        {
            this.WriteByte((byte)(value >> 8 & 0xFF));
            this.WriteByte((byte)(value      & 0xFF));
            return;
        }

        this.WriteByte((byte)(value      & 0xFF));
        this.WriteByte((byte)(value >> 8 & 0xFF));
    }

    public void WriteShort(short value) => this.WriteUShort((ushort)value);

    public void WriteUInt(uint value)
    {
        if (BitConverter.IsLittleEndian)
        {
            this.WriteByte((byte)(value >> 24 & 0xFF));
            this.WriteByte((byte)(value >> 16 & 0xFF));
            this.WriteByte((byte)(value >> 8  & 0xFF));
            this.WriteByte((byte)(value       & 0xFF));
            return;
        }

        this.WriteByte((byte)(value       & 0xFF));
        this.WriteByte((byte)(value >> 8  & 0xFF));
        this.WriteByte((byte)(value >> 16 & 0xFF));
        this.WriteByte((byte)(value >> 24 & 0xFF));
    }

    public void WriteInt(int value) => this.WriteUInt((uint)value);

    public void WriteULong(ulong value)
    {
        if (BitConverter.IsLittleEndian)
        {
            this.WriteByte((byte)(value >> 56 & 0xFF));
            this.WriteByte((byte)(value >> 48 & 0xFF));
            this.WriteByte((byte)(value >> 40 & 0xFF));
            this.WriteByte((byte)(value >> 32 & 0xFF));
            this.WriteByte((byte)(value >> 24 & 0xFF));
            this.WriteByte((byte)(value >> 16 & 0xFF));
            this.WriteByte((byte)(value >> 8  & 0xFF));
            this.WriteByte((byte)(value       & 0xFF));
            return;
        }

        this.WriteByte((byte)(value       & 0xFF));
        this.WriteByte((byte)(value >> 8  & 0xFF));
        this.WriteByte((byte)(value >> 16 & 0xFF));
        this.WriteByte((byte)(value >> 24 & 0xFF));
        this.WriteByte((byte)(value >> 32 & 0xFF));
        this.WriteByte((byte)(value >> 40 & 0xFF));
        this.WriteByte((byte)(value >> 48 & 0xFF));
        this.WriteByte((byte)(value >> 56 & 0xFF));
    }

    public void WriteLong(long value) => this.WriteULong((ulong)value);

    public void WriteFloat(float value)
    {
        uint val = BitConverter.SingleToUInt32Bits(value);
        this.WriteUInt(val);
    }

    public void WriteDouble(double value)
    {
        ulong val = BitConverter.DoubleToUInt64Bits(value);
        this.WriteULong(val);
    }

    public void WriteVarInt(int value)
    {
        while (true)
        {
            if ((value & ~0x7F) == 0)
            {
                this._buffer.WriteByte((byte)value);
                return;
            }

            this._buffer.WriteByte((byte)(value & 0x7F | 0x80));
            value >>= 7;
        }
    }

    public void WriteVarLong(int value)
    {
        while ((value & ~0x7F) != 0x00)
        {
            this._buffer.WriteByte((byte)((value & 0xFF) | 0x80));
            value >>= 7;
        }

        this._buffer.WriteByte((byte)value);
    }

    public void WriteString(string value, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        Span<byte> bytes  = stackalloc byte[encoding.GetByteCount(value)];
        int        length = encoding.GetBytes(value, bytes);

        this.WriteVarInt(length);
        this.WriteBytes(bytes);
    }

    public void WriteUuid(UUID value)
    {
        this.WriteLong(value.MostSignificantBits);
        this.WriteLong(value.LeastSignificantBits);
    }

    public void WriteVarIntArray<T>(ICollection<T> collection, Action<PacketBuffer, T> writer)
    {
        this.WriteVarInt(collection.Count);

        foreach (var element in collection)
        {
            writer(this, element);
        }
    }

    public void WriteNbt(NbtTag? tag)
    {
        if (tag is null or NbtCompound { Count: 0 })
        {
            this._buffer.WriteByte((byte)NbtTagType.End);
            return;
        }

        NbtFile f = new NbtFile(tag) { BigEndian = true, Anonymous = this.UseAnonymousNbt };
        f.SaveToStream(_buffer, NbtCompression.None);
    }

    public void WriteBlockEntity(BlockEntity entity)
    {
        var packedXZ = (byte)(entity.X << 4 & 0xF | entity.Z & 0xF);
        this.WriteByte(packedXZ);
        this.WriteShort(entity.Y);
        this.WriteVarInt(entity.Type);
        this.WriteNbt(entity.Data);
    }

    #endregion

#pragma warning restore CS1591

    /// <summary>
    /// Disposes the underlying <see cref="MemoryStream"/>
    /// </summary>
    public void Dispose()
    {
        _buffer.Dispose();
    }

    /// <summary>
    /// Disposes the underlying <see cref="MemoryStream"/>
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _buffer.DisposeAsync();
    }
}
