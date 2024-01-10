using System;

using System.Text.RegularExpressions;

namespace SlackPlug
{
    class SlackId
    {
        internal readonly Attributes Attrs;
        internal readonly string Id;

        internal class Attributes
        {
            internal bool IsChannel;
            internal bool IsSlackID;
        }

        SlackId(string id, Attributes attributes)
        {
            Id = id;
            Attrs = attributes;
        }

        internal static SlackId Build(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException();

            if (input.StartsWith("#"))
            {
                // #development
                return new SlackId(input.Substring(1), new Attributes()
                {
                    IsChannel = true,
                    IsSlackID = false
                });
            }

            if (input.StartsWith("@"))
            {
                // @john
                return new SlackId(input.Substring(1), new Attributes()
                {
                    IsChannel = false,
                    IsSlackID = false
                });
            }

            if (IsSlackChannelId(input))
            {
                // C039V9P8X
                return new SlackId(input, new Attributes()
                {
                    IsChannel = true,
                    IsSlackID = true
                });
            }

            if (IsSlackUserId(input))
            {
                // U0498AXGU
                return new SlackId(input, new Attributes()
                {
                    IsChannel = false,
                    IsSlackID = true
                });
            }

            // john
            return new SlackId(input, new Attributes()
            {
                IsChannel = false,
                IsSlackID = false
            });
        }

        static bool IsSlackChannelId(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return mChannelRegex.IsMatch(input);
        }

        static bool IsSlackUserId(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return mUserRegex.IsMatch(input);
        }

        const string CHANNEL_REGEX_PATTERN = "^[C][A-Z0-9]+$";
        const string USER_REGEX_PATTERN = "^[UW][A-Z0-9]+$";

        static Regex mUserRegex = new Regex(USER_REGEX_PATTERN);
        static Regex mChannelRegex = new Regex(CHANNEL_REGEX_PATTERN);
    }
}
