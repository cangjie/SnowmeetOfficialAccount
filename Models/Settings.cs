using System;
using Microsoft.Extensions.Configuration;
namespace LuqinOfficialAccount.Models
{
    public class Settings
    {
        public string? appId { get; set; }
        public string? appSecret { get; set; }
        public string? originalId { get; set; }
        public string? token { get; set; }

        public static Settings GetSettings(IConfiguration config)
        {
            IConfiguration settings = config.GetSection("Settings");
            return new Settings()
            {
                appId = settings.GetSection("AppId").Value.Trim(),
                appSecret = settings.GetSection("AppSecret").Value.Trim(), 
                originalId = settings.GetSection("OriginalId").Value.Trim(),
                token = settings.GetSection("token").Value.Trim() 
            };
        }
    }
}
