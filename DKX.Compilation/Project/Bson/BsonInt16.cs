using System.IO;

namespace DKX.Compilation.Project.Bson
{
    public class BsonInt16 : BsonNode
    {
        private short _value;

        public BsonInt16(BsonFile file, short value)
            : base(file)
        {
            _value = value;
        }

        public BsonInt16(BsonFile file, BinaryReader bin)
            : base(file)
        {
            _value = bin.ReadInt16();
        }

        protected override void WriteInner(BinaryWriter bin)
        {
            bin.Write(_value);
        }

        protected override NodeType NodeTypeId => NodeType.Int16;
        public short Value => _value;
    }
}
