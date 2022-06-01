using Newtonsoft.Json;
using System;
using System.IO;

namespace DKX.Compilation.Project.Bson
{
    public abstract class BsonNode : IBsonCreator
    {
        protected enum NodeType : byte
        {
            Object = 1,
            Array = 2,
            String = 3,
            Boolean = 4,
            Int16 = 5,
            UInt16 = 6,
            Int32 = 7,
            UInt32 = 8,
            Decimal = 9,
            Enum = 10,
            DateTime = 11,

            DataType = 32,
            Span = 33
        }

        private static Type GetNodeType(NodeType typeId)
        {
            switch (typeId)
            {
                case NodeType.Object: return typeof(BsonObject);
                case NodeType.Array: return typeof(BsonArray);
                case NodeType.String: return typeof(BsonString);
                case NodeType.Boolean: return typeof(BsonBoolean);
                case NodeType.Int16: return typeof(BsonInt16);
                case NodeType.UInt16: return typeof(BsonUInt16);
                case NodeType.Int32: return typeof(BsonInt32);
                case NodeType.UInt32: return typeof(BsonUInt32);
                case NodeType.Decimal: return typeof(BsonDecimal);
                case NodeType.Enum: return typeof(BsonEnum);
                case NodeType.DateTime: return typeof(BsonDateTime);
                case NodeType.DataType: return typeof(BsonDataType);
                case NodeType.Span: return typeof(BsonSpan);
                default: throw new InvalidBsonTypeException();
            }
        }

        protected abstract NodeType NodeTypeId { get; }

        private BsonFile _file;

        public BsonNode(BsonFile file)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
        }

        public BsonFile File => _file;

        public void Write(BinaryWriter bin)
        {
            bin.Write((byte)NodeTypeId);
            WriteInner(bin);
        }

        protected abstract void WriteInner(BinaryWriter bin);

        public static BsonNode Read(BsonFile file, BinaryReader bin)
        {
            var id = bin.ReadByte();
            if (!Enum.IsDefined(typeof(NodeType), id)) throw new InvalidBsonTypeException();

            return (BsonNode)Activator.CreateInstance(GetNodeType((NodeType)id), new object[] { file, bin });
        }

        public abstract void WriteJson(JsonWriter json);
    }
}
