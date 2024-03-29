﻿using System;

using Codice.LogWrapper;

namespace SlackPlug
{
    class WebSocketRequest
    {
        internal WebSocketRequest(string slackToken)
        {
            mSlackToken = slackToken;
            mSlackCache = new SlackCache(slackToken, null);
        }

        internal void Init()
        {
            mSlackCache.Reload().Wait();
        }

        internal string ProcessMessage(string rawMessage)
        {
            string requestId = Messages.GetRequestId(rawMessage);
            try
            {
                NotificationMessage message = Messages.ReadNotificationMessage(rawMessage);

                SlackNotification.Notify(
                    message.Message, message.Recipients, mSlackCache, mSlackToken).Wait();

                return Messages.BuildSuccessfulResponse(requestId);
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat("Error processing message:\n{0}. Error: {1}",
                    rawMessage, ex.Message);
                mLog.Debug(ex.StackTrace);
                return Messages.BuildErrorResponse(requestId, ex.Message);
            }
        }

        readonly SlackCache mSlackCache;
        readonly string mSlackToken;
        static readonly ILog mLog = LogManager.GetLogger("slackplug");
    }
}