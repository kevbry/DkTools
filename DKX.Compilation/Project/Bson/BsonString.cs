using Newtonsoft.Json;
using System;
using System.IO;

namespace DKX.Compilation.Project.Bson
{
    public class BsonString : BsonNode
    {
        private int _stringId;

        public BsonString(BsonFile file, string value)
            : base(file)
        {
            _stringId = file.AddString(value ?? throw new ArgumentNullException(nameof(value)));
        }

        public BsonString(BsonFile file, BinaryReader bin)
            : base(file)
        {
            _stringId = bin.ReadInt32();
        }

        protected override void WriteInner(BinaryWriter bin)
        {
            bin.Write(_stringId);
        }

        protected override NodeType NodeTypeId => NodeType.String;
        public string Value => File.GetString(_stringId);

        public override string ToString() => File.GetString(_stringId);

        public override void WriteJson(JsonWriter json)
        {
            json.WriteValue(File.GetString(_stringId));
        }
    }
}
