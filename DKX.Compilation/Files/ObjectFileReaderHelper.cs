using DK.Code;
using System.Collections.Generic;

namespace DKX.Compilation.Files
{
    public static class ObjectFileReaderHelper
    {
        public static IEnumerable<FileContext> GetFileContexts(this IObjectFileReader ofr)
        {
            var model = ofr.GetModel();
            var fileContexts = new HashSet<FileContext>();

            if (model.Methods != null)
            {
                foreach (var method in model.Methods)
                {
                    if (!fileContexts.Contains(method.FileContext)) fileContexts.Add(method.FileContext);
                }
            }

            if (model.Properties != null)
            {
                foreach (var property in model.Properties)
                {
                    if (property.Getters != null)
                    {
                        foreach (var getter in property.Getters)
                        {
                            if (!fileContexts.Contains(getter.FileContext)) fileContexts.Add(getter.FileContext);
                        }
                    }

                    if (property.Setters != null)
                    {
                        foreach (var setter in property.Setters)
                        {
                            if (!fileContexts.Contains(setter.FileContext)) fileContexts.Add(setter.FileContext);
                        }
                    }
                }
            }

            if (model.MemberVariables != null)
            {
                foreach (var variable in model.MemberVariables)
                {
                    if (!fileContexts.Contains(variable.FileContext)) fileContexts.Add(variable.FileContext);
                }
            }

            return fileContexts;
        }
    }
}
