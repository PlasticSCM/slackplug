using System.Threading.Tasks;

namespace SlackPlug
{
    internal static class SlackChannel
    {
        async internal static Task<string> GetId(SlackId slackId, SlackCache cache)
        {
            if (slackId.Attrs.IsSlackID)
                return slackId.Id;

            return await cache.GetChannelId(slackId.Id);
        }
    }
}
