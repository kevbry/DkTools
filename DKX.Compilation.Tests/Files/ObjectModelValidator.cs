using DKX.Compilation.ObjectFiles;
using NUnit.Framework;
using System.Collections;
using System.Linq;

namespace DKX.Compilation.Tests.Files
{
    static class ObjectModelValidator
    {
        public static void ValidateModel(ObjectFileModel expectedModel, ObjectFileModel actualModel)
        {
            Validate(expectedModel, actualModel, "model", approachAttribute: null);
        }

        private static void Validate(object expected, object actual, string relPath, TestApproachAttribute approachAttribute)
        {
            if (expected == null) Assert.IsNull(actual, $"{relPath} - Expected null but got not null.");
            if (actual == null) Assert.IsNull(expected, $"{relPath} - Expected not null but got null.");
            if (expected == null) return;

            var type = expected.GetType();
            Assert.AreEqual(type, actual.GetType(), $"{relPath} - Expected type '{type}' but got '{actual.GetType()}'.");

            if (type.IsClass && type != typeof(string))
            {
                if (expected is IEnumerable)
                {
                    var expEnumerator = (expected as IEnumerable).GetEnumerator();
                    var actEnumerator = (actual as IEnumerable).GetEnumerator();
                    var index = 0;

                    while (true)
                    {
                        var expMove = expEnumerator.MoveNext();
                        var actMove = actEnumerator.MoveNext();
                        if (expMove != actMove)
                        {
                            Assert.IsTrue(expMove, $"{relPath} - List ends at index {index} but got more elements.");
                            Assert.IsTrue(actMove, $"{relPath} - Actual ends at index {index} but expected more elements.");
                        }
                        if (!expMove) break;
                        Validate(expEnumerator.Current, actEnumerator.Current, $"{relPath}[{index}]", approachAttribute);
                        index++;
                    }
                }
                else
                {
                    foreach (var propInfo in type.GetProperties())
                    {
                        var expectedProperty = propInfo.GetValue(expected);
                        var actualProperty = propInfo.GetValue(actual);

                        var propApproach = propInfo.GetCustomAttributes(typeof(TestApproachAttribute), inherit: false)
                            .Cast<TestApproachAttribute>().FirstOrDefault();

                        Validate(expectedProperty, actualProperty, $"{relPath}.{propInfo.Name}", propApproach);
                    }
                }
            }
            else // Primitive type
            {
                //switch (approachAttribute?.Approach ?? TestApproach.Normal)
                //{
                //    case TestApproach.OpCodeValidator:
                //        Assert.AreEqual(typeof(string), type, $"{relPath} - Expected op code string to be a string.");
                //        OpCodeStringValidator.Validate((string)expected, (string)actual);
                //        return;
                //}

                if (approachAttribute?.IgnoreCase == true && type == typeof(string))
                {
                    Assert.AreEqual((expected as string).ToLower(), (actual as string).ToLower(), $"{relPath} - Values are not equal.");
                }
                else
                {
                    Assert.AreEqual(expected, actual, $"{relPath} - Values are not equal.");
                }
            }
        }
    }
}
