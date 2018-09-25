using System;
using System.Collections.Generic;

using log4net;

namespace SlackPlug
{
    static class SlackNotification
    {
        internal static void Notify(string message, List<string> recipients,
            SlackCache slackCache, string slackToken)
        {
            string errorMessage = string.Empty;
            foreach (string recipient in recipients)
            {
                try
                {
                    SlackId slackId = SlackId.Build(recipient);

                    if (slackId.Attrs.IsChannel)
                    {
                        NotifyChannel(slackId, message, slackCache, slackToken);
                        continue;
                    }

                    NotifyUser(slackId, message, slackCache, slackToken);
                }
                catch(Exception e)
                {
                    string currentError = string.Format(
                        "'{0}' was not notified due to the following error: {1}",
                        recipient, e.Message);

                    mLog.Error(currentError);
                    mLog.DebugFormat("StackTrace: {0}", e.StackTrace);

                    errorMessage += currentError + "\n";
                }
            }

            if (errorMessage == string.Empty)
                return;

            throw new Exception(errorMessage.Trim());
        }

        static void NotifyUser(SlackId slackId, string message,
            SlackCache slackCache, string slackToken)
        {
            string userId = SlackUser.GetId(slackId, slackCache);

            if (string.IsNullOrEmpty(userId))
                throw new Exception(string.Format("User {0} not found", slackId.Id));

            string channelId = null;
            try
            {
                channelId = SlackAPI.OpenIm(slackToken, userId);
            }
            catch
            {
                // if can't connect to the user
                // means the user no longer exists => reload the users cache
                mLog.ErrorFormat(
                    "Error openning a channel to user {0}. Reloading cache.",
                    slackId.Id);
                slackCache.ReloadUsers();
                throw;
            }

            SlackAPI.PostChatMessage(slackToken, channelId, message);
        }

        static void NotifyChannel(SlackId slackId, string message,
            SlackCache slackCache, string slackToken)
        {
            string channelId = SlackChannel.GetId(slackId, slackCache);

            if (string.IsNullOrEmpty(channelId))
                throw new Exception(string.Format("Channel {0} not found", slackId.Id));

            SlackAPI.PostChatMessage(slackToken,channelId, message);
        }

        static ILog mLog = LogManager.GetLogger("SlackNotification");
    }
}
