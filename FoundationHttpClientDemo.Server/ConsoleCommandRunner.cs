using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoundationHttpClientDemo.Server
{
    public abstract class ConsoleCommandRunner
    {
        protected ConsoleCommandRunner()
        {
        }

        protected async Task RunConsoleCommandAndShowOutputAsync(string command)
        {
            var consoleOutput = await RunConsoleCommandAsync(command);
            if (!string.IsNullOrWhiteSpace(consoleOutput))
            {
                Console.WriteLine(consoleOutput);
            }
        }

        protected async Task<string> RunConsoleCommandAsync(string command)
        {
            string consoleOutput;
            var psi = new ProcessStartInfo("CMD.exe", command)
                          {
                              WindowStyle = ProcessWindowStyle.Hidden,
                              RedirectStandardOutput = true,
                              UseShellExecute = false,
                              StandardOutputEncoding = Encoding.UTF8
                          };
            using (var cmd = Process.Start(psi))
            {
                consoleOutput = await cmd.StandardOutput.ReadToEndAsync();

                cmd.WaitForExit();
                cmd.Dispose();
            }

            return consoleOutput;
        }
    }
}
