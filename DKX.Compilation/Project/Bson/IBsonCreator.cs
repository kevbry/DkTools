using DKX.Compilation.DataTypes;
using System;

namespace DKX.Compilation.Project.Bson
{
    public interface IBsonCreator
    {
        BsonFile File { get; }
    }

    public static class BsonCreatorHelper
    {
        public static BsonObject CreateObject(this IBsonCreator bson) => new BsonObject(bson.File);

        public static BsonArray CreateArray(this IBsonCreator bson) => new BsonArray(bson.File);

        public static BsonString CreateString(this IBsonCreator bson, string value) => new BsonString(bson.File, value);

        public static BsonDateTime CreateDateTime(this IBsonCreator bson, DateTime value) => new BsonDateTime(bson.File, value);

        public static BsonDataType CreateDataType(this IBsonCreator bson, DataType value) => new BsonDataType(bson.File, value);

        public static BsonEnum CreateEnum(this IBsonCreator bson, object value) => new BsonEnum(bson.File, value);

        public static BsonBoolean CreateBoolean(this IBsonCreator bson, bool value) => new BsonBoolean(bson.File, value);

        public static BsonInt16 CreateInt16(this IBsonCreator bson, short value) => new BsonInt16(bson.File, value);

        public static BsonUInt16 CreateUInt16(this IBsonCreator bson, ushort value) => new BsonUInt16(bson.File, value);

        public static BsonInt32 CreateInt32(this IBsonCreator bson, int value) => new BsonInt32(bson.File, value);

        public static BsonUInt32 CreateUInt32(this IBsonCreator bson, uint value) => new BsonUInt32(bson.File, value);

        public static BsonDecimal CreateDecimal(this IBsonCreator bson, decimal value) => new BsonDecimal(bson.File, value);

        public static BsonSpan CreateSpan(this IBsonCreator bson, Span value) => new BsonSpan(bson.File, value);
    }
}
