using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Codice.LogWrapper;

namespace SlackPlug
{
    class SlackCache
    {
        internal SlackCache(string slackToken, SemaphoreSlim plugOperationsThrottle)
        {
            mSlackToken = slackToken;
            mPlugOperationsThrottle = plugOperationsThrottle;
        }

        internal async Task<string> GetUserId(string userName)
        {
            await mUsersSemaphore.WaitAsync();
            try
            {
                string userId = FindMemberId(userName, mUsers);
                if (userId != null)
                    return userId;

                if ((DateTime.UtcNow - mUsersUpdateTime).TotalMinutes < CACHE_TRUST_TIME_IN_MINUTES)
                    return null;

                await ReloadUsers();

                return FindMemberId(userName, mUsers);
            }
            finally
            {
                mUsersSemaphore.Release();
            }
        }

        internal async Task<string> GetChannelId(string channelName)
        {
            await mChannelsSemaphore.WaitAsync();
            try
            {
                string channelId;
                if (mChannelIdByName.TryGetValue(channelName, out channelId))
                    return channelId;

                if ((DateTime.UtcNow - mChannelsUpdateTime).TotalMinutes < CACHE_TRUST_TIME_IN_MINUTES)
                    return null;

                await ReloadChannels();

                if (mChannelIdByName.TryGetValue(channelName, out channelId))
                    return channelId;

                return null;
            }
            finally
            {
                mChannelsSemaphore.Release();
            }
        }

        internal async Task Reload()
        {
            try
            {
                await mUsersSemaphore.WaitAsync();
                try
                {
                    await ReloadUsers();
                }
                finally
                {
                    mUsersSemaphore.Release();
                }


                await mChannelsSemaphore.WaitAsync();
                try
                {
                    await ReloadChannels();
                }
                finally
                {
                    mChannelsSemaphore.Release();
                }
            }
            catch (Exception e)
            {
                mLog.ErrorFormat("Error reloading users and channels: {0}", e.Message);
                mLog.Debug(e.StackTrace);
            }
        }

        async Task ReloadUsers()
        {
            mUsers = await LoadUsers(mSlackToken, mPlugOperationsThrottle);
            mUsersUpdateTime = DateTime.UtcNow;
        }

        static async Task<List<SlackAPI.Member>> LoadUsers(
            string slackToken, SemaphoreSlim plugOperationsThrottle)
        {
            if (plugOperationsThrottle != null)
                await plugOperationsThrottle.WaitAsync();
            try
            {
                int ini = Environment.TickCount;
                SlackAPI.UserList userList = await SlackAPI.ListUsers(slackToken);
                mLog.DebugFormat(
                    "Time listing slack users ({0} users): {1} ms",
                    userList.Members.Count,
                    Environment.TickCount - ini);
                return userList.Members;
            }
            finally
            {
                if (plugOperationsThrottle != null)
                    plugOperationsThrottle.Release();
            }
        }

        async Task ReloadChannels()
        {
            List<SlackAPI.Channel> channels =
                await LoadChannels(mSlackToken, mPlugOperationsThrottle);

            mChannelsUpdateTime = DateTime.UtcNow;

            mChannelIdByName.Clear();
            mChannelIdByName.EnsureCapacity(channels.Count);
            foreach (SlackAPI.Channel channel in channels)
                mChannelIdByName[channel.Name] = channel.Id;
        }

        static async Task<List<SlackAPI.Channel>> LoadChannels(
            string slackToken, SemaphoreSlim plugOperationsThrottle)
        {
            if (plugOperationsThrottle != null)
                await plugOperationsThrottle.WaitAsync();
            try
            {
                int ini = Environment.TickCount;
                SlackAPI.ChannelList channelList = await SlackAPI.ListChannels(slackToken);
                mLog.DebugFormat(
                    "Time listing slack channels ({0} num channels): {1} ms",
                    channelList.Channels.Count,
                    Environment.TickCount - ini);
                return channelList.Channels;
            }
            finally
            {
                if (plugOperationsThrottle != null)
                    plugOperationsThrottle.Release();
            }
        }

        static string FindMemberId(string userName, List<SlackAPI.Member> users)
        {
            foreach (SlackAPI.Member member in users)
            {
                if (userName == member.Name ||
                    userName == member.RealName ||
                    userName == member.Profile.Email ||
                    userName == member.Profile.DisplayName ||
                    userName == member.Profile.DisplayNameNormalized)
                {
                    return member.Id;
                }
            }

            return null;
        }

        readonly SemaphoreSlim mUsersSemaphore = new SemaphoreSlim(1);
        readonly SemaphoreSlim mChannelsSemaphore = new SemaphoreSlim(1);

        List<SlackAPI.Member> mUsers = new List<SlackAPI.Member>(0);
        DateTime mUsersUpdateTime = DateTime.MinValue;

        readonly Dictionary<string, string> mChannelIdByName = new Dictionary<string, string>();
        DateTime mChannelsUpdateTime = DateTime.MinValue;

        readonly string mSlackToken;
        readonly SemaphoreSlim mPlugOperationsThrottle;

        const int CACHE_TRUST_TIME_IN_MINUTES = 30;

        static ILog mLog = LogManager.GetLogger("SlackNotification");
    }
}
