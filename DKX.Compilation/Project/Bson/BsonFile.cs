using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DKX.Compilation.Project.Bson
{
    public class BsonFile : IBsonCreator
    {
        private const uint FileSignature = 0x4e4f5342;
        private const uint FileVersion = 1;

        private BsonObject _root;
        private List<string> _strings = new List<string>();
        private Dictionary<string, int> _sortedStrings = new Dictionary<string, int>();

        public BsonFile()
        {
            _root = new BsonObject(this);

            _strings.Add(string.Empty);
            _sortedStrings[string.Empty] = 0;
        }

        public BsonFile File => this;
        public BsonObject Root => _root;

        public string GetString(int id)
        {
            if (id < 0 || id >= _strings.Count) throw new ArgumentOutOfRangeException(nameof(id));
            return _strings[id];
        }

        public int GetStringId(string str)
        {
            if (_sortedStrings.TryGetValue(str, out var id)) return id;
            return -1;
        }

        public int AddString(string str)
        {
            if (_sortedStrings.TryGetValue(str, out var id)) return id;

            id = _strings.Count;
            _strings.Add(str);
            _sortedStrings[str] = id;
            return id;
        }

        public void Write(Stream stream)
        {
            using (var bin = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                bin.Write(FileSignature);
                bin.Write(FileVersion);

                bin.Write(_strings.Count);
                foreach (var str in _strings) bin.Write(str);

                _root.Write(bin);

                bin.Flush();
            }
        }

        public void Read(Stream stream)
        {
            using (var bin = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                var sig = bin.ReadUInt32();
                if (sig != FileSignature) throw new InvalidBsonDataException($"File signature does not match (expected 0x{FileSignature:X} but was 0x{sig:X})");

                var version = bin.ReadUInt32();
                if (version != FileVersion) throw new InvalidBsonDataException($"File version does not match (expected {FileVersion} but was {version})");

                var numStrings = bin.ReadInt32();
                _strings.Clear();
                _sortedStrings.Clear();
                for (var i = 0; i < numStrings; i++)
                {
                    var str = bin.ReadString();
                    _strings.Add(str);
                    _sortedStrings[str] = i;
                }

                var root = BsonNode.Read(this, bin);
                if (!(root is BsonObject obj)) throw new InvalidBsonDataException("Root node is not an object.");
                _root = obj;
            }
        }

        public string ToJson(Formatting formatting = Formatting.None)
        {
            var sb = new StringBuilder();

            using (var textWriter = new StringWriter(sb))
            using (var jsonWriter = new JsonTextWriter(textWriter))
            {
                jsonWriter.Formatting = formatting;
                _root.WriteJson(jsonWriter);
            }

            return sb.ToString();
        }
    }

    class BsonException : Exception
    {
        public BsonException(string message) : base(message) { }
    }

    class InvalidBsonTypeException : BsonException
    {
        public InvalidBsonTypeException() : base("The BSON type was not expected.") { }
        public InvalidBsonTypeException(string message) : base(message) { }
    }

    class InvalidBsonDataException : BsonException
    {
        public InvalidBsonDataException() : base("The BSON data is not structured correctly.") { }
        public InvalidBsonDataException(string message) : base(message) { }
    }
}
