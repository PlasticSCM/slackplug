using System.Threading.Tasks;

namespace SlackPlug
{
    static class SlackUser
    {
        async internal static Task<string> GetId(SlackId recipient, SlackCache slackCache)
        {
            if (recipient.Attrs.IsSlackID)
                return recipient.Id;

            return await slackCache.GetUserId(recipient.Id);
        }
    }
}
