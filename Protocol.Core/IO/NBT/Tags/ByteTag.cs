using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.IO.NBT.Tags
{
    public class ByteTag : NBTTag, INBTPayload<byte>
    {
        public override NBTTagType Type => NBTTagType.Byte;

        public byte Payload { get; set; }

        public override NBTTag Read(NBTReader reader)
        {
            if (!IsListItem)
                Name = reader.ReadString();
            Payload = reader.ReadByte();
            return this;
        }

        public override NBTTag Write(NBTWriter writer)
        {
            WriteHeader(writer);
            writer.WriteByte(Payload);
            return this;
        }
        public override string ToString() => Payload.ToString();
    }
}
