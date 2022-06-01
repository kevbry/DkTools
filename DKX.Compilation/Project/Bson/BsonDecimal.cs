using Newtonsoft.Json;
using System.IO;

namespace DKX.Compilation.Project.Bson
{
    public class BsonDecimal : BsonNode
    {
        private decimal _value;

        public BsonDecimal(BsonFile file, decimal value)
            : base(file)
        {
            _value = value;
        }

        public BsonDecimal(BsonFile file, BinaryReader bin)
            : base(file)
        {
            _value = bin.ReadDecimal();
        }

        protected override void WriteInner(BinaryWriter bin)
        {
            bin.Write(_value);
        }

        protected override NodeType NodeTypeId => NodeType.Decimal;
        public decimal Value => _value;

        public override void WriteJson(JsonWriter json)
        {
            json.WriteValue(_value);
        }
    }
}
