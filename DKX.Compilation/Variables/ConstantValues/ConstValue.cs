using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.ReportItems;
using System;

namespace DKX.Compilation.Variables.ConstantValues
{
    public abstract class ConstValue
    {
        public abstract DataType DataType { get; }
        public abstract bool? GetComparisonResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector reportOrNull);
        public abstract ConstValue GetMathResultOrNull(Operator op, ConstValue rightValue, IReportItemCollector reportOrNull);
        public abstract void SaveInner(BsonObject obj);

        internal abstract CodeFragment ToWbdkCode();

        public virtual bool IsBool => false;
        public virtual bool Bool => default;
        public virtual bool IsNumber => false;
        public virtual decimal Number => default;
        public virtual bool IsString => false;
        public virtual string String => default;
        public virtual bool IsChar => false;
        public virtual char Char => default;
        public virtual bool IsNull => false;
        public virtual bool IsDate => false;
        public virtual DkDate Date => default;
        public virtual bool IsTime => false;
        public virtual DkTime Time => default;


        private Span _span;

        public ConstValue(Span span)
        {
            _span = span;
        }

        public Span Span => _span;

        public BsonObject ToBson(BsonFile bson)
        {
            var obj = bson.CreateObject();

            obj.SetString("Type", GetType().FullName);
            obj.SetSpan("Span", _span);
            SaveInner(obj);

            return obj;
        }

        public static ConstValue FromBson(BsonNode node)
        {
            if (!(node is BsonObject obj)) throw new InvalidBsonDataException("ConstValue node is not an object.");

            var typeName = obj.GetString("Type");
            var span = obj.GetSpan("Span");

            var type = System.Reflection.Assembly.GetExecutingAssembly().GetType(typeName);
            if (type == null) throw new InvalidBsonDataException($"ConstValue type '{typeName}' was not found.");
            if (!typeof(ConstValue).IsAssignableFrom(type)) throw new InvalidBsonDataException($"Type '{typeName}' does not inherit from ConstValue.");

            // Find a compatible constructor which includes a BinaryReader.
            foreach (var ctor in type.GetConstructors())
            {
                var ctorParams = ctor.GetParameters();
                var gotBinaryReader = false;
                var compatible = true;
                foreach (var p in ctorParams)
                {
                    if (p.ParameterType == typeof(BsonObject))
                    {
                        gotBinaryReader = true;
                    }
                    else if (p.ParameterType != typeof(Span))
                    {
                        compatible = false;
                        break;
                    }
                }

                if (compatible && gotBinaryReader)
                {
                    var parms = new object[ctorParams.Length];
                    for (var i = 0; i < parms.Length; i++)
                    {
                        if (ctorParams[i].ParameterType == typeof(BsonObject)) parms[i] = obj;
                        else if (ctorParams[i].ParameterType == typeof(Span)) parms[i] = span;
                    }
                    return (ConstValue)ctor.Invoke(parms);
                }
            }

            throw new InvalidOperationException("A compatible constructor could not be found.");
        }
    }
}
