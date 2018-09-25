using log4net;

namespace SlackPlug
{
    static class SlackUser
    {
        internal static string GetId(SlackId recipient, SlackCache slackCache)
        {
            if (recipient.Attrs.IsSlackID)
                return recipient.Id;

            return Resolve(recipient.Id, slackCache);
        }

        static string Resolve(string userName, SlackCache slackCache)
        {
            string userId;

            if (slackCache.TryGetUserId(userName, out userId))
                return userId;

            mLog.DebugFormat("User {0} not found. Reloading cache.", userName);
            slackCache.ReloadUsers();
            slackCache.TryGetChannelId(userName, out userId);

            return userId;
        }

        static ILog mLog = LogManager.GetLogger("SlackUser");
    }
}
