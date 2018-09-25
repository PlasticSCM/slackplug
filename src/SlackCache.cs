using System;

namespace SlackPlug
{
    class SlackCache
    {
        internal SlackCache(string slackToken)
        {
            mSlackToken = slackToken;
        }

        internal bool TryGetUserId(string userName, out string userId)
        {
            userId = null;

            lock(mUsersLock)
            {
                foreach (SlackAPI.Member member in mUsers.Members)
                {
                    if (userName == member.Name ||
                        userName == member.RealName ||
                        userName == member.Profile.Email ||
                        userName == member.Profile.DisplayName ||
                        userName == member.Profile.DisplayNameNormalized)
                    {
                        userId = member.Id;
                        return true;
                    }
                }
            }

            return false;
        }

        internal bool TryGetChannelId(string channelName, out string channelId)
        {
            channelId = null;

            lock(mChannelsLock)
            {
                foreach (SlackAPI.Channel channel in mChannels.Channels)
                {
                    if (channelName == channel.Name)
                    {
                        channelId = channel.Id;
                        return true;
                    }
                }
            }

            return false;
        }

        internal void Reload()
        {
            ReloadUsers();
            ReloadChannels();
        }

        internal void ReloadUsers()
        {
            lock (mUsersLock)
            {
                mUsers = SlackAPI.ListUsers(mSlackToken);
            }
        }

        internal void ReloadChannels()
        {
            lock (mChannelsLock)
            {
                mChannels = SlackAPI.ListChannels(mSlackToken);
            }
        }

        object mUsersLock = new object();
        object mChannelsLock = new object();

        SlackAPI.UserList mUsers;
        SlackAPI.ChannelList mChannels;

        readonly string mSlackToken;
    }
}
