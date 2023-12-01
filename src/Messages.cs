using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SlackPlug
{
    static class Messages
    {
        internal static string BuildRegisterPlugMessage(string name, string type)
        {
            JObject obj = new JObject(
                new JProperty("action", "register"),
                new JProperty("type", type),
                new JProperty("name", name));

            return obj.ToString();
        }

        internal static string BuildLoginMessage(string organization, string token)
        {
            JObject obj = new JObject(
                new JProperty("action", "login"),
                new JProperty("key", token),
                new JProperty("organization", organization));

            return obj.ToString();
        }

        internal static NotificationMessage ReadNotificationMessage(string message)
        {
            return JsonConvert.DeserializeObject<NotificationMessage>(message);
        }

        internal static string BuildErrorResponse(string requestId, string message)
        {
            return new JObject(
                new JProperty("requestId", requestId),
                new JProperty("error", message)).ToString();
        }

        internal static string BuildSuccessfulResponse(string requestId)
        {
            return new JObject(new JProperty("requestId", requestId)).ToString();
        }

        internal static string GetRequestId(string message)
        {
            return JObject.Parse(message).Value<string>("requestId");
        }
    }

    class NotificationMessage
    {
        public string Message;
        public List<string> Recipients;
    }
}
