using DKX.Compilation.DataTypes;
using System.IO;

namespace DKX.Compilation.Project.Bson
{
    public class BsonDataType : BsonNode
    {
        private DataType _dataType;

        public BsonDataType(BsonFile file, DataType dataType)
            : base(file)
        {
            _dataType = dataType;
        }

        public BsonDataType(BsonFile file, BinaryReader bin)
            : base(file)
        {
            _dataType = DataType.Deserialize(bin);
        }

        protected override void WriteInner(BinaryWriter bin)
        {
            _dataType.Serialize(bin);
        }

        public DataType DataType => _dataType;
        protected override NodeType NodeTypeId => NodeType.DataType;

        public override string ToString() => _dataType.ToString();
    }
}
