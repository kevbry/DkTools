using DK.AppEnvironment;
using DK.Diagnostics;
using DK.Implementation.Windows;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compiler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var fileSystem = new WindowsFileSystem();
                var log = new ConsoleLogger();
                var config = new WindowsAppConfigSource(log);
                
                var app = new DkAppContext(fileSystem, log, config);
                app.LoadAppSettings(null);

                var compiler = new Compilation.Compiler(app);

                await compiler.CompileAsync(CancellationToken.None);

#if DEBUG
                if (Debugger.IsAttached)
                {
                    Console.WriteLine();
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
