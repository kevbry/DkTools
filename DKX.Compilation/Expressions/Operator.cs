using System;

namespace DKX.Compilation.Expressions
{
    enum Operator
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
        Negative,

        Increment,
        Decrement,

        Dot,

        Equal,
        NotEqual,
        LessThan,
        LessEqual,
        GreaterThan,
        GreaterEqual
    }

    enum OpPrec
    {
        // Lowest
        None,
        Compare,
        Assign,
        AddSub,
        MulDiv,
        Not,
        Negative,
        IncDec,
        Dot
        // Highest
    }

    static class OperatorUtil
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
                case Operator.Assign: return "==";
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
                default: throw new InvalidOperatorException();
            }
        }

        public static string GetOpCode(this Operator op)
        {
            switch (op)
            {
                case Operator.Dot: return "dot";
                case Operator.Increment: return "inc";
                case Operator.Decrement: return "dec";
                case Operator.Multiply: return "mul";
                case Operator.Divide: return "div";
                case Operator.Modulus: return "mod";
                case Operator.Add: return "add";
                case Operator.Subtract: return "sub";
                case Operator.Assign: return "asn";
                case Operator.AssignAdd: return "aad";
                case Operator.AssignSubtract: return "asb";
                case Operator.AssignMultiply: return "aml";
                case Operator.AssignDivide: return "adv";
                case Operator.AssignModulus: return "amo";
                case Operator.Equal: return "eq";
                case Operator.NotEqual: return "ne";
                case Operator.LessThan: return "lt";
                case Operator.LessEqual: return "le";
                case Operator.GreaterThan: return "gt";
                case Operator.GreaterEqual: return "ge";
                case Operator.Negative: return "neg";
                case Operator.Not: return "not";
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
    }

    class InvalidOperatorException : Exception { }
}
