﻿using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Configuration;
//using LuqinOfficialAccount.Models;
using System.Security.Cryptography;
using SnowmeetOfficialAccount.Models;
using SnowmeetOfficialAccount;
using Azure.Core;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SnowmeetOfficialAccount.Controllers
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
        [NonAction]
        public string SendServiceMessage(OASent message)
        {
            string result = "";
            string token = GetAccessToken().Trim();
            string sentUrl = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token=" + token.Trim();
            string postJson = "";
            string messageJson = "";
            switch (message.MsgType.Trim())
            {
                case "video":
                    messageJson = "\"msgtype\": \"video\", \"video\": {\"media_id\":\"" + message.Content.Trim() + "\" "
                        + ",  \"thumb_media_id\":\"" + message.Thumb.Trim() + "\" }"; //, \"title\": \"test\", \"description\":\"aaa\"  }";
                                                                                      //+ " }";
                    break;
                case "news":
                    string articleJson = "";
                    for (int i = 0; i < message.newContentArray.Length; i++)
                    {
                        articleJson = articleJson + ((!articleJson.Trim().Equals("")) ? ", " : "")
                            + "{\"title\": \"" + message.newContentArray[i].title.Trim() + "\", \"description\": \"" + message.newContentArray[i].description.Trim() + "\", "
                            + "\"url\": \"" + message.newContentArray[i].url.Trim() + "\", \"picurl\": \"" + message.newContentArray[i].picUrl.Trim() + "\" } ";
                    }
                    messageJson = "\"msgtype\": \"news\", \"news\": {\"articles\": [" + articleJson + "]}";
                    break;
                case "text":
                default:
                    messageJson = "\"msgtype\": \"text\", \"text\": {\"content\":\"" + message.Content.Trim().Replace("\"", "'") + "\" }";
                    break;
            }
            postJson = "{\"touser\":\"" + message.ToUserName.Trim() + "\", " + messageJson.Trim() + " }";
            string resultJson = Util.GetWebContent(sentUrl, postJson);
            ApiResult resultObj = JsonConvert.DeserializeObject<ApiResult>(resultJson);
            message.err_code = resultObj.errcode.ToString();
            message.err_msg = resultObj.errmsg.ToString();
            message.is_service = 1;
            message.origin_message_id = 0;
            try
            {
                _context.oASent.Add(message);
                _context.SaveChanges();
            }
            catch
            {

            }
            return result.Trim();
        }


        [HttpGet]
        public void RefreshAccessToken()
        {
            GetAccessToken();
        }

        [NonAction]
        public string GetAccessToken()
        {
            string tokenFilePath = $"{Environment.CurrentDirectory}";
            tokenFilePath = tokenFilePath + "/access_token.official_account";
            string token = "";
            string tokenTime = Util.GetLongTimeStamp(DateTime.Parse("1970-1-1"));
            string nowTime = Util.GetLongTimeStamp(DateTime.Now);
            bool fileExists = false;
            if (System.IO.File.Exists(tokenFilePath))
            {
                fileExists = true;
                using (StreamReader sr = new StreamReader(tokenFilePath))
                {
                    try
                    {
                        token = sr.ReadLine();
                    }
                    catch
                    {

                    }
                    try
                    {
                        tokenTime = sr.ReadLine();
                    }
                    catch
                    {

                    }
                    sr.Close();
                }
                long timeDiff = long.Parse(nowTime) - long.Parse(tokenTime);
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, (int)timeDiff);
                //TimeSpan ts = new TimeSpan()
                if (ts.TotalSeconds > 3600)
                {
                    token = "";
                    if (fileExists)
                    {
                        try
                        {
                            System.IO.File.Delete(tokenFilePath);
                        }
                        catch
                        {

                        }
                    }
                }
                else
                {
                    return token.Trim();
                    //return "";
                }
            }
            string getTokenUrl = "https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid="
                + _settings.appId.Trim() + "&secret=" + _settings.appSecret.Trim();

            string ret = Util.GetWebContent(getTokenUrl);
            AccessToken at = JsonConvert.DeserializeObject<AccessToken>(ret);
            if (!at.access_token.Trim().Equals(""))
            {
                //System.IO.File.AppendAllText(tokenFilePath, at.access_token + "\r\n" + nowTime);
                System.IO.File.WriteAllText(tokenFilePath, at.access_token + "\r\n" + nowTime);
                return at.access_token.Trim();
                //return "";
            }
            else
            {
                return "";
            }
            

        }

        [HttpPost]
        public async Task<ActionResult<string>> PushMessage([FromQuery]string signature,
            [FromQuery] string timestamp, [FromQuery] string nonce)
        {
            string ret = "success";
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
        
                using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
                {
                    body = await reader.ReadToEndAsync();

                    string path = $"{Environment.CurrentDirectory}";
                    string dateStr = DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString().PadLeft(2, '0')
                        + DateTime.Now.Day.ToString().PadLeft(2, '0');
                    if (path.StartsWith("/"))
                    {
                        path = path + "/";
                    }
                    else
                    {
                        path = path + "\\";
                    }
                    path = path + "wechat_post_" + dateStr + ".txt";
                    using (StreamWriter fw = new StreamWriter(path, true))
                    {
                        fw.WriteLine(body.Trim());
                        fw.Close();
                    }
                }
                
            }
            try
            {
                XmlDocument xmlD = new XmlDocument();
                xmlD.LoadXml(body);
                XmlNode root = xmlD.SelectSingleNode("//xml");

                string eventStr = "";
                string eventKey = "";
                string content = "";
                string msgId = "";
                string msgType = root.SelectSingleNode("MsgType").InnerText.Trim();

                if (msgType.Trim().Equals("event"))
                {
                    eventStr = root.SelectSingleNode("Event").InnerText.Trim();
                    eventKey = root.SelectSingleNode("EventKey").InnerText.Trim();
                }
                else
                {
                    content = root.SelectSingleNode("Content").InnerText.Trim();
                    msgId = root.SelectSingleNode("MsgId").InnerText.Trim();
                    msgType = root.SelectSingleNode("MsgType").InnerText.Trim();
                }

                OARecevie msg = new OARecevie()
                {
                    id = 0,
                    ToUserName = root.SelectSingleNode("ToUserName").InnerText.Trim(),
                    FromUserName = root.SelectSingleNode("FromUserName").InnerText.Trim(),
                    CreateTime = root.SelectSingleNode("CreateTime").InnerText.Trim(),
                    MsgType = msgType,
                    Event = eventStr,
                    EventKey = eventKey,
                    MsgId = msgId,
                    Content = content

                };

                await _context.oARecevie.AddAsync(msg);
                await _context.SaveChangesAsync();
                try
                {
                    await SyncUserInfo(msg.FromUserName.Trim());
                }
                catch
                { 
                
                }
                ret = await DealMessage(msg);

            }
            catch
            {

            }


            return ret;
        }

        [NonAction]
        public async Task<User> SyncUserInfo(string openId)
        {
            User user = await _context.user.FindAsync(openId);
            if (user == null)
            {
                user = new User()
                {
                    open_id = openId.Trim()
                };
                await _context.user.AddAsync(user);
                await _context.SaveChangesAsync();
            }
            try
            {
                if (user.union_id.Trim().Equals(""))
                {
                    string accessToken = GetAccessToken();
                    string url = "https://api.weixin.qq.com/cgi-bin/user/info?access_token=" + accessToken.Trim()
                    + "&openid=" + openId.Trim() + "&lang=zh_CN";
                    string ret = Util.GetWebContent(url);
                    UserInfo info = JsonConvert.DeserializeObject<UserInfo>(ret);
                    user.union_id = info.unionid;
                    _context.Entry(user).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
            }
            catch
            { 
            
            }
            try
            {
                if (user.member_id == 0 && !user.union_id.Trim().Equals(""))
                {
                    var miniList = await _context.miniUser
                        .Where(m => (m.union_id.Trim().Equals(user.union_id)))
                        .ToListAsync();
                    if (miniList != null && miniList.Count > 0)
                    {
                        user.member_id = miniList[0].member_id;
                        _context.Entry(user).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch
            { 
            
            }
            return user;
        }

        [HttpGet]
        public async Task<ActionResult<string>> DealMessageTest(int id)
        {
            OARecevie msg = await _context.oARecevie.FindAsync(id);
            string ret = await DealMessage(msg);
            return Ok(ret);
        }

        [NonAction]
        public async Task<string> DealMessage(OARecevie receiveMsg)
        {
            string ret = "success";
            switch (receiveMsg.MsgType.Trim().ToLower())
            {
                case "text":
                    ret = await DealTextMessage(receiveMsg);
                    break;
                case "event":
                    ret = await DealEventMessage(receiveMsg);
                    break;
                default:
                    break;
            }

            return ret;
        }

        [NonAction]
        public async Task<string> DealTextMessage(OARecevie receiveMsg)
        {
            string ret = "success";
            
            return ret;
        }

        [NonAction]
        public async Task<string> DealEventMessage(OARecevie receiveMsg)
        {
            string ret = "success";

            switch (receiveMsg.Event.ToLower().Trim())
            {
                case "scan":
                    ret = await DealScanMessage(receiveMsg);
                    break;
                case "subscribe":
                    ret = await DealSubscribeMessage(receiveMsg);
                    break;
                default:
                    ret = await DealCommonMessage(receiveMsg);
                    break;
            }

            return ret;
        }

        [NonAction]
        public async Task<string> DealCommonMessage(OARecevie receiveMsg)
        {
            string ret = "success";

            return ret;
        }

        [NonAction]
        public async Task<string> DealScanMessage(OARecevie receiveMsg)
        {
            string ret = "success";
            ret = await DealEventKeyAction(receiveMsg, receiveMsg.EventKey.ToLower());
            return ret;
        }

        [NonAction]
        public async Task<string> DealSubscribeMessage(OARecevie receiveMsg)
        {
            string ret = "success";

            if (receiveMsg.EventKey.ToLower().StartsWith("qrscene"))
            {
                ret = await DealEventKeyAction(receiveMsg, receiveMsg.EventKey.ToLower().Replace("qrscene_", ""));
            }
            else
            {

            }

            return ret;
        }

        [NonAction]
        public async Task<string> DealEventKeyAction(OARecevie receiveMsg, string key)
        {
            string ret = "success";
            if (receiveMsg.EventKey.IndexOf(key) < 0)
            {
                return ret;
            }
            string[] keyArr = key.Trim().Split('_');
            switch (keyArr[0].Trim())
            {
                case "pay":
                    ret = await DealPaymentAction(receiveMsg, keyArr);
                    break;
                default:
                    break;
            }
            return ret;
        }

        [NonAction]
        public async Task<string> DealPaymentAction(OARecevie receiveMsg, string[] keyArr)
        {
            int id = int.Parse(keyArr[keyArr.Length - 1].Trim());
            string message = "您有一笔费用需要支付。";
            string miniAppPath = "/pages/payment/pay_hub?paymentId=" + id.ToString();// + "&item=" + item.Trim();
            message = message + "<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\"" + miniAppPath + "\" >点击这里查看</a>。";
            string ret = "success";
            OASent reply = new OASent()
            {
                id = 0,
                FromUserName = receiveMsg.ToUserName.Trim(),
                ToUserName = receiveMsg.FromUserName.Trim(),
                MsgType = "text",
                Content = message.Trim(),
                origin_message_id = receiveMsg.id,
                is_service = 0
            };

            await _context.oASent.AddAsync(reply);
            await _context.SaveChangesAsync();

            ret = reply.GetXmlDocument().InnerXml.Trim();

            return ret;
        }

        protected class UserInfo
        {
            public int subscribe = 0;
            public string openid = "";
            public string language = "";
            public long subscribe_time = 0;
            public string unionid = "";
            public string remark = "";
            public int groupid = 0;
            public int[] tagid_list = new int[] { 0, 0 };
            public string subscribe_scene = "";
            public string qr_scene = "";
            public string qr_scene_str = "";
        }

        protected class UserToken
        {
            public string access_token = "";
            public int expires_in = 0;
            public string refresh_token = "";
            public string openid = "";
            public string scope = "";
        }

        protected class AccessToken
        {
            public string access_token = "";
            public int expires_in = 0;

        }

        protected class ApiResult
        {
            public int errcode = -1;
            public string errmsg = "";
        }

        public class DraftResult
        {
            public int total_count = 0;
            public int item_count = 0;
            public DraftItem[] item;
            public class DraftItem
            {
                public string media_id = "";
                public int update_time;
                public ContentStrct content;
                public class ContentStrct
                {

                    public int create_time = 0;
                    public int update_time = 0;
                    public NewsItem[] news_item;
                    public class NewsItem
                    {
                        public string title = "";
                        public string url = "";
                        public string thumb_url = "";
                    }
                }
            }
        }
    }
}
