using DKX.Compilation.DataTypes;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.Resolving;
using DKX.Compilation.Variables;

namespace DKX.Compilation.Project
{
    class ProjectArgument : IArgument
    {
        private string _name;
        private DataType _dataType;
        private ArgumentPassType _passType;

        public ProjectArgument(IArgument fileArgument)
        {
            _name = fileArgument.Name;
            _dataType = fileArgument.DataType;
            _passType = fileArgument.PassType;
        }

        private ProjectArgument(string name, DataType dataType, ArgumentPassType passType)
        {
            _name = name;
            _dataType = dataType;
            _passType = passType;
        }

        public DataType DataType => _dataType;
        public string Name => _name;
        public ArgumentPassType PassType => _passType;

        public override string ToString() => $"ProjectArgument: {_dataType} {_name}";

        public BsonNode ToBson(BsonFile bson)
        {
            var obj = new BsonObject(bson);

            obj.SetDataType("DataType", _dataType);
            obj.SetString("Name", _name);
            obj.SetEnum("PassType", _passType);

            return obj;
        }

        public static ProjectArgument FromBson(BsonNode node)
        {
            if (!(node is BsonObject obj)) throw new InvalidBsonDataException("Argument node is not an object.");

            var dataType = obj.GetDataType("DataType");
            var name = obj.GetString("Name");
            var passType = obj.GetEnum<ArgumentPassType>("PassType");

            return new ProjectArgument(name, dataType, passType);
        }
    }
}
