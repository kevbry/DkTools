using DK.Code;

namespace DKX.Compilation.CodeGeneration.OpCodes
{
    class OpCodeGenerator
    {
        /// <summary>
        /// Generates a suffix that contains the relative span of an item.
        /// </summary>
        /// <param name="parentOffset">Starting position of the parent item.
        /// Pass -1 to not generate any suffix.</param>
        /// <param name="span">Span of the item.</param>
        /// <returns>A suffix string that encodes a span relative to the parent element.
        /// Empty string is parentOffset is -1.</returns>
        public static string GenerateSpanCode(int parentOffset, CodeSpan span)
        {
            if (parentOffset < 0) return string.Empty;

            var start = span.Start - parentOffset;
            var end = span.End - parentOffset;
            if (start < 0) start = 0;
            if (end < 0) end = 0;
            var len = end - start;

            if (len < 100)
            {
                return $":{start * 100 + len}";
            }
            else
            {
                return $":{start}:{len}";
            }
        }

        public static string GenerateOpCode(string opCodeName, int parentOffset, CodeSpan fullOpCodeSpan)
        {
            return $"{opCodeName}{GenerateSpanCode(parentOffset, fullOpCodeSpan)}";
        }

        public static string GenerateVariable(string variableName, int parentOffset, CodeSpan variableSpan)
        {
            return $"${variableName}{GenerateSpanCode(parentOffset, variableSpan)}";
        }

        public static string GenerateStringLiteral(string rawText, int parentOffset, CodeSpan literalSpan)
        {
            return $"{CodeParser.StringToStringLiteral(rawText)}{GenerateSpanCode(parentOffset, literalSpan)}";
        }

        public static string GenerateNumber(string numberText, int parentOffset, CodeSpan numberSpan)
        {
            return $"{numberText}{GenerateSpanCode(parentOffset, numberSpan)}";
        }
    }
}
