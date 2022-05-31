using System.IO;

namespace DKX.Compilation.Project.Bson
{
    public class BsonUInt16 : BsonNode
    {
        private ushort _value;

        public BsonUInt16(BsonFile file, ushort value)
            : base(file)
        {
            _value = value;
        }

        public BsonUInt16(BsonFile file, BinaryReader bin)
            : base(file)
        {
            _value = bin.ReadUInt16();
        }

        protected override void WriteInner(BinaryWriter bin)
        {
            bin.Write(_value);
        }

        protected override NodeType NodeTypeId => NodeType.UInt16;
        public ushort Value => _value;
    }
}
