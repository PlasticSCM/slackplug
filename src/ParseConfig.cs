using System;

namespace SlackPlug
{
    class ParseConfig
    {
        internal static Config Parse(string config)
        {
            try
            {
                Config result = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(config);

                if (result == null)
                    throw new Exception(string.Format(
                        "Config '{0}' is not valid", config));

                CheckFieldIsNotEmpty("slackToken", result.SlackToken);

                return result;
            }
            catch (Exception e)
            {
                throw new Exception("The config cannot be loaded. Error: " + e.Message);
            }
        }       
        
        static void CheckFieldIsNotEmpty(string fieldName, string fieldValue)
        {
            if (!string.IsNullOrEmpty(fieldValue))
                return;

            throw BuildFieldNotDefinedException(fieldName);
        }
        
        static Exception BuildFieldNotDefinedException(string fieldName)
        {
            throw new Exception(string.Format(
                "The field '{0}' must be defined in the config", fieldName));
        }        
    }
}