using DKX.Compilation.Project.Bson;
using DKX.Compilation.Resolving;
using DKX.Compilation.Variables.ConstTerms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DKX.Compilation.Project
{
    class ProjectNamespace : INamespace
    {
        private string _name;
        private Dictionary<string, ProjectClass> _classes = new Dictionary<string, ProjectClass>();
        private DateTime _scanTime;
        private bool _system;

        public ProjectNamespace(string name, DateTime? scanTime = null)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _scanTime = scanTime ?? DateTime.MinValue;
        }

        private ProjectNamespace(string name, DateTime scanTime, Dictionary<string, ProjectClass> classes)
        {
            _name = name;
            _scanTime = scanTime;
            _classes = classes;
        }

        public ProjectNamespace(string name, bool system)
        {
            _name = name;
            _system = system;
        }

        public NamespaceAccessType AccessType => _system ? NamespaceAccessType.System : NamespaceAccessType.Normal;
        public IEnumerable<ProjectClass> Classes => _classes.Values;
        IEnumerable<IClass> INamespace.Classes => _classes.Values;
        public string NamespaceName => _name;
        public DateTime ScanTime => _scanTime;
        public bool System => _system;

        IClass INamespace.GetClass(string name)
        {
            if (_classes.TryGetValue(name, out var class_)) return class_;
            return null;
        }

        public ProjectClass GetClass(string name)
        {
            if (_classes.TryGetValue(name, out var class_)) return class_;
            return null;
        }

        public void AddClass(ProjectClass class_)
        {
            _classes[(class_ ?? throw new ArgumentNullException(nameof(class_))).ClassName] = class_;
        }

        public void Update(CompilePhase phase, INamespace fileNamespace)
        {
            _scanTime = DateTime.Now;

            foreach (var fileClass in fileNamespace.Classes)
            {
                if (!_classes.TryGetValue(fileClass.ClassName, out var projectClass))
                {
                    _classes[fileClass.ClassName] = projectClass = new ProjectClass(fileClass.ClassName, fileClass.FullClassName, fileClass.NamespaceName, fileClass.WbdkClassName, fileClass.DkxPathName, fileClass.NameSpan);
                }

                projectClass.Update(phase, fileClass);
            }
        }

        public BsonObject ToBson(BsonFile bson)
        {
            var bsonObj = new BsonObject(bson);
            bsonObj["Name"] = new BsonString(bson, _name);
            bsonObj["ScanTime"] = new BsonDateTime(bson, _scanTime);
            bsonObj["Classes"] = new BsonArray(bson, _classes.Values.Select(x => x.ToBson(bson)));
            return bsonObj;
        }

        public static ProjectNamespace FromBson(BsonNode node)
        {
            if (!(node is BsonObject obj)) throw new InvalidBsonDataException("Namespace node is not an object.");

            var name = obj.GetString("Name");
            var scanTime = obj.GetDateTime("ScanTime");

            var classes = new Dictionary<string, ProjectClass>();
            foreach (var cls in obj.GetArray("Classes").Select(x => ProjectClass.FromBson(x)))
            {
                classes[cls.ClassName] = cls;
            }

            return new ProjectNamespace(name, scanTime, classes);
        }

        public void ClearAllConstants()
        {
            foreach (var cls in _classes.Values)
            {
                cls.ClearAllConstants();
            }
        }

        public void ResolveAllConstants(ConstResolutionContext context)
        {
            foreach (var cls in _classes.Values)
            {
                cls.ResolveAllConstants(context);
            }
        }

        public int CountUnresolvedConstants()
        {
            var count = 0;

            foreach (var cls in _classes.Values)
            {
                count += cls.CountUnresolvedConstants();
            }

            return count;
        }
    }
}
