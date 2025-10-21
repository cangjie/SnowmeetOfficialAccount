using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using SnowmeetOfficialAccount.Models;
using SnowmeetOfficialAccount;
using Azure.Core;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Runtime.CompilerServices;
using static System.Net.WebRequestMethods;
using System.Collections;
using System.Net.Sockets;
using System.Collections.Generic;

namespace SnowmeetOfficialAccount.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class OfficialAccountApi : ControllerBase
    {
        private readonly AppDBContext _context;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly MemberController _memberHelper;




        public OfficialAccountApi(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            _memberHelper = new MemberController(context, config);
        }

        [HttpGet]
        public async Task<ActionResult<string>> PushMessage(string signature,
            string timestamp, string nonce, string echostr)
        {
            return echostr.Trim();
        }

        [HttpGet]
        public async Task SendTextMessage(string unionId, string content)
        {
            unionId = Util.UrlDecode(unionId);
            content = Util.UrlDecode(content).Replace("+", " ");
            var l = await _context.user.Where(u => u.union_id.Trim().Equals(unionId.Trim()))
                .AsNoTracking().ToListAsync();
            if (l == null || l.Count == 0)
            {
                return;
            }
            string openId = l[0].open_id.Trim();
            if (openId.Trim().Equals(""))
            {
                return;
            }
            OASent msg = new OASent()
            {
                id = 0,
                MsgType = "text",
                ToUserName = openId,
                Content = content
            };
            SendServiceMessage(msg);
        }

        [NonAction]
        public string SendServiceMessage(OASent message)
        {
            string result = "";
            string token = GetAccessToken().Trim();
            //token = "88_JykS7udIvUc8lWvrtKgsmo5otlvhocE_6eXS1gEUI2Sxj08_jlMVjzb6a6wPEaeSwQ4RGsX6jzhMMKka9ty3UEQA3-6ST7y9QNjBvh6EkLvljyK7LVhGlmHHwP8DQTeAHAJWZ";
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
                    for (int i = 0; i < message.newsContentArray.Length; i++)
                    {
                        articleJson = articleJson + ((!articleJson.Trim().Equals("")) ? ", " : "")
                            + "{\"title\": \"" + message.newsContentArray[i].title.Trim() + "\", \"description\": \"" + message.newsContentArray[i].description.Trim() + "\", "
                            + "\"url\": \"" + message.newsContentArray[i].url.Trim() + "\", \"picurl\": \"" + message.newsContentArray[i].picUrl.Trim() + "\" } ";
                    }
                    messageJson = "\"msgtype\": \"news\", \"news\": {\"articles\": [" + articleJson + "]}";
                    break;
                case "miniprogrampage":
                    messageJson = "\"msgtype\": \"miniprogrampage\", \"miniprogrampage\": {\"title\":\"" + message.newsContentArray[0].title.Trim() + "\", \"appid\": \"wxd1310896f2aa68bb\" "
                        + ",  \"pagepath\":\"" + message.newsContentArray[0].url.Trim() + "\", \"thumb_media_id\": \"" + message.newsContentArray[0].picUrl + "\" }";
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
            //return "79_z6Q-cZ0EXeoRUOQwxtQ1qtn0N0Dk8zI6XyOB5pxzR2WexXb5WqUjOcglscaiWFMmOhOBRMLvBf_Y1ikbelem58BjJ4EgQWMFVDwJc9vISwJfmXHDO0pWVdKt5YYBDTaAIAGIY";
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
                if (ts.TotalSeconds > 900)
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
        public async Task<ActionResult<string>> PushMessage([FromQuery] string signature,
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
                //return NoContent(); 
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
                        await fw.WriteLineAsync(signature);
                        await fw.WriteLineAsync(timestamp);
                        await fw.WriteLineAsync(nonce);
                        await fw.WriteLineAsync(body.Trim());
                        await fw.WriteLineAsync("");
                        fw.Close();
                    }
                }

            }
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
            ret = await DealMessage(msg);
            return ret;
        }

        [HttpGet]
        public async Task SyncMemberInfo(string openId)
        {
            var msaList = await _context.memberSocailAccount
                .Where(m => (m.type.Trim().Equals("wechat_oa_openid") && m.num.Trim().Equals(openId)))
                .AsNoTracking().ToListAsync();
            int memberId = 0;
            string unionId = "";
            if (msaList != null && msaList.Count > 0)
            {
                memberId = msaList[0].member_id;
            }

            if (memberId == 0)
            {
                UserInfo info = GetUserInfoFromWechat(openId.Trim());
                unionId = info.unionid.Trim();
                if (unionId != null && !unionId.Trim().Equals(""))
                {
                    msaList = await _context.memberSocailAccount
                        .Where(m => (m.type.Trim().Equals("wechat_unionid") && m.num.Trim().Equals(unionId)))
                        .AsNoTracking().ToListAsync();
                    if (msaList != null && msaList.Count > 0)
                    {
                        memberId = msaList[0].member_id;
                    }
                }
            }
            if (memberId == 0)
            {
                Member member = new Member()
                {
                    id = 0,
                };
                MemberSocialAccount msaOpenId = new MemberSocialAccount()
                {
                    id = 0,
                    member_id = 0,
                    type = "wechat_oa_openid",
                    num = openId.Trim(),
                    valid = 1
                };
                UserInfo info = GetUserInfoFromWechat(openId.Trim());
                MemberSocialAccount msaUnionId = new MemberSocialAccount()
                {
                    id = 0,
                    member_id = 0,
                    type = "wechat_unionid",
                    num = info.unionid.Trim(),
                    valid = 1
                };
                member.memberSocialAccounts.Add(msaOpenId);
                if (!msaUnionId.num.Trim().Equals(""))
                {
                    member.memberSocialAccounts.Add(msaUnionId);
                }
                await _context.member.AddAsync(member);
                await _context.SaveChangesAsync();
            }
            else
            {
                var memberList = await _context.member.Include(m => m.memberSocialAccounts)
                .Where(m => m.id == memberId).AsNoTracking().ToListAsync();
                if (memberList == null || memberList.Count == 0)
                {
                    return;
                }

                if (memberList[0].wechatUnionId == null || memberList[0].wechatUnionId.Trim().Equals(""))
                {

                    UserInfo info = GetUserInfoFromWechat(openId.Trim());
                    if (!info.unionid.Trim().Equals(""))
                    {
                        MemberSocialAccount msaUnionId = new MemberSocialAccount()
                        {
                            id = 0,
                            member_id = memberId,
                            type = "wechat_unionid",
                            num = info.unionid.Trim(),
                            valid = 1
                        };
                        await _context.memberSocailAccount.AddAsync(msaUnionId);
                        await _context.SaveChangesAsync();
                    }
                }
                if (memberList[0].oaOpenId == null || memberList[0].oaOpenId.Trim().Equals(""))
                {
                    MemberSocialAccount msaOaOpenId = new MemberSocialAccount()
                    {
                        id = 0,
                        member_id = memberId,
                        type = "wechat_oa_openid",
                        num = openId.Trim(),
                        valid = 1
                    };
                    await _context.memberSocailAccount.AddAsync(msaOaOpenId);
                    await _context.SaveChangesAsync();
                }
            }

        }
        [NonAction]
        public UserInfo GetUserInfoFromWechat(string openId)
        {
            string accessToken = GetAccessToken();
            //accessToken = "86_ZQ8BBf0416xcMbDq_cEmNHj3F0OQGzpkqJE5wfbhDM1yKi3bkOn6-5X-v3o5VBIHfW07FIfCHOh4XeqL4VW8SemnheOyld6hmOuhyAmyb2c4q0ao0QPGK8ORLX0WSAdACASOW";
            string url = "https://api.weixin.qq.com/cgi-bin/user/info?access_token=" + accessToken.Trim()
                    + "&openid=" + openId.Trim() + "&lang=zh_CN";
            string ret = Util.GetWebContent(url);
            UserInfo info = JsonConvert.DeserializeObject<UserInfo>(ret);
            return info;
        }
        [HttpGet]
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
            //await SyncMemberInfo(receiveMsg.FromUserName.Trim());
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
            string msg = "";
            receiveMsg.Content = receiveMsg.EventKey.ToLower();
            if (receiveMsg.Content.StartsWith("我要入职"))
            {
                msg = "您目前还不是易龙雪聚员工，<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\"/pages/register/staff_check_in\" >点此查看个人信息</a>。";
            }
            if (!msg.Trim().Equals(""))
            {
                OASent reply = new OASent()
                {
                    id = 0,
                    FromUserName = receiveMsg.ToUserName.Trim(),
                    ToUserName = receiveMsg.FromUserName.Trim(),
                    MsgType = "text",
                    Content = msg.Trim(),
                    origin_message_id = receiveMsg.id,
                    is_service = 0
                };
                ret = reply.GetXmlDocument().InnerXml.Trim();
                await _context.oASent.AddAsync(reply);
                await _context.SaveChangesAsync();
            }
            return ret;
        }
        [NonAction]
        public async Task<string> DealEventMessage(OARecevie receiveMsg)
        {
            string ret = "success";
            receiveMsg.Content = receiveMsg.EventKey.ToLower();
            switch (receiveMsg.Event.ToLower().Trim())
            {
                case "scan":
                    await SetFollowingStatus(receiveMsg.FromUserName.Trim(), true);
                    ret = await DealScanMessage(receiveMsg);
                    break;
                case "subscribe":
                    await SetFollowingStatus(receiveMsg.FromUserName.Trim(), true);
                    ret = await DealSubscribeMessage(receiveMsg);
                    break;
                case "unsubscribe":
                    await SetFollowingStatus(receiveMsg.FromUserName.Trim(), false);
                    break;
                default:
                    ret = await DealCommonMessage(receiveMsg);
                    break;
            }
            return ret;
        }
        [NonAction]
        public async Task SetFollowingStatus(string openId, bool following)
        {
            MemberSocialAccount msa = await _context.memberSocailAccount
                .Where(m => (m.type.Trim().Equals("wechat_oa_openid") && m.num.Trim().Equals(openId.Trim()) && m.valid == 1))
                .AsNoTracking().FirstOrDefaultAsync();
            if (msa == null)
            {
                return;
            }
            Member member = await _context.member.Where(m => (m.id == msa.member_id)).AsNoTracking().FirstOrDefaultAsync();
            if (member == null)
            {
                return;
            }
            member.following_wechat = following ? 1 : 0;
            member.update_date = DateTime.Now;
            _context.member.Entry(member).State = EntityState.Modified;
            CoreDataModLog log = new CoreDataModLog()
            {
                id = 0,
                table_name = "member",
                field_name = "following_wechat",
                prev_value = (following ? 0 : 1).ToString(),
                current_value = (following ? 1 : 0).ToString(),
                is_manual = 1,
                manual_memo = "用户关注",
                create_date = DateTime.Now,
                key_value = member.id,
                scene = "公众号操作"

            };
            await _context.dataLog.AddAsync(log);
            int i = await _context.SaveChangesAsync();
            //Console.WriteLine(i.ToString());
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
        public async Task<string> DealGetTicket(OARecevie receiveMsg, string[] keyArr)
        {
            int templateId = int.Parse(keyArr[1]);
            string channel = keyArr[2].Trim();
            MemberController _memberHelper = new MemberController(_context, _config);
            Member member = await _memberHelper.GetMemberByOfficialAccountOpenId(receiveMsg.FromUserName, "领取优惠券");
            if (member == null)
            {
                return null;
            }
            bool canGenerate = false;
            string failReason = "";
            DateTime expireDate = DateTime.Parse("2026-04-30");
            switch (templateId)
            {
                case 12:
                    List<Ticket> tickets = await _context.ticket
                        .Where(t => t.template_id == templateId && t.member_id == member.id
                            && t.create_date.Date == DateTime.Now.Date && t.valid == 1)
                        .AsNoTracking().ToListAsync();
                    if (tickets == null || tickets.Count == 0)
                    {
                        canGenerate = true;
                    }
                    else
                    {
                        failReason = "该优惠券，每天只能领取一次";
                    }
                    break;
                default:
                    canGenerate = true;
                    break;
            }
            TicketTemplate template = await _context.ticketTemplate.Where(t => t.id == templateId)
                    .AsNoTracking().FirstOrDefaultAsync();
            string pic = "https://wxoa.snowmeet.top/0.png";
            //string url = "<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\"pages/mine/ticket/ticket_list\" href=\"#\" >点击查看</a>";
            string url = "https://snowmeet.wanlonghuaxue.com/mapp/open_mapp_page.html?path=pages/mine/ticket/ticket_list&query=&version=trail";
            if (canGenerate)
            {
                string code = Util.GetRandomCode(9);
                Ticket dupTicket = await _context.ticket.Where(t => t.code.Trim().Equals(code.Trim()))
                    .AsNoTracking().FirstOrDefaultAsync();
                for (int times = 0; times < 100 && dupTicket != null; times++)
                {
                    code = Util.GetRandomCode(9);
                    dupTicket = await _context.ticket.Where(t => t.code.Trim().Equals(code.Trim()))
                        .AsNoTracking().FirstOrDefaultAsync();
                }

                Ticket ticket = new Ticket()
                {
                    code = code.Trim(),
                    name = template.name,
                    memo = template.memo,
                    open_id = receiveMsg.FromUserName.Trim(),
                    member_id = member.id,
                    oper_open_id = receiveMsg.FromUserName.Trim(),
                    shared = 0,
                    printed = 0,
                    used = 0,
                    template_id = template.id,
                    miniapp_recept_path = "",
                    create_date = DateTime.Now,
                    channel = channel,
                    valid = 1,
                    expire_date = expireDate
                };
                await _context.AddAsync(ticket);
                await _context.SaveChangesAsync();
                string title = template.name.Trim() + " 已经领取成功";
                //string pic = "";
                //string url = "";
                OASent sent = new OASent()
                {
                    id = 0,
                    is_service = 0,
                    FromUserName = _settings.originalId.Trim(),
                    ToUserName = receiveMsg.FromUserName,
                    origin_message_id = receiveMsg.id,
                    MsgType = "news",
                    newsContentArray = new OASent.NewsContent[] { new OASent.NewsContent()
                    {
                        title = title,
                        picUrl = pic,
                        description = "领取成功，请在本雪季使用。",
                        url = url
                    } }
                };
                sent.Content = sent.GetXmlDocument().InnerXml.Trim();
                await _context.oASent.AddAsync(sent);
                await _context.SaveChangesAsync();
                return sent.Content;
            }
            else
            {
                string title = template.name.Trim();// + " " + failReason;
                //string pic = "";
                //string url = "";
                OASent sent = new OASent()
                {
                    id = 0,
                    is_service = 0,
                    FromUserName = _settings.originalId.Trim(),
                    ToUserName = receiveMsg.FromUserName,
                    origin_message_id = receiveMsg.id,
                    MsgType = "news",
                    newsContentArray = new OASent.NewsContent[] { new OASent.NewsContent()
                    {
                        title = title,
                        picUrl = pic,
                        description = failReason,
                        url = url
                    } }
                };
                sent.Content = sent.GetXmlDocument().InnerXml.Trim();
                await _context.oASent.AddAsync(sent);
                await _context.SaveChangesAsync();
                return sent.Content;
            }
        }
        [NonAction]
        public async Task<string> ImportTicket(OARecevie receiveMsg, string[] keyArr)
        {
            return null;
        }
        [NonAction]
        public async Task<string> DealEventKeyAction(OARecevie receiveMsg, string key)
        {
            string ret = "success";
            receiveMsg.EventKey = receiveMsg.EventKey.ToLower();
            if (receiveMsg.EventKey.IndexOf(key) < 0)
            {
                return ret;
            }
            string[] keyArr = key.Trim().Split('_');
            switch (keyArr[0].Trim())
            {
                case "getticket":
                    ret = await DealGetTicket(receiveMsg, keyArr);
                    break;
                case "import_ticket":
                    ret = await ImportTicket(receiveMsg, keyArr);
                    break;
                case "me":
                    ret = await Me(receiveMsg, keyArr);
                    break;
                case "confirm":
                case "pay":
                    ret = await DealPaymentAction(receiveMsg, keyArr);
                    break;
                case "recept":
                    ret = await ScanReceptNew(receiveMsg, keyArr);
                    break;
                case "shop":
                case "nanshanskipass":
                case "maintainreturn":
                    ret = await ScanRecept(receiveMsg, keyArr);
                    await SendMaintainPickVerCode(int.Parse(keyArr[keyArr.Length - 1]));
                    break;
                case "nanshanreserve":
                    ret = await NanshanReserve(receiveMsg, keyArr);
                    break;
                case "wanlong":
                    if (keyArr[1].Equals("trainer") && keyArr[2].Equals("reg"))
                    {
                        ret = await WanlongTrainReg(receiveMsg);
                    }
                    break;
                case "snowmeet":
                    if (keyArr[1].Equals("staff") && keyArr[2].Equals("reg"))
                    {
                        ret = await SnowmeetStaffReg(receiveMsg);
                    }
                    break;
                case "oper":
                    if (keyArr[1].Trim().Equals("ticket"))
                    {
                        ret = await ScanTicket(receiveMsg);
                    }
                    break;
                case "ticketactivity":
                    ret = await TicketActivity(receiveMsg);
                    break;
                case "reserveskipass":
                    ret = await ReserveSkipass(receiveMsg);
                    break;
                case "contact":
                    ret = await GetContact(receiveMsg);
                    break;
                default:
                    if (keyArr[0].StartsWith("3"))
                    {
                        string skiPassCode = keyArr[0].Substring(1, keyArr[0].Length - 1);
                        string msg = "雪票：" + skiPassCode.Trim() + "，<a href='http://weixin.snowmeet.top/pages/admin/wechat/card_confirm.aspx?code=" + skiPassCode + "' >点击验证</a>";
                        OASent reply = new OASent()
                        {
                            id = 0,
                            FromUserName = receiveMsg.ToUserName.Trim(),
                            ToUserName = receiveMsg.FromUserName.Trim(),
                            MsgType = "text",
                            Content = msg.Trim(),
                            origin_message_id = receiveMsg.id,
                            is_service = 0
                        };
                        ret = reply.GetXmlDocument().InnerXml.Trim();

                    }
                    break;
            }
            return ret;
        }
        [NonAction]
        public async Task<string> Me(OARecevie receiveMsg, string[] kArr)
        {
            string channel = kArr[1].Trim() + "_" + kArr[2].Trim();
            OASent.NewsContent news = new OASent.NewsContent()
            {
                url = "pages/tickets/me_pick?templateId=15&channel=" + channel,
                picUrl = "gltv7fpLtJQg_sTpVwzJY9cPiXuZfG91MKnJwscUqdikZhRDtHtrDeo-MiFdzebg",
                title = "点击领取"
            };
            OASent sent = new OASent()
            {
                id = 0,
                FromUserName = receiveMsg.ToUserName,
                ToUserName = receiveMsg.FromUserName,
                MsgType = "miniprogrampage",
                newsContentArray = new OASent.NewsContent[] { news }
            };
            SendServiceMessage(sent);
            return "success";
        }
        [NonAction]
        public async Task<string> GetContact(OARecevie receiveMsg)
        {
            string content = "联系方式 南山店 17800191050；崇礼旗舰店 13910228351";
            return await GetSendTextMessageXml(content, receiveMsg);
        }
        [NonAction]
        public async Task<string> ReserveSkipass(OARecevie receiveMsg)
        {
            string[] eventArr = receiveMsg.EventKey.Split('_');
            int memberId = 0;
            string resort = "万龙";
            if (eventArr.Length == 3)
            {
                memberId = int.Parse(eventArr[2].Trim());
                resort = eventArr[1].Trim();
            }
            else
            {
                memberId = int.Parse(eventArr[1].Trim());
            }

            string content = "订雪票，送打蜡。请<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\"pages/ski_pass/ski_pass_selector?resort="
                + Util.UrlEncode(resort) + "&memberId=" + memberId + "\" href=\"#\" >点击此处</a>进入小程序操作。";
            return await GetSendTextMessageXml(content, receiveMsg);
        }

        [NonAction]
        public async Task<string> GetSendTextMessageXml(string content, OARecevie receiveMsg)
        {
            OASent reply = new OASent()
            {
                id = 0,
                FromUserName = receiveMsg.ToUserName.Trim(),
                ToUserName = receiveMsg.FromUserName.Trim(),
                MsgType = "text",
                Content = content.Trim(),
                origin_message_id = receiveMsg.id,
                is_service = 0
            };

            await _context.oASent.AddAsync(reply);
            await _context.SaveChangesAsync();

            string ret = reply.GetXmlDocument().InnerXml.Trim();

            return ret;
        }

        [NonAction]
        public async Task<string> TicketActivity(OARecevie receiveMsg)
        {
            string[] eventArr = receiveMsg.EventKey.Split('_');
            int ticketTemplateId = int.Parse(eventArr[eventArr.Length - 1]);
            string source = eventArr[eventArr.Length - 2].Trim();

            Member member = await _memberHelper.GetMember(receiveMsg.FromUserName, "wechat_oa_openId");
            if (member == null)
            {
                return "success";
            }
            bool haveJoined = false;
            if (member.wechatMiniOpenId == null || member.wechatMiniOpenId.Trim().Equals(""))
            {
                haveJoined = false;
            }
            else
            {
                var ticketList = await _context.ticket
                    .Where(t => t.template_id == ticketTemplateId
                    && t.open_id.Trim().Equals(member.wechatMiniOpenId.Trim()))
                    .AsNoTracking().ToListAsync();
                if (ticketList != null && ticketList.Count > 0)
                {
                    haveJoined = true;
                }

            }
            string content = "";
            if (!haveJoined)
            {
                TicketTemplate tt = await _context.ticketTemplate.FindAsync(ticketTemplateId);
                content = "感谢您的关注，易龙雪聚" + tt.name + "，请<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\""
                   + tt.miniapp_recept_path + "&source=" + Util.UrlEncode(source) + "\" >" + "点击此链接" + "</a>领取。";
            }
            else
            {
                content = "感谢您对易龙雪聚的支持与关注，此项活动您无需重复参加。";
            }
            OASent reply = new OASent()
            {
                id = 0,
                FromUserName = receiveMsg.ToUserName.Trim(),
                ToUserName = receiveMsg.FromUserName.Trim(),
                MsgType = "text",
                Content = content.Trim(),
                origin_message_id = receiveMsg.id,
                is_service = 0
            };

            await _context.oASent.AddAsync(reply);
            await _context.SaveChangesAsync();

            string ret = reply.GetXmlDocument().InnerXml.Trim();

            return ret;
            //return "";
        }


        [NonAction]
        public async Task<string> ScanTicket(OARecevie receiveMsg)
        {
            string[] keyArr = receiveMsg.EventKey.Trim().Split('_');
            string code = keyArr[keyArr.Length - 1].Trim();
            string openId = receiveMsg.FromUserName.Trim();
            Member member = await _memberHelper.GetMember(openId, "wechat_oa_openid");
            Ticket ticket = await _context.ticket.FindAsync(code);
            if (ticket == null)
            {
                return "success";
            }
            string content = "";
            if (ticket.open_id.Trim().Equals(""))
            {
                content = "绑定此券，请<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\"pages/mine/ticket/ticket_bind?ticketCode=" + ticket.code.Trim()
                + "\" href=\"#\" >点击此处</a>进入小程序操作。";

                if (member.is_admin == 1 || member.is_manager == 1 || member.is_staff == 1)
                {
                    if (!ticket.miniapp_recept_path.Trim().Equals(""))
                    {
                        content = content + "      如果是客人出示并要使用此" + ticket.name.Trim() + "，请<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\""
                        + ticket.miniapp_recept_path.Trim() + "?ticketCode=" + ticket.code.Trim() + "\" href=\"#\"  >进入此处</a>为其操作。";
                    }
                    else
                    {
                        content = "客人使用此" + ticket.name.Trim() + "，具体内容：" + ticket.memo.Trim()
                            + "请<a  href=\"http://weixin.snowmeet.top/pages/admin/wechat/use_ticket.aspx?code=" + ticket.code.Trim() + "\"  >进入此处</a>为其核销。";
                    }
                }
            }
            else
            {
                if (member.is_admin == 1 || member.is_manager == 1 || member.is_staff == 1)
                {
                    if (!ticket.miniapp_recept_path.Trim().Equals(""))
                    {
                        content = "客人使用此" + ticket.name.Trim() + "，请<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\""
                            + ticket.miniapp_recept_path.ToString() + "?ticketCode=" + ticket.code.Trim() + "\" href=\"#\"  >进入此处</a>为其操作。";
                    }
                    else
                    {
                        content = "客人使用此" + ticket.name.Trim() + "，具体内容：" + ticket.memo.Trim()
                            + "请<a  href=\"http://weixin.snowmeet.top/pages/admin/wechat/use_ticket.aspx?code=" + ticket.code.Trim() + "\"  >进入此处</a>为其核销。";
                    }

                }
                else
                {
                    content = "此券已经被其他用户添加至其个人账户。";
                }
            }
            OASent reply = new OASent()
            {
                id = 0,
                FromUserName = receiveMsg.ToUserName.Trim(),
                ToUserName = receiveMsg.FromUserName.Trim(),
                MsgType = "text",
                Content = content.Trim(),
                origin_message_id = receiveMsg.id,
                is_service = 0
            };

            await _context.oASent.AddAsync(reply);
            await _context.SaveChangesAsync();

            string ret = reply.GetXmlDocument().InnerXml.Trim();

            return ret;
        }

        [NonAction]
        public async Task<string> WanlongTrainReg(OARecevie receiveMsg)
        {
            string msg = "万龙教练请<a data-miniprogram-appid=\"wx00d9526056641d74\" data-miniprogram-path=\"/pages/admin/admin\" >点击注册</a>安全快乐滑雪系统";
            string ret = "success";
            OASent reply = new OASent()
            {
                id = 0,
                FromUserName = receiveMsg.ToUserName.Trim(),
                ToUserName = receiveMsg.FromUserName.Trim(),
                MsgType = "text",
                Content = msg.Trim(),
                origin_message_id = receiveMsg.id,
                is_service = 0
            };

            await _context.oASent.AddAsync(reply);
            await _context.SaveChangesAsync();

            ret = reply.GetXmlDocument().InnerXml.Trim();

            return ret;

        }

        [NonAction]
        public async Task<string> SnowmeetStaffReg(OARecevie receiveMsg)
        {
            string msg = "易龙雪聚新员工请<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\"/pages/admin/staff_reg\" >点击注册</a>后，联系管理员开通权限。";
            string ret = "success";
            OASent reply = new OASent()
            {
                id = 0,
                FromUserName = receiveMsg.ToUserName.Trim(),
                ToUserName = receiveMsg.FromUserName.Trim(),
                MsgType = "text",
                Content = msg.Trim(),
                origin_message_id = receiveMsg.id,
                is_service = 0
            };

            await _context.oASent.AddAsync(reply);
            await _context.SaveChangesAsync();

            ret = reply.GetXmlDocument().InnerXml.Trim();

            return ret;

        }
        [NonAction]
        public async Task<string> ScanReceptNew(OARecevie receiveMsg, string[] keyArr)
        {
            int id = int.Parse(keyArr[keyArr.Length - 1].Trim());
            ScanQrCode scanQrCode = await _context.scanQrCode.Where(s => s.id == id)
                .AsNoTracking().FirstOrDefaultAsync();
            if (scanQrCode == null)
            {
                return "success";
            }
            scanQrCode.scaned = 1;
            scanQrCode.scan_time = DateTime.Now;

            MemberController _memberHelper = new MemberController(_context, _config);
            Member member = await _memberHelper.GetMemberByOfficialAccountOpenId(receiveMsg.FromUserName, "店铺接待，扫码关注公众号");
            if (member != null)
            {
                scanQrCode.scaner_member_id = member.id;
                if (member.cell != null)
                {
                    scanQrCode.cell = member.cell.Trim();
                }
            }
            _context.Entry(scanQrCode).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            string url = "";
            string pic = "";
            string title = "";
            if (member.isNew)
            {
                title = "恭喜注册为易龙雪聚的新会员";
                pic = "https://mini.snowmeet.top/images/welcome_new.jpg";
                url = "weixin://dl/business/?appid=wxd1310896f2aa68bb&path=pages/register/reg";
            }
            else if (member.cell == null)
            {
                title = "请验证您的手机号";
                pic = "https://mini.snowmeet.top/images/need_to_veri_num.jpg";
                url = "weixin://dl/business/?appid=wxd1310896f2aa68bb&path=pages/register/reg";
            }
            else
            {
                title = "请等待店员开单";
                pic = "https://mini.snowmeet.top/images/wait.jpg";
                url = "";
            }
            OASent sent = new OASent()
            {
                id = 0,
                is_service = 0,
                FromUserName = _settings.originalId.Trim(),
                ToUserName = receiveMsg.FromUserName,
                origin_message_id = receiveMsg.id,
                MsgType = "news",
                newsContentArray = new OASent.NewsContent[] { new OASent.NewsContent()
                {
                    title = title,
                    picUrl = pic,
                    description = "",
                    url = url
                } }
            };
            sent.Content = sent.GetXmlDocument().InnerXml.Trim();
            await _context.oASent.AddAsync(sent);
            await _context.SaveChangesAsync();
            return sent.Content;
        }
        [NonAction]
        public async Task<string> ScanRecept(OARecevie receiveMsg, string[] keyArr)
        {
            string ret = "success";
            Member member = await _memberHelper.GetMember(receiveMsg.FromUserName, "wechat_oa_openid");
            int id = int.Parse(keyArr[keyArr.Length - 1].Trim());
            ShopSaleInteract scan = await _context.shopSaleInteract.FindAsync(id);
            scan.scan = 1;
            scan.scaner_oa_open_id = receiveMsg.FromUserName.Trim();//user.open_id.Trim();
            scan.scaner_union_id = member.GetNum("wechat_unionid");
            _context.Entry(scan).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            string message = "";
            bool isMember = (member.GetNum("cell").Trim().Equals("") ? false : true)
                && (member.GetNum("wechat_mini_openid").Trim().Equals("") ? false : true);

            switch (scan.scan_type.Trim())
            {
                case "nanshanskipass":
                    if (!isMember)
                    {
                        message = "您目前还不是易龙雪聚会员，<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\"/pages/register/register\" >点此注册</a>。";
                    }
                    else
                    {
                        message = "请稍后，等待店员取票。";
                    }
                    break;
                case "觅计划旗舰引流":
                    message = "欢迎光临，请等待店员查看您的优惠券。";
                    break;
                case "发板":
                    message = "请等待店员确认。";
                    break;
                default:
                    message = "欢迎回来，请等待店员开单。";
                    if (keyArr[1].Trim().Equals("maintain"))
                    {
                        message = "请等待店员核验身份。";
                    }
                    if (!isMember)
                    {
                        message = "您目前还不是易龙雪聚会员，<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\"/pages/register/register\" >点此注册</a>。";
                    }
                    break;
            }
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
            ret = reply.GetXmlDocument().InnerXml.Trim();
            return ret;
        }

        [NonAction]
        public async Task<string> NanshanReserve(OARecevie receiveMsg, string[] keyArr)
        {
            string path = "/pages/ski_pass/ski_pass_reserve?id=" + keyArr[1] + "&date=" + keyArr[2];
            string content = "南山雪票，临时购买。<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\"" + path + "\" >点击这里支付</a>";
            string ret = "success";
            OASent reply = new OASent()
            {
                id = 0,
                FromUserName = receiveMsg.ToUserName.Trim(),
                ToUserName = receiveMsg.FromUserName.Trim(),
                MsgType = "text",
                Content = content.Trim(),
                origin_message_id = receiveMsg.id,
                is_service = 0
            };

            await _context.oASent.AddAsync(reply);
            await _context.SaveChangesAsync();

            ret = reply.GetXmlDocument().InnerXml.Trim();

            return ret;
        }

        [NonAction]
        public async Task<string> GetTextMessageXml(OARecevie receiveMsg, string content)
        {
            string ret = "success";
            OASent reply = new OASent()
            {
                id = 0,
                FromUserName = receiveMsg.ToUserName.Trim(),
                ToUserName = receiveMsg.FromUserName.Trim(),
                MsgType = "text",
                Content = content.Trim(),
                origin_message_id = receiveMsg.id,
                is_service = 0
            };

            await _context.oASent.AddAsync(reply);
            await _context.SaveChangesAsync();

            ret = reply.GetXmlDocument().InnerXml.Trim();

            return ret;
        }

        [NonAction]
        public async Task<string> DealPaymentAction(OARecevie receiveMsg, string[] keyArr)
        {
            int id = int.Parse(keyArr[keyArr.Length - 1].Trim());
            if (keyArr.Length >= 3 && keyArr[1].Equals("rent") && keyArr[2].Equals("add"))
            {
                return await DealRentAddPayment(receiveMsg, keyArr);
            }
            string message = "您有一笔费用需要支付。";
            string miniAppPath = "/pages/payment/pay_hub?paymentId=" + id.ToString();// + "&item=" + item.Trim();
            if (keyArr.Length > 2 && keyArr[1].Trim().ToLower().Equals("recept"))
            {
                miniAppPath = "/pages/payment/pay_recept?id=" + id.ToString();
            }
            message = message + "<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\"" + miniAppPath + "\" >点击这里查看</a>。";
            return await GetTextMessageXml(receiveMsg, message);
        }
        [NonAction]
        public async Task<string> DealRentAddPayment(OARecevie receiveMsg, string[] keyArr)
        {
            int id = int.Parse(keyArr[keyArr.Length - 1].Trim());
            RentAdditionalPayment addPay = await _context.rentAdditionalPayment.FindAsync(id);
            if (addPay == null)
            {
                return "";
            }
            string miniAppPath = "/pages/payment/rent_pay_add?id=" + id.ToString();
            string msg = "您的租赁订单需要补交一笔费用。原因：" + addPay.reason.Trim() + " 金额：" + addPay.amount.ToString() + "元。";
            msg += "<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\"" + miniAppPath + "\" >点击这里支付</a>。";
            return await GetTextMessageXml(receiveMsg, msg);
        }
        [HttpGet]
        public string GetOAQRCodeUrl(string content)
        {
            string jsonStr = "{\"expire_seconds\": 604800, \"action_name\": \"QR_STR_SCENE\", \"action_info\": {\"scene\": {\"scene_str\": \"" + content.Trim() + "\"}}}";
            string postUrl = "https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token=" + GetAccessToken().Trim();
            string ret = Util.GetWebContent(postUrl, jsonStr);
            OAQRTicket t = JsonConvert.DeserializeObject<OAQRTicket>(ret);
            return "https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket=" + t.ticket.Trim();
        }
        [HttpGet]
        public ActionResult<string> GetOALimitQrCode(string content, string token)
        {
            string sysToken = GetAccessToken().Trim();
            if (token.Trim().Equals(sysToken.Trim()) == false)
            {
                return NoContent();
            }
            string jsonStr = "{ \"action_name\": \"QR_LIMIT_STR_SCENE\", \"action_info\": {\"scene\": {\"scene_str\": \"" + content.Trim() + "\"}}}";
            string postUrl = "https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token=" + sysToken;
            string ret = Util.GetWebContent(postUrl, jsonStr);
            OAQRTicket t = JsonConvert.DeserializeObject<OAQRTicket>(ret);
            return Ok("https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket=" + t.ticket.Trim());
        }
        [NonAction]
        public string[] GetSubscribedOpenId()
        {
            ArrayList openIdArr = new ArrayList();
            string token = GetAccessToken();
            //token = "79_caAgFn0npGT0DIlNzxViDSTRheqtIDN7MWuwJcY41F1YhJz7czKmr0yvxyVDpnaZoV_Sio1xBgM0fujnL1MAZUfH8vWDc-ZQ0lI6pN2M_zOuRUN16oraMwpflxABYJeAEATQB";
            string nextOpenId = "";
            string url = "https://api.weixin.qq.com/cgi-bin/user/get?access_token=" + token;
            string content = Util.GetWebContent(url);
            SubscribedOpenIdSet openIdSet = JsonConvert.DeserializeObject<SubscribedOpenIdSet>(content);
            nextOpenId = openIdSet.next_openid.Trim();
            for (; openIdSet.data != null && openIdSet.data.openid.Length > 0;)
            {
                for (int i = 0; i < openIdSet.data.openid.Length; i++)
                {
                    openIdArr.Add(openIdSet.data.openid[i].Trim());
                }
                content = Util.GetWebContent(url + "&next_openid=" + openIdSet.next_openid);
                openIdSet = JsonConvert.DeserializeObject<SubscribedOpenIdSet>(content);

            }
            string[] ret = new string[openIdArr.Count];

            for (int i = 0; i < openIdArr.Count; i++)
            {
                ret[i] = openIdArr[i].ToString();
            }

            return ret;

        }

        [HttpGet]
        public async Task GetUserInfo()
        {
            string[] openIdArr = GetSubscribedOpenId();
            string token = GetAccessToken();
            for (int i = 0; i < openIdArr.Length; i++)
            {
                Console.WriteLine(i.ToString());
                string openId = openIdArr[i].Trim();
                OAUserInfo savedInfo = await _context.oaUserInfo.FindAsync(openId.Trim());
                if (savedInfo != null)
                {
                    continue;
                }
                string url = "https://api.weixin.qq.com/cgi-bin/user/info?access_token=" + token + "&openid=" + openId + "&lang=zh_CN";
                string content = Util.GetWebContent(url);
                OAUserInfo info = JsonConvert.DeserializeObject<OAUserInfo>(content);
                info.tagid_list_str = "";
                await _context.oaUserInfo.AddAsync(info);
                await _context.SaveChangesAsync();



            }

        }
        [NonAction]
        public async Task<WebApiLog> PerformRequest(string url, string header, string payload,
            string method = "GET", string source = "易龙雪聚小程序", string purpose = "", string memo = "")
        {
            WebApiLog log = new WebApiLog()
            {
                id = 0,
                source = source.Trim(),
                purpose = purpose.Trim(),
                memo = memo.Trim(),
                method = method.Trim(),
                header = header.Trim(),
                payload = payload.Trim(),
                request_url = url.Trim()
            };
            await _context.webApiLog.AddAsync(log);
            await _context.SaveChangesAsync();
            try
            {
                switch (method.ToLower())
                {
                    case "post":
                        log.response = Util.GetWebContent(log.request_url, log.payload, "application/json");
                        break;
                    default:
                        log.response = Util.GetWebContent(log.request_url);
                        break;
                }
            }
            catch
            {

            }
            log.deal = 1;
            log.update_date = DateTime.Now;
            _context.webApiLog.Entry(log).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return log;
        }
        [HttpGet]
        public async Task SendMaintainPickVerCode(int scanId)
        {
            ShopSaleInteract scan = await _context.shopSaleInteract.FindAsync(scanId);
            if (scan.biz_id == null)
            {
                return;
            }
            MaintainLive task = await _context.maintainLive.FindAsync(scan.biz_id);
            if (task == null)
            {
                return;
            }
            List<MemberSocialAccount> taskMsaList = await _context.memberSocailAccount
                .Where(m => (m.num.Trim().Equals(task.open_id.Trim()) && m.type.Trim().Equals("wechat_mini_openid")))
                .AsNoTracking().ToListAsync();
            if (taskMsaList.Count == 0)
            {
                return;
            }
            List<MemberSocialAccount> scanMsaList = await _context.memberSocailAccount
                .Where(m => (m.num.Trim().Equals(scan.scaner_oa_open_id) && m.type.Trim().Equals("wechat_oa_openid")))
                .AsNoTracking().ToListAsync();
            if (scanMsaList.Count == 0)
            {
                return;
            }
            if (scanMsaList[0].member_id == taskMsaList[0].member_id)
            {
                return;
            }
            List<MemberSocialAccount> oaList = await _context.memberSocailAccount
                .Where(m => (m.member_id == taskMsaList[0].member_id && m.type.Trim().Equals("wechat_oa_openid")))
                .AsNoTracking().ToListAsync();
            if (oaList.Count == 0)
            {
                return;
            }
            string equipName = task.confirmed_brand.Trim() + " " + task.confirmed_equip_type + " 长度：" + task.confirmed_scale.Trim();
            string openId = oaList[0].num.Trim();
            string veriCode = Util.CreateVerifyCode(6);
            task.pick_veri_code = veriCode.Trim();
            _context.maintainLive.Entry(task).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            string url = "https://api.weixin.qq.com/cgi-bin/message/template/send?access_token=" + GetAccessToken().Trim();
            string json = "{"
                + "\"touser\": \"" + openId.Trim() + "\", "
                + "\"template_id\": \"-FxfVcWYFq079YIWfaT6khxQn6__b-CD9Xty_M_iP1U\", "
                + "\"url\": \"https://mini.snowmeet.top\", "
                + "\"mini_program\": {"
                + "\"appid\": \"wxd1310896f2aa68bb\", "
                + "\"pagepath\": \"pages/index\" "
                + " }, "
                + "\"data\": {"
                + "\"character_string1\":{\"value\": \"" + task.task_flow_num.Trim() + "\"}, "
                + "\"thing6\":{\"value\": \"" + equipName.Trim() + "\"}, "
                + "\"character_string5\":{\"value\": \"" + veriCode + "\"} "
                + "} }";
            await PerformRequest(url, "", json, "POST", "易龙雪聚公众号", "取板发送验证码", veriCode);
        }
        protected class SubscribedOpenIdSet
        {
            public int total { get; set; }
            public int count { get; set; }
            public DataSet data { get; set; }
            public string next_openid { get; set; }

            public class DataSet
            {
                public string[] openid { get; set; }
            }
        }

        protected class OAQRTicket
        {
            public string ticket { get; set; } = "";
        }

        public class UserInfo
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
