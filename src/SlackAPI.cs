using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Codice.LogWrapper;
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
            [JsonProperty("response_metadata")]
            public ResponseMetadata Metadata { get; set; }
        }

        public class UserList
        {
            [JsonProperty("members")]
            public List<Member> Members { get; set; }
            [JsonProperty("response_metadata")]
            public ResponseMetadata Metadata { get; set; }
        }

        public class ResponseMetadata
        {
            [JsonProperty("next_cursor")]
            public string NextCursor { get; set; }
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

        async internal static Task<UserList> ListUsers(string token)
        {
            string nextCursor = string.Empty;
            List<Member> users = new List<Member>();

            do
            {
                WebRequest request;
                if (string.IsNullOrEmpty(nextCursor))
                    request = BuildWebRequest(USER_LIST);
                else
                    request = BuildWebRequest(USER_LIST + SLACK_GET_CURSOR_FORMAT, nextCursor);

                request.Headers[HttpRequestHeader.Authorization] = GetBearer(token);
                using(WebResponse response = await GetWebResponseAsync(request))
                {
                    string sResponse = await ResponseToString (response);
                    UserList responseResult = JsonConvert.DeserializeObject<UserList>(sResponse);
                    if (responseResult != null && responseResult.Members != null)
                    {
                        users.AddRange(responseResult.Members);
                        nextCursor = responseResult.Metadata.NextCursor;
                    }
                    else
                    {
                        mLog.Error("Slack users can't be loaded.");
                        mLog.DebugFormat("Slack API response: {0}", sResponse);
                        nextCursor = string.Empty;
                    }
                }
            }
            while (!string.IsNullOrEmpty(nextCursor));

            return new UserList() { Members = users };
        }

        async internal static Task<ChannelList> ListChannels(string token)
        {
            string nextCursor = string.Empty;
            List<Channel> channels = new List<Channel>();

            do
            {
                WebRequest request;
                if (string.IsNullOrEmpty(nextCursor))
                    request = BuildWebRequest(CHANNELS_LIST);
                else
                    request = BuildWebRequest(CHANNELS_LIST + SLACK_GET_CURSOR_FORMAT, nextCursor);

                request.Headers[HttpRequestHeader.Authorization] = GetBearer(token);

                using (WebResponse response = await GetWebResponseAsync(request))
                {
                    string sResponse = await ResponseToString (response);
                    ChannelList responseResult =
                        JsonConvert.DeserializeObject<ChannelList>(sResponse);
                    if (responseResult != null && responseResult.Channels != null)
                    {
                        channels.AddRange(responseResult.Channels);
                        nextCursor = responseResult.Metadata.NextCursor;
                    }
                    else
                    {
                        mLog.Error("Slack channels can't be loaded.");
                        mLog.DebugFormat("Slack API response: {0}", sResponse);
                        nextCursor = string.Empty;
                    }
                }
            }
            while (!string.IsNullOrEmpty(nextCursor));

            return new ChannelList() { Channels = channels };
        }

        async internal static Task PostChatMessage(string token, string channel, string text)
        {
            HttpWebRequest request = BuildPostRequest(CHAT_POSTMESSAGE, token);
            await WriteDataToRequest(request, GetSerializedData(channel,text));

            try
            {
                using (HttpWebResponse response =  (HttpWebResponse)await request.GetResponseAsync())
                {
                    PostMessageResult result =
                        await DeserializeResponse <PostMessageResult>(response);

                    if (result.IsOk)
                        return;

                    throw new Exception(string.Format(
                        "Posting chat message to '{0}' failed due to the following reason: {1}",
                        channel, result.ErrorMessage));
                }
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        throw new Exception(streamReader.ReadToEnd());
                    }
                }
            }
        }

        static string GetSerializedData(string channel, string text)
        {
            Dictionary<string, string> body = new Dictionary<string, string>();
            body[CHANNEL_KEY] = channel;
            body[TEXT_KEY] = text;
            return JsonConvert.SerializeObject(body);
        }

        async static Task WriteDataToRequest(HttpWebRequest request, string data)
        {
            using (StreamWriter requestStream =
                new StreamWriter(await request.GetRequestStreamAsync()))
            {
                await requestStream.WriteAsync(data);
            }
        }

        async static Task<WebResponse> GetWebResponseAsync(WebRequest webRequest)
        {
            webRequest.Timeout = TIMEOUT;
            return await webRequest.GetResponseAsync();
        }

        static WebRequest BuildWebRequest(string uri)
        {
            return WebRequest.Create(new Uri(uri));
        }

        static WebRequest BuildWebRequest(string baseUri, params object[] args)
        {
            Uri uri = new Uri(string.Format(baseUri, args));
            return WebRequest.Create(uri);
        }

        static HttpWebRequest BuildPostRequest(string endPointUri, string token)
        {
            HttpWebRequest result = (HttpWebRequest)WebRequest.Create(new Uri(endPointUri));
            result.ContentType = "application/json";
            result.Method = "POST";
            result.Timeout = 5000;
            result.Headers.Add(
                HttpRequestHeader.Authorization,
                GetBearer(token));

            return result;
        }

        async static Task<string> ResponseToString(WebResponse response)
        {
            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                return await stream.ReadToEndAsync();
        }

        async static Task<T> DeserializeResponse<T>(WebResponse response)
        {
            string raw = await ResponseToString(response);

            return string.IsNullOrEmpty(raw)
                ? default(T)
                : JsonConvert.DeserializeObject<T>(raw);
        }

        static string GetBearer(string token)
        {
            return string.Format(BEARER, token);
        }

        const int TIMEOUT = 15000;

        const string BASE_URI = "https://slack.com/api";
        const string USER_LIST = BASE_URI + "/users.list?limit=500";
        const string CHANNELS_LIST = BASE_URI + "/conversations.list?limit=500&types=public_channel,private_channel&exclude_archived=true";
        const string CHAT_POSTMESSAGE = BASE_URI + "/chat.postMessage";
        const string SLACK_GET_CURSOR_FORMAT = "&cursor={0}";
        const string BEARER = "Bearer {0}";
        const string CHANNEL_KEY = "channel";
        const string TEXT_KEY = "text";

        static ILog mLog = LogManager.GetLogger("SlackNotification");
    }
}
