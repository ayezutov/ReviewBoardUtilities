using System;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using ReviewBoardTfsAutoMerger.Log;

namespace ReviewBoardTfsAutoMerger
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                if (args.Length != 1)
                {
                    WriteHelpMessageToConsole();
                    return;
                }

                switch (args[0])
                {
                    case "/install":
                        ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
                        break;
                    case "/uninstall":
                        ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;
                    case "/run":
                        Run(new ConsoleLog());
                        break;
                    default:
                        WriteHelpMessageToConsole();
                        return;
                }
            }
            else
            {
                using (var service = new ReviewBoardTfsAutoPost())
                {
                    ServiceBase.Run(service);
                }
            }
            
        }

        private static void WriteHelpMessageToConsole()
        {
            Console.WriteLine(
                string.Format(
                    "Usage:\r\n\tInstall the service: {0} /install\r\n\tUninstall the service: {0} /uninstall\r\n\tRun from console: {0} /run",
                    Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location)));
        }


        public static void Run(ILog log)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => log.Error(args.ExceptionObject.ToString());
            while (true)
            {
                var configuration = new Configuration.Configuration();
                try
                {
                    new AutomatedPostReview(log, configuration).Run();
                }
                catch (Exception ex)
                {
                    log.Error(ex.ToString());
                }
                Thread.Sleep(configuration.Timeout);
            }
        }
    }
}
