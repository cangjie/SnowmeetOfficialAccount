using System;
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
using System.Runtime.CompilerServices;

namespace SnowmeetOfficialAccount.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class OfficialAccountApi:ControllerBase
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
                if (ts.TotalSeconds > 300)
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
                    await SyncMemberInfo(msg.FromUserName.Trim());
                    //await SyncUserInfo(msg.FromUserName.Trim());
                    //ret = "suc";
                }
                catch(Exception err)
                {
                    ret = err.ToString().Trim();
                }
                ret = await DealMessage(msg);

            }
            catch
            {

            }


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

            

            /*
            else
            {
                for(int i = 0; i < msaList.Count; i++)
                {
                    if (msaList[i].type.Trim().Equals("wechat_unionid"))
                    {
                        unionId = msaList[i].num.Trim();
                        break;
                    }
                }
            }
            */
            


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
                unionId = memberList[0].wechatUnionId.Trim();
                if (unionId == null || unionId.Trim().Equals(""))
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
            //accessToken = "86_l5pwLyxZyNxM-OIRPJnjxuFd57Q5tsD5BExtCfAZXMuq8ioJLyds_YbjEsknJjGd6LFOPkImk2kOx18cPmly1LMh6fCo5v2ZuKfInUhcSsTSM8bwzyOSP6jgHMsFGEeABAYZX";
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
                case "recept":
                    ret = await ScanRecept(receiveMsg, keyArr);
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
            string msg = "易龙雪聚新员请<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\"/pages/admin/staff_reg\" >点击注册</a>后，联系管理员开通权限。";
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
        public async Task<string> ScanRecept(OARecevie receiveMsg, string[] keyArr)
        {
            string ret = "success";



            /*

            User user = await _context.user.FindAsync(receiveMsg.FromUserName.Trim());
            if (user == null)
            {
                return ret;
            }
            */

            Member member = await _memberHelper.GetMember(receiveMsg.FromUserName, "wechat_oa_openid");



            int id = int.Parse(keyArr[keyArr.Length - 1].Trim());
            ShopSaleInteract scan = await _context.shopSaleInteract.FindAsync(id);
            scan.scan = 1;

            


            scan.scaner_oa_open_id = receiveMsg.FromUserName.Trim() ;//user.open_id.Trim();


            /*
            if (user.union_id == null || user.union_id.Trim().Equals(""))
            {
                user = (await SyncUserInfo(user.open_id.Trim()));
            }
            */
            scan.scaner_union_id = member.GetNum("wechat_unionid");




            _context.Entry(scan).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            /*
            var miniUserList = await _context.miniUser.Where(m => m.union_id.Trim().Equals(user.union_id.Trim())).ToListAsync();
            bool isMember = false;
            //string cell = "";
            if (miniUserList != null && miniUserList.Count > 0)
            {
                if (miniUserList[0].cell_number != null && miniUserList[0].cell_number.Length == 11)
                {
                    isMember = true;
                }
            }
            */
            bool isMember = member.GetNum("cell").Trim().Equals("") ? false : true;
            string message = "欢迎回来，请等待店员开单。";
            if (keyArr[1].Trim().Equals("maintain"))
            {
                message = "请等待店员核验身份。";
            }
            if (!isMember)
            {
                message = "您目前还不是易龙雪聚会员，<a data-miniprogram-appid=\"wxd1310896f2aa68bb\" data-miniprogram-path=\"/pages/register/register\" >点此注册</a>。";
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
        public async Task<string> DealPaymentAction(OARecevie receiveMsg, string[] keyArr)
        {
            int id = int.Parse(keyArr[keyArr.Length - 1].Trim());
            string message = "您有一笔费用需要支付。";
            string miniAppPath = "/pages/payment/pay_hub?paymentId=" + id.ToString();// + "&item=" + item.Trim();
            if (keyArr.Length > 2 && keyArr[1].Trim().ToLower().Equals("recept"))
            {
                miniAppPath = "/pages/payment/pay_recept?id=" + id.ToString();
            }
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

        [HttpGet]
        public string GetOAQRCodeUrl(string content)
        {
            string jsonStr = "{\"expire_seconds\": 604800, \"action_name\": \"QR_STR_SCENE\", \"action_info\": {\"scene\": {\"scene_str\": \"" + content.Trim() + "\"}}}";
            string postUrl = "https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token=" + GetAccessToken().Trim();
            string ret = Util.GetWebContent(postUrl, jsonStr);
            OAQRTicket t = JsonConvert.DeserializeObject<OAQRTicket>(ret);
            return "https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket=" + t.ticket.Trim();
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
