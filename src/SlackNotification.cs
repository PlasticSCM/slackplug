using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Codice.LogWrapper;

namespace SlackPlug
{
    static class SlackNotification
    {
        async internal static Task Notify(string message, List<string> recipients,
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
                        await NotifyChannel(slackId, message, slackCache, slackToken);
                        continue;
                    }

                    await NotifyUser(slackId, message, slackCache, slackToken);
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

        async static Task NotifyUser(SlackId slackId, string message,
            SlackCache slackCache, string slackToken)
        {
            string userId = await SlackUser.GetId(slackId, slackCache);

            if (string.IsNullOrEmpty(userId))
                throw new Exception(string.Format("User {0} not found", slackId.Id));

            await SlackAPI.PostChatMessage(slackToken, userId, message);
        }

        async static Task NotifyChannel(SlackId slackId, string message,
            SlackCache slackCache, string slackToken)
        {
            string channelId = await SlackChannel.GetId(slackId, slackCache);

            if (string.IsNullOrEmpty(channelId))
                throw new Exception(string.Format("Channel {0} not found", slackId.Id));

            await SlackAPI.PostChatMessage(slackToken,channelId, message);
        }

        static ILog mLog = LogManager.GetLogger("SlackNotification");
    }
}
