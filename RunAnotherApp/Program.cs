using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Linq;

namespace RunAnotherApp
{
    class Program
    {
        private static StreamWriter file;

        static int Main(string[] args)
        {
            using (var fileStream = new FileStream(Assembly.GetExecutingAssembly().Location + ".log", FileMode.Create))
            {
                file = new StreamWriter(fileStream) { AutoFlush = true };
                var fileToRun = ConfigurationManager.AppSettings["fileToRun"];
                var commandLineParameters = ConfigurationManager.AppSettings["commandLineParameters"];
                
                var process = new Process
                                  {
                                      EnableRaisingEvents = true,
                                      StartInfo = new ProcessStartInfo
                                                      {
                                                          Arguments =
                                                              string.Format(commandLineParameters,
                                                                            string.Join(" ",
                                                                            args.Select(a => string.Format("\"{0}\"", a)))),
                                                          CreateNoWindow = false,
                                                          RedirectStandardOutput = true,
                                                          RedirectStandardError = true,
                                                          UseShellExecute = false,
                                                          FileName = fileToRun
                                                      }
                                  };

                file.WriteLine("Running: {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

                process.ErrorDataReceived += ProcessDataReceived;

                process.Start();
                process.BeginErrorReadLine();

                var output = process.StandardOutput.ReadToEnd();

                process.WaitForExit();

                Console.Write(output);
                file.Write(output);

                return process.ExitCode;
            }
        }

        private static void ProcessDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            string line = dataReceivedEventArgs.Data;
            Console.Write(line);
            file.Write(line);
        }
    }
}
