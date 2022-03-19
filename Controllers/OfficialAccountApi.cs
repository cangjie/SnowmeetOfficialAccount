using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;
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
        public async Task<ActionResult<string>> PushMessage(string signature,
            string timestamp, string nonce, string echostr, object postData)
        {
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
                fw.WriteLine(postData.ToString());
                fw.Close();
            }
            return "";
        }

    }
}
