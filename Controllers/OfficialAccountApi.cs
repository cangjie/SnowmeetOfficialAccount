using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Configuration;
using LuqinOfficialAccount.Models;
using System.Security.Cryptography;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class OfficialAccountApi:ControllerBase
    {
        private readonly AppDBContext _context;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        public OfficialAccountApi(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }

        [HttpGet]
        public async Task<ActionResult<string>> PushMessage(string signature,
            string timestamp, string nonce, string echostr)
        {
            return echostr.Trim();
        }

        [HttpPost]
        public async Task<ActionResult<string>> PushMessage([FromQuery]string signature,
            [FromQuery] string timestamp, [FromQuery] string nonce)
        {
            string[] validStringArr = new string[] { _settings.token.Trim(), timestamp.Trim(), nonce.Trim() };
            Array.Sort(validStringArr);
            string validString = String.Join("", validStringArr);
            SHA1 sha = SHA1.Create();
            ASCIIEncoding enc = new ASCIIEncoding();
            byte[] bArr = enc.GetBytes(validString);
            bArr = sha.ComputeHash(bArr);
            string validResult = "";
            for (int i = 0; i < bArr.Length; i++)
            {
                validResult = validResult + bArr[i].ToString("x").PadLeft(2, '0');
            }
            if (validResult != signature)
            {
                return NoContent();
                
            }
            string body = "";
            var stream = Request.Body;
            if (stream != null)
            {
                //stream.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
                {
                    body = await reader.ReadToEndAsync();
                }
                //stream.Seek(0, SeekOrigin.Begin);
            }
            XmlDocument xmlD = new XmlDocument();
            xmlD.LoadXml(body);
            XmlNode root = xmlD.SelectSingleNode("//xml");

            OARecevie msg = new OARecevie()
            {
                id = 0,
                ToUserName = root.SelectSingleNode("ToUserName").InnerText.Trim(),
                FromUserName = root.SelectSingleNode("FromUserName").InnerText.Trim(),
                CreateTime = root.SelectSingleNode("CreateTime").InnerText.Trim(),
                MsgType = root.SelectSingleNode("MsgType").InnerText.Trim(),
                Event = root.SelectSingleNode("Event").InnerText.Trim(),
                EventKey = root.SelectSingleNode("EventKey").InnerText.Trim()

            };
            await _context.oARecevie.AddAsync(msg);
            await _context.SaveChangesAsync();
            return "success";
        }
    }
}
