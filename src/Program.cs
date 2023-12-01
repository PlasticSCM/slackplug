using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using Codice.LogWrapper;

namespace SlackPlug
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                PlugArguments plugArgs = new PlugArguments(args);

                bool bValidArgs = plugArgs.Parse();

                ConfigureLogging(plugArgs.BasePath, plugArgs.BotName);

                mLog.InfoFormat("SlackPlug [{0}] started. Version [{1}]",
                    plugArgs.BotName,
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

                string argsStr = args == null ? string.Empty : string.Join(" ", args);
                mLog.DebugFormat("Args: [{0}]. Are valid args?: [{1}]", argsStr, bValidArgs);

                if (!bValidArgs || plugArgs.ShowUsage)
                {
                    PrintUsage();
                    return 0;
                }

                CheckArguments(plugArgs);

                Config config = ParseConfig.Parse(File.ReadAllText(plugArgs.ConfigFilePath));

                LaunchSlackPlug(
                    plugArgs.WebSocketUrl,
                    config.SlackToken,
                    plugArgs.BotName,
                    plugArgs.ApiKey,
                    plugArgs.Organization);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                mLog.ErrorFormat("Error: {0}", ex.Message);
                mLog.DebugFormat("StackTrace: {0}", ex.StackTrace);
                return 1;
            }
        }

        static void LaunchSlackPlug(
            string serverUrl,
            string slackToken,
            string plugName,
            string apiKey,
            string organization)
        {
            mLog.DebugFormat("Starting ws...");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            WebSocketRequest request = new WebSocketRequest(slackToken);
            request.Init();

            WebSocketClient ws = new WebSocketClient(
                serverUrl,
                "notifierPlug",
                plugName,
                apiKey,
                organization,
                request.ProcessMessage);

            ws.ConnectWithRetries();

            Task.Delay(-1).Wait();
        }

        static void CheckArguments(PlugArguments plugArgs)
        {
            CheckAgumentIsNotEmpty(
                "Plastic web socket url endpoint",
                plugArgs.WebSocketUrl,
                "web socket url",
                "--server wss://blackmore:7111/plug");

            CheckAgumentIsNotEmpty("name for this bot", plugArgs.BotName, "name", "--name slack");
            CheckAgumentIsNotEmpty("connection API key", plugArgs.ApiKey, "api key",
                "--apikey 014B6147A6391E9F4F9AE67501ED690DC2D814FECBA0C1687D016575D4673EE3");
            CheckAgumentIsNotEmpty("JSON config file", plugArgs.ConfigFilePath, "file path",
                "--config slack-config.conf");
        }

        static void ConfigureLogging(string basePath, string plugName)
        {
            if (string.IsNullOrEmpty(plugName))
                plugName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm");

            try
            {
                string logOutputPath = Path.GetFullPath(Path.Combine(
                   basePath,
                   "../../../../logs",
                   "slackplug." + plugName + ".log.txt"));
                string log4netpath = LogConfig.GetLogConfigFile(basePath);
                log4net.GlobalContext.Properties["LogOutputPath"] = logOutputPath;
                Configurator.Configure(log4netpath);
            }
            catch
            {
                //it failed configuring the logging info; nothing to do.
            }
        }

        static void CheckAgumentIsNotEmpty(
            string fielName, string fieldValue, string type, string example)
        {
            if (!string.IsNullOrEmpty(fieldValue))
                return;
            string message = string.Format("slackplug can't start without specifying a {0}.{1}" +
                "Please type a valid {2}. Example:  \"{3}\"",
                fielName, Environment.NewLine, type, example);
            throw new Exception(message);
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\tslackplug.exe --server <WEB_SOCKET_URL> --config <JSON_CONFIG_FILE_PATH>");
            Console.WriteLine("\t              --apikey <WEB_SOCKET_CONN_KEY> --name <PLUG_NAME>");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("\tslackplug.exe --server wss://blackmore:7111/plug --config slack-config.conf ");
            Console.WriteLine("\t              --apikey x2fjk28fda --name slack");
            Console.WriteLine();

        }

        static readonly ILog mLog = LogManager.GetLogger("slackplug");
    }
}
