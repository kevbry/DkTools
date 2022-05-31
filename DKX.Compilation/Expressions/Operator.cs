using DKX.Compilation.ReportItems;
using System;
using System.IO;

namespace DKX.Compilation.Expressions
{
    public enum Operator
    {
        Assign,
        AssignAdd,
        AssignSubtract,
        AssignMultiply,
        AssignDivide,
        AssignModulus,

        Add,
        Subtract,
        Multiply,
        Divide,
        Modulus,

        Not,
        And,
        Or,

        Negative,
        Increment,
        Decrement,

        Dot,

        Equal,
        NotEqual,
        LessThan,
        LessEqual,
        GreaterThan,
        GreaterEqual,

        Ternary1,
        Ternary2
    }

    public enum OpPrec
    {
        // Lowest
        None,
        Compare,
        Assign,
        Or,
        And,
        Ternary,
        AddSub,
        MulDiv,
        Not,
        Negative,
        IncDec,
        Dot
        // Highest
    }

    public static class OperatorUtil
    {
        public static OpPrec GetPrecedence(this Operator op)
        {
            switch (op)
            {
                case Operator.Dot:
                    return OpPrec.Dot;
                case Operator.Increment:
                case Operator.Decrement:
                    return OpPrec.IncDec;
                case Operator.Negative:
                    return OpPrec.Negative;
                case Operator.Not:
                    return OpPrec.Not;
                case Operator.Multiply:
                case Operator.Divide:
                case Operator.Modulus:
                    return OpPrec.MulDiv;
                case Operator.Add:
                case Operator.Subtract:
                    return OpPrec.AddSub;
                case Operator.Ternary1:
                case Operator.Ternary2:
                    return OpPrec.Ternary;
                case Operator.And:
                    return OpPrec.And;
                case Operator.Or:
                    return OpPrec.Or;
                case Operator.Assign:
                case Operator.AssignAdd:
                case Operator.AssignSubtract:
                case Operator.AssignMultiply:
                case Operator.AssignDivide:
                case Operator.AssignModulus:
                    return OpPrec.Assign;
                case Operator.Equal:
                case Operator.NotEqual:
                case Operator.LessThan:
                case Operator.LessEqual:
                case Operator.GreaterThan:
                case Operator.GreaterEqual:
                    return OpPrec.Compare;
                default:
                    throw new InvalidOperatorException();
            }
        }

        public static bool IsLeftToRight(this Operator op)
        {
            switch (op)
            {
                case Operator.Assign:
                case Operator.AssignAdd:
                case Operator.AssignSubtract:
                case Operator.AssignMultiply:
                case Operator.AssignDivide:
                case Operator.AssignModulus:
                    return false;
                default:
                    return true;
            }
        }

        public static string GetText(this Operator op)
        {
            switch (op)
            {
                case Operator.Dot: return ".";
                case Operator.Increment: return "++";
                case Operator.Decrement: return "--";
                case Operator.Multiply: return "*";
                case Operator.Divide: return "/";
                case Operator.Modulus: return "%";
                case Operator.Add: return "+";
                case Operator.Subtract: return "-";
                case Operator.Assign: return "=";
                case Operator.AssignAdd: return "+=";
                case Operator.AssignSubtract: return "-=";
                case Operator.AssignMultiply: return "*=";
                case Operator.AssignDivide: return "/=";
                case Operator.AssignModulus: return "%=";
                case Operator.Equal: return "==";
                case Operator.NotEqual: return "!=";
                case Operator.LessThan: return "<";
                case Operator.LessEqual: return "<=";
                case Operator.GreaterThan: return ">";
                case Operator.GreaterEqual: return ">=";
                case Operator.Negative: return "-";
                case Operator.Not: return "!";
                case Operator.And: return "&&";
                case Operator.Or: return "||";
                case Operator.Ternary1: return "?";
                case Operator.Ternary2: return ":";
                default: throw new InvalidOperatorException();
            }
        }

        /// <summary>
        /// Returns true if the operator occurs before a single item (!, -)
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public static bool IsUnaryPre(this Operator op)
        {
            switch (op)
            {
                case Operator.Not:
                case Operator.Negative:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns true if the operator occurs after a single item (++, --)
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public static bool IsUnaryPost(this Operator op)
        {
            switch (op)
            {
                case Operator.Increment:
                case Operator.Decrement:
                    return true;

                default:
                    return false;
            }
        }

        public static bool YieldsBoolean(this Operator op)
        {
            switch (op)
            {
                case Operator.Equal:
                case Operator.NotEqual:
                case Operator.LessThan:
                case Operator.LessEqual:
                case Operator.GreaterThan:
                case Operator.GreaterEqual:
                case Operator.Not:
                case Operator.And:
                case Operator.Or:
                    return true;
                default:
                    return false;
            }
        }

        public static bool GetCompareResult(this Operator op, int compareResult)
        {
            switch (op)
            {
                case Operator.Equal:
                    return compareResult == 0;
                case Operator.NotEqual:
                    return compareResult != 0;
                case Operator.LessThan:
                    return compareResult < 0;
                case Operator.LessEqual:
                    return compareResult <= 0;
                case Operator.GreaterThan:
                    return compareResult > 0;
                case Operator.GreaterEqual:
                    return compareResult >= 0;
                default:
                    throw new InvalidOperatorException();
            }
        }

        public static decimal GetMathResult(this Operator op, decimal left, decimal right, IReportItemCollector reportOrNull, Span errorSpan)
        {
            switch (op)
            {
                case Operator.Add:
                    return left + right;
                case Operator.Subtract:
                    return left - right;
                case Operator.Multiply:
                    return left * right;
                case Operator.Divide:
                    if (right == 0)
                    {
                        reportOrNull?.Report(errorSpan, ErrorCode.DivideByZero);
                        return 0;
                    }
                    return left / right;
                case Operator.Modulus:
                    if (right == 0)
                    {
                        reportOrNull?.Report(errorSpan, ErrorCode.DivideByZero);
                        return 0;
                    }
                    return left % right;
                default:
                    throw new InvalidOperatorException();
            }

        }

        public static void Serialize(this Operator op, BinaryWriter bin)
        {
            bin.Write((byte)op);
        }

        public static Operator Deserialize(BinaryReader bin)
        {
            var value = bin.ReadByte();
            if (!Enum.IsDefined(typeof(Operator), value)) throw new InvalidOperatorException();
            return (Operator)value;
        }
    }

    class InvalidOperatorException : Exception { }
}
