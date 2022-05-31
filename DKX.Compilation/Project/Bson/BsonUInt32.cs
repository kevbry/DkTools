using System.IO;

namespace DKX.Compilation.Project.Bson
{
    public class BsonUInt32 : BsonNode
    {
        private uint _value;

        public BsonUInt32(BsonFile file, uint value)
            : base(file)
        {
            _value = value;
        }

        public BsonUInt32(BsonFile file, BinaryReader bin)
            : base(file)
        {
            _value = bin.ReadUInt32();
        }

        protected override void WriteInner(BinaryWriter bin)
        {
            bin.Write(_value);
        }

        protected override NodeType NodeTypeId => NodeType.UInt32;
        public uint Value => _value;
    }
}
