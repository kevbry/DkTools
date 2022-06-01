using Newtonsoft.Json;
using System.IO;

namespace DKX.Compilation.Project.Bson
{
    public class BsonInt32 : BsonNode
    {
        private int _value;

        public BsonInt32(BsonFile file, int value)
            : base(file)
        {
            _value = value;
        }

        public BsonInt32(BsonFile file, BinaryReader bin)
            : base(file)
        {
            _value = bin.ReadInt32();
        }

        protected override void WriteInner(BinaryWriter bin)
        {
            bin.Write(_value);
        }

        protected override NodeType NodeTypeId => NodeType.Int32;
        public int Value => _value;

        public override void WriteJson(JsonWriter json)
        {
            json.WriteValue(_value);
        }
    }
}
