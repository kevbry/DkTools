using DK.Code;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Jobs;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables.ConstTerms;
using static DKX.Compilation.Scopes.Modifiers;

namespace DKX.Compilation.SystemClasses
{
    static class SystemAttributes
    {
        public static void CheckAttributeForMethod(AttributeModifier attribute, Modifiers modifiers, MethodScope method, CompilePhase phase)
        {
            try
            {
                switch (attribute.Name)
                {
                    case DkxConst.Attributes.ServerProgram:
                    case DkxConst.Attributes.GatewayProgram:
                        if (phase == CompilePhase.MemberScan || phase == CompilePhase.FullCompilation)
                        {
                            var fileContext = attribute.Name == DkxConst.Attributes.ServerProgram ? FileContext.ServerProgram : FileContext.GatewayProgram;
                            var relPathName = null as string;

                            if (phase == CompilePhase.FullCompilation)
                            {
                                if (attribute.Arguments.Length != 1) throw new CodeException(attribute.Span, ErrorCode.AttributeRequiresSingleArgument, attribute.Name);

                                var constContext = new ConstResolutionContext(method, method.Project);
                                var constValue = attribute.Arguments[0].ResolveConstantOrNull(constContext, DkxConst.EmptyStringArray);
                                if (constValue == null || !constValue.IsString) throw new CodeException(attribute.Span, ErrorCode.AttributeExpectedProgramPathName);
                                relPathName = constValue.String;
                                if (string.IsNullOrWhiteSpace(relPathName) || relPathName.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0) throw new CodeException(attribute.Span, ErrorCode.AttributeExpectedProgramPathName);

                                // Add the '.sp'/'.gp' extension if the developer didn't include it.
                                var ext = FileContextHelper.GetExtension(fileContext);
                                if (!relPathName.EndsWith(ext, System.StringComparison.OrdinalIgnoreCase)) relPathName += ext;
                            }
                            else
                            {
                                relPathName = method.GetScope<ClassScope>().WbdkClassName + FileContextHelper.GetExtension(fileContext);
                            }

                            if (fileContext == FileContext.ServerProgram)
                            {
                                if (method.FileContext.IsServerSide()) throw new CodeException(attribute.Span, ErrorCode.AttributeMustHaveContext, attribute.Name, DkxConst.Keywords.Server);
                            }
                            else
                            {
                                if (method.FileContext.IsClientSide()) throw new CodeException(attribute.Span, ErrorCode.AttributeMustHaveContext, attribute.Name, DkxConst.Keywords.Client);
                            }

                            if (!method.Flags.HasFlag(ModifierFlags.Static)) throw new CodeException(attribute.Span, ErrorCode.AttributeMustBeStatic, attribute.Name);
                            method.Flags |= ModifierFlags.NotCallable | ModifierFlags.ProgramEntryPoint;
                            method.FileContext = FileContext.ServerProgram;
                            method.WbdkName = "main";
                            method.FileTarget = new FileTarget(FileContext.ServerProgram, relPathName);
                        }
                        break;

                    default:
                        method.Report(attribute.Span, ErrorCode.AttributeNotValidHere, attribute.Name);
                        break;
                }
            }
            catch (CodeException ex)
            {
                method.AddReportItem(ex.ToReportItem());
            }
        }
    }

}
