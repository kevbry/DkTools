using Newtonsoft.Json;
using System;
using System.IO;

namespace DKX.Compilation.Project.Bson
{
    public class BsonDateTime : BsonNode
    {
        private DateTime _dateTime;

        public BsonDateTime(BsonFile bson, DateTime dateTime)
            : base(bson)
        {
            _dateTime = dateTime;
        }

        public BsonDateTime(BsonFile bson, BinaryReader bin)
            : base(bson)
        {
            _dateTime = new DateTime(bin.ReadInt64());
        }

        protected override void WriteInner(BinaryWriter bin)
        {
            bin.Write(_dateTime.Ticks);
        }

        protected override NodeType NodeTypeId => NodeType.DateTime;
        public DateTime Value => _dateTime;

        public override string ToString() => _dateTime.ToString();

        public override void WriteJson(JsonWriter json)
        {
            json.WriteValue(_dateTime);
        }
    }
}
