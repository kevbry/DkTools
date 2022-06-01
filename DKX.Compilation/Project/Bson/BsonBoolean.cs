using Newtonsoft.Json;
using System.IO;

namespace DKX.Compilation.Project.Bson
{
    public class BsonBoolean : BsonNode
    {
        private bool _value;

        public BsonBoolean(BsonFile file, bool value)
            : base(file)
        {
            _value = value;
        }

        public BsonBoolean(BsonFile file, BinaryReader bin)
            : base(file)
        {
            _value = bin.ReadBoolean();
        }

        protected override void WriteInner(BinaryWriter bin)
        {
            bin.Write(_value);
        }

        protected override NodeType NodeTypeId => NodeType.Boolean;
        public bool Value => _value;

        public override void WriteJson(JsonWriter json)
        {
            json.WriteValue(_value);
        }
    }
}
