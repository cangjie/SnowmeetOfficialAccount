using System;
using Microsoft.AspNetCore.Mvc;

namespace SnowmeetOfficialAccount.Models
{
    public class OAPostData
    {
        [FromQuery]
        public string signature { get; set; }
        [FromQuery]
        public string timestamp { get; set; }
        [FromQuery]
        public string nonce { get; set; }
        [FromQuery]
        public string? echostr { get; set; }
        [FromBody]
        public string postData { get; set; }
    }
}
