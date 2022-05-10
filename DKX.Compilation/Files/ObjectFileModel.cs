using DKX.Compilation.Variables;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

/*
Code Strings:

Op      Description         Example
i       Identifier          icust
n       Number literal      n-123.45
s       String literal      s"hello"
c       Char literal        c'x'
t       Boolean true        t
f       Boolean false       f
#       Operator            #add(n1,n2)
;       statement delim     #asn(ix,n0);#asn(iy,0)                          x = 0; y = 0;
if      if statement        if(#eq(ix,n0),{...},#eq(ix,n1),{...},{...})     if (x == 0) { ... } else if (x == 1) { ... } else { ... }
while   while statement     while(t,{...})                                  while (true) { ... }
dow     do-while statement  dow({...},t)                                    do { ... } while (true);
for     for statement       for({#asn(ii,n0)},#lt(ii,n10),{#inc(ii)},{...}) for (i = 0; i < 10; i++) { ... }
ret     return statement    ret(n0) --or-- ret()                            return 0; --or-- return;
*/

namespace DKX.Compilation.Files
{
    public class ObjectFileModel
    {
        public string SourcePathName { get; set; }

        public string DestinationPathName { get; set; }

        public string ClassName { get; set; }

        public ObjectFileDependency[] FileDependencies { get; set; }

        public ObjectTableDependency[] TableDependencies { get; set; }

        public ObjectMethod[] Methods { get; set; }

        public ObjectProperty[] Properties { get; set; }

        public ObjectMemberVariable[] MemberVariables { get; set; }

        public ObjectConstant[] Constants { get; set; }
    }

    public class ObjectMethod
    {
        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Privacy Privacy { get; set; }

        public string ReturnDataType { get; set; }

        public ObjectMethodArgument[] Arguments { get; set; }

        public ObjectBody Body { get; set; }
    }

    public class ObjectProperty
    {
        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Privacy Privacy { get; set; }

        public string DataType { get; set; }

        public bool ReadOnly { get; set; }

        public ObjectBody Getter { get; set; }

        public ObjectBody Setter { get; set; }
    }

    public class ObjectMethodArgument
    {
        public string Name { get; set; }

        public string DataType { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ArgumentPassType PassType { get; set; }
    }

    public class ObjectMemberVariable
    {
        public string Name { get; set; }

        public string DataType { get; set; }
    }

    public class ObjectConstant
    {
        public string Name { get; set; }

        public string DataType { get; set; }

        public string Code { get; set; }
    }

    public class ObjectBody
    {

        public ObjectVariable[] Variables { get; set; }

        public string[] Statements { get; set; }
    }

    public class ObjectVariable
    {
        public string Name { get; set; }

        public string DataType { get; set; }

        public string InitializerCode { get; set; }
    }

    public class ObjectFileDependency
    {
        public string PathName { get; set; }

        public static readonly ObjectFileDependency[] EmptyArray = new ObjectFileDependency[0];
    }

    public class ObjectTableDependency
    {
        public string TableName { get; set; }

        public string Hash { get; set; }

        public static readonly ObjectTableDependency[] EmptyArray = new ObjectTableDependency[0];
    }

    class InvalidObjectFileException : Exception
    {
        public InvalidObjectFileException(string pathName) : base($"Object file '{pathName}' does not have a correct format.") { }
    }
}
