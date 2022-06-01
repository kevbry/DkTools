using Newtonsoft.Json;
using System;
using System.IO;

namespace DKX.Compilation.Project.Bson
{
    public class BsonEnum : BsonNode
    {
        private int _value;

        public BsonEnum(BsonFile file, object value)
            : base(file)
        {
            _value = Convert.ToInt32(value);
        }

        public BsonEnum(BsonFile file, BinaryReader bin)
            : base(file)
        {
            _value = bin.ReadInt32();
        }

        protected override void WriteInner(BinaryWriter bin)
        {
            bin.Write(_value);
        }

        protected override NodeType NodeTypeId => NodeType.Enum;

        public T GetValue<T>() where T : Enum
        {
            if (!Enum.IsDefined(typeof(T), _value)) throw new InvalidBsonDataException($"Cannot convert integer '{_value}' to enum type '{typeof(T)}'");
            return (T)(object)_value;
        }

        public override void WriteJson(JsonWriter json)
        {
            json.WriteValue(_value.ToString());
        }
    }
}
