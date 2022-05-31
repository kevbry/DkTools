using System.IO;

namespace DKX.Compilation.Project.Bson
{
    public class BsonSpan : BsonNode
    {
        private Span _span;

        public BsonSpan(BsonFile file, Span span)
            : base(file)
        {
            _span = span;
        }

        public BsonSpan(BsonFile file, BinaryReader bin)
            : base(file)
        {
            var stringId = bin.ReadInt32();
            var start = bin.ReadInt32();
            var end = bin.ReadInt32();

            _span = new Span(file.GetString(stringId), start, end);
        }

        protected override void WriteInner(BinaryWriter bin)
        {
            var stringId = File.AddString(_span.PathName ?? string.Empty);
            bin.Write(stringId);
            bin.Write(_span.Start);
            bin.Write(_span.End);
        }

        protected override NodeType NodeTypeId => NodeType.Span;
        public Span Value => _span;
    }
}
