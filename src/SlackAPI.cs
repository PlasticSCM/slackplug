using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using Newtonsoft.Json;

namespace SlackPlug
{
    internal static class SlackAPI
    {
        public class PostMessageResult
        {
            [JsonProperty("ok")]
            public bool IsOk;

            [JsonProperty("error")]
            public string ErrorMessage;
        }

        public class ImOpenResponse
        {
            [JsonProperty("ok")]
            public bool IsOk;

            [JsonProperty("error")]
            public string ErrorMessage;

            [JsonProperty("channel")]
            public Channel Channel { get; set; }
        }

        public class Channel
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        public class ChannelList
        {
            [JsonProperty("channels")]
            public List<Channel> Channels { get; set; }
        }

        public class UserList
        {
            [JsonProperty("members")]
            public List<Member> Members { get; set; }
        }

        public class Member
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("real_name")]
            public string RealName { get; set; }

            [JsonProperty("profile")]
            public Profile Profile { get; set; }
        }

        public class Profile
        {
            [JsonProperty("display_name")]
            public string DisplayName { get; set; }

            [JsonProperty("display_name_normalized")]
            public string DisplayNameNormalized { get; set; }

            [JsonProperty("email")]
            public string Email { get; set; }
        }

        internal static UserList ListUsers(string token)
        {
            WebRequest request = BuildWebRequest(USER_LIST, token);
            using (WebResponse response = GetWebResponse(request))
            {
                return DeserializeResponse<UserList>(response);
            }
        }

        internal static ChannelList ListChannels(string token)
        {
            WebRequest request = BuildWebRequest(CHANNELS_LIST, token);

            using (WebResponse response = GetWebResponse(request))
            {
                return DeserializeResponse<ChannelList>(response);
            }
        }

        internal static string OpenIm(string token, string userId)
        {
            WebRequest request = BuildWebRequest(IM_OPEN, token, userId);

            using (WebResponse response = GetWebResponse(request))
            {
                ImOpenResponse result = DeserializeResponse<ImOpenResponse>(response);

                if (result.IsOk)
                    return result.Channel.Id;

                throw new Exception(string.Format(
                    "Opening im to '{0}' failed due to the following reason: {1}",
                    userId, result.ErrorMessage));
            }
        }

        internal static void PostChatMessage(string token, string channel, string text)
        {
            WebRequest request = BuildWebRequest(
                CHAT_POSTMESSAGE,
                token,
                channel,
                Ellipsize(text, MAX_MESSAGE_LENGTH));

            using (WebResponse response = GetWebResponse(request))
            {
                PostMessageResult result = DeserializeResponse<PostMessageResult>(response);

                if (result.IsOk)
                    return;

                throw new Exception(string.Format(
                    "Posting chat message to '{0}' failed due to the following reason: {1}",
                    channel, result.ErrorMessage));
            }
        }

        static WebResponse GetWebResponse(WebRequest webRequest)
        {
            webRequest.Timeout = TIMEOUT;
            return webRequest.GetResponse();
        }

        static WebRequest BuildWebRequest(string baseUri, params object[] args)
        {
            Uri uri = new Uri(string.Format(baseUri, args));
            return WebRequest.Create(uri);
        }

        static T DeserializeResponse<T>(WebResponse response)
        {
            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
            {
                string raw = stream.ReadToEnd();

                return string.IsNullOrEmpty(raw)
                    ? default(T)
                    : JsonConvert.DeserializeObject<T>(raw);
            }
        }

        static string Ellipsize(string str, int maxLength)
        {
            if (str.Length <= maxLength)
                return str;

            return str.Substring(0, maxLength - 3) + "...";
        }

        const int MAX_MESSAGE_LENGTH = 800;

        const int TIMEOUT = 15000;

        const string BASE_URI = "https://slack.com/api";
        const string IM_OPEN = BASE_URI + "/im.open?token={0}&user={1}";
        const string USER_LIST = BASE_URI + "/users.list?token={0}";
        const string CHANNELS_LIST = BASE_URI + "/channels.list?token={0}";
        const string CHAT_POSTMESSAGE = BASE_URI + "/chat.postMessage?token={0}&channel={1}&text={2}";
    }
}
