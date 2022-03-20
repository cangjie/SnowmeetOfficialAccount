using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using LuqinOfficialAccount.Models;
namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class OfficialAccountApi:ControllerBase
    {
        private readonly AppDBContext _context;

        public OfficialAccountApi(AppDBContext context)
        {
            _context = context;
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
            string path = $"{Environment.CurrentDirectory}";

            if (path.StartsWith("/"))
            {
                path = path + "/";
            }
            else
            {
                path = path + "\\";
            }
            path = path + "wechat_post.txt";
            using (StreamWriter fw = new StreamWriter(path, true))
            {
                fw.WriteLine(body.ToString());
                fw.Close();
            }
            return "";
        }

    }
}
