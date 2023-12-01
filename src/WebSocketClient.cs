using System;
using System.Threading;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using Codice.LogWrapper;
using WebSocketSharp;
using System.Threading.Tasks;

namespace SlackPlug
{
    class WebSocketClient
    {
        internal WebSocketClient(
            string serverUrl,
            string type,
            string name,
            string apikey,
            string organization,
            Func<string, string> processMessage)
        {
            mName = name;
            mApiKey = apikey;
            mOrganization = organization;
            mType = type;

            mWebSocket = new WebSocket(serverUrl);
            mWebSocket.OnMessage += OnMessage;
            mWebSocket.OnClose += OnClose;
            mWebSocket.Log.Output = LogOutput;

            if (mWebSocket.IsSecure)
                mWebSocket.SslConfiguration.ServerCertificateValidationCallback += CertificateValidation;

            mProcessMessage = processMessage;
        }

        internal void ConnectWithRetries()
        {
            if (mbIsTryingConnection)
                return;

            mbIsTryingConnection = true;
            try
            {
                while (true)
                {
                    if (Connect())
                        return;

                    System.Threading.Thread.Sleep(5000);
                }
            }
            finally
            {
                mbIsTryingConnection = false;
            }
        }

        bool Connect()
        {
            mWebSocket.Connect();
            if (!mWebSocket.IsAlive)
                return false;

            mWebSocket.Send(Messages.BuildLoginMessage(mOrganization, mApiKey));
            mWebSocket.Send(Messages.BuildRegisterPlugMessage(mName, mType));

            mLog.Debug(string.Format("Plug ({0}) connected!", mName));
            return true;
        }

        void OnClose(object sender, CloseEventArgs closeEventArgs)
        {
            mLog.InfoFormat(
                "OnClose was called! Code [{0}]. Reason [{1}]",
                closeEventArgs.Code, closeEventArgs.Reason);

            ConnectWithRetries();
        }

        void OnMessage(object sender, MessageEventArgs e)
        {
            Task.Run(() => mWebSocket.Send(mProcessMessage(e.Data)));
        }

        static void LogOutput(LogData arg1, string arg2)
        {
        }

        static bool CertificateValidation(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        readonly WebSocket mWebSocket;

        readonly ILog mLog = LogManager.GetLogger("slackplug-websocket");
        readonly string mName;
        readonly string mApiKey;
        readonly string mOrganization;
        readonly string mType;
        readonly Func<string, string> mProcessMessage;

        volatile bool mbIsTryingConnection = false;
    }
}
