using log4net;

namespace SlackPlug
{
    internal static class SlackChannel
    {
        internal static string GetId(SlackId slackId, SlackCache cache)
        {
            if (slackId.Attrs.IsSlackID)
                return slackId.Id;

            return Resolve(slackId.Id, cache);
        }

        static string Resolve(string channelName, SlackCache slackCache)
        {
            string channelId;

            if (slackCache.TryGetChannelId(channelName, out channelId))
                return channelId;

            mLog.DebugFormat("Channel {0} not found. Reloading cache.", channelName);
            slackCache.ReloadChannels();
            slackCache.TryGetChannelId(channelName, out channelId);

            return channelId;
        }

        static ILog mLog = LogManager.GetLogger("SlackChannel");
    }
}
