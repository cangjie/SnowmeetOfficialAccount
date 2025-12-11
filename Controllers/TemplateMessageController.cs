using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SnowmeetOfficialAccount.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnowmeetOfficialAccount.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class TemlateMessageController : ControllerBase
    {
        private readonly AppDBContext _db;
        private readonly IConfiguration _config;
        private readonly Settings _settings;
        public TemlateMessageController(AppDBContext context, IConfiguration config)
        {
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }
        [HttpGet]
        public async Task GetAllTemplate()
        {
            OfficialAccountApi _accountHelper = new OfficialAccountApi(_db, _config);
            string getUrl = "https://api.weixin.qq.com/cgi-bin/template/get_all_private_template?access_token=" + _accountHelper.GetAccessToken().Trim();
            string ret = Util.GetWebContent(getUrl);
            TemplateModelList templatesList = Newtonsoft.Json.JsonConvert.DeserializeObject<TemplateModelList>(ret);
            for(int i = 0; i < templatesList.template_list.Count; i++)
            {
                TemplateModel model = templatesList.template_list[i];
                //List<string> param = templatesList.template_list[i].templateParams;
                TemplateModel ori = await _db.templateModel.Where(t => t.template_id == model.template_id).FirstOrDefaultAsync();
                if (ori == null)
                {
                    string list = "";
                    for(int j = 0; j < model.templateParams.Count; j++)
                    {
                        list += ((j == 0 ? "" : ",") + model.templateParams[j].Trim());
                    }
                    model.param_list = list;
                    await _db.templateModel.AddAsync(model);
                }
                else
                {
                    string list = "";
                    for(int j = 0; j < model.templateParams.Count; j++)
                    {
                        list += ((j == 0 ? "" : ",") + model.templateParams[j].Trim());
                    }
                    ori.content = model.content;
                    ori.title = model.title;
                    ori.param_list = list;
                    ori.update_date = DateTime.Now;
                    _db.templateModel.Entry(ori).State = EntityState.Modified;

                }
            }
            await _db.SaveChangesAsync();
        }
        [HttpGet]
        public async Task<ActionResult<TemplateMessage>> SendTemplateMessage(int memberId, string templateId, string first, string keywords, string remark, string url, string sessionKey)
        {
            OfficialAccountApi _accountHelper = new OfficialAccountApi(_db, _config);
            string token = _accountHelper.GetAccessToken().Trim();
            //miniAppOpenId = Util.UrlDecode(miniAppOpenId);
            templateId = Util.UrlDecode(templateId);
            first = Util.UrlDecode(first);
            keywords = Util.UrlDecode(keywords);
            remark = Util.UrlDecode(remark);
            url = Util.UrlDecode(url);
            sessionKey = Util.UrlDecode(sessionKey);
            TemplateModel model = await _db.templateModel.Where(t => t.template_id == templateId).AsNoTracking().FirstOrDefaultAsync();
            List<string> pList = model.param_list.Split(',').ToList();
            List<string> values = keywords.Split('|').ToList();
            if (pList.Count != values.Count)
            {
                return BadRequest();
            }
        
            Staff staff = await Util.GetStaffBySessionKey(_db, sessionKey);
            if (staff == null || staff.title_level < 100)
            {
                return BadRequest();
            }
            MemberSocialAccount? msa = await _db.memberSocialAccount
                .Where(m => m.member_id == memberId && m.valid == 1 && m.type == "wechat_oa_openid").AsNoTracking().FirstOrDefaultAsync();
            if (msa == null)
            {
                return NoContent();
            }
            string openId = msa.num.Trim();
            //string[] keywordArr = keywords.Split('|');
            string keywordJson = "";
            for (int i = 0; i < pList.Count; i++)
            {
                keywordJson = keywordJson  + ",\"" + pList[i].Trim()  + "\": { \"value\": \"" + values[i].Trim() + "\" }";
            }
            keywordJson = "\"first\": { \"value\": \"" + first + "\", \"color\": \"#000000\" } " + keywordJson
                + ", \"remark\": { \"value\": \"" + remark + "\", \"color\": \"#000000\" }";
            string postJson = "{ \"touser\": \"" + openId.Trim() + "\", \"template_id\" : \"" + templateId.Trim() + "\", \"url\": \"" + url.Trim() + "\", "
                + " \"topcolor\": \"#FF0000\", \"data\":  {" + keywordJson + "}}";
            string postUrl = "https://api.weixin.qq.com/cgi-bin/message/template/send?access_token=" + token.Trim();
            string ret = Util.GetWebContent(postUrl, postJson);
            TemplateMessage msg = new TemplateMessage()
            {
                from = _config.GetSection("Settings").GetSection("OriginalId").Value.Trim(),
                to = openId,
                template_id = templateId,
                first = first,
                keywords = keywords,
                remark = remark,
                url = url,
                ret_message = ret
            };
            await _db.AddAsync(msg);
            await _db.SaveChangesAsync();
            return Ok(msg);
        }

    }

}