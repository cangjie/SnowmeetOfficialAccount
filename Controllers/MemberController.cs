using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SnowmeetOfficialAccount.Models;
using Microsoft.Extensions.Configuration;
using SQLitePCL;
namespace SnowmeetOfficialAccount.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberController : ControllerBase
    {
        private readonly AppDBContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        public MemberController(AppDBContext context, IConfiguration config)
        {
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }
        [NonAction]
        public async Task<Member> GetMemberByOfficialAccountOpenId(string openId)
        {
            OfficialAccountApi.UserInfo? userInfo = null;
            string? unionId = null;
            int? memberId = null;
            List<MemberSocialAccount> listMsaOfficial = await _db.memberSocailAccount
                .Where(m => m.num.Trim().Equals(openId.Trim()) && m.type.Trim().Equals("wechat_oa_openid"))
                .AsNoTracking().ToListAsync();
            if (listMsaOfficial != null && listMsaOfficial.Count > 0)
            {
                memberId = listMsaOfficial[0].member_id;
            }
            if (memberId != null)
            {
                OfficialAccountApi _oaHelper = new OfficialAccountApi(_db, _config);
                userInfo = _oaHelper.GetUserInfoFromWechat(openId);
                if (userInfo != null && userInfo.unionid != null)
                {
                    unionId = userInfo.unionid;
                    listMsaOfficial = await _db.memberSocailAccount
                        .Where(m => m.num.Trim().Equals(unionId.Trim()) && m.type.Trim().Equals("wechat_unionid"))
                        .AsNoTracking().ToListAsync();
                    if (listMsaOfficial != null && listMsaOfficial.Count > 0)
                    {
                        memberId = listMsaOfficial[0].member_id;
                    }
                }
            }
            if (memberId == null && unionId != null)
            {
                return await CreateMember(openId.Trim(), unionId.Trim());
            }
            else if (memberId != null)
            {
                List<Member> memberList = await _db.member.Where(m => m.id == memberId)
                    .Include(m => m.memberSocialAccounts).AsNoTracking().ToListAsync();
                if (memberList == null || memberList.Count == 0)
                {
                    return null;
                }
                await CorrectMemberInfo(memberList[0], openId, unionId);
                return memberList[0];
            }
            else
            {
                return null;
            }
        }
        [NonAction]
        public async Task CorrectMemberInfo(Member member, string openId, string unionId)
        {
            bool existsOpenId = false;
            bool existsUnionId = false;
            List<MemberSocialAccount> openIdList = member.memberSocialAccounts
                .Where(m => m.valid == 1 && m.num.Trim().Equals(openId.Trim()) && m.type.Equals("wechat_oa_openid"))
                .ToList();
            if (openIdList != null && openIdList.Count > 0)
            {
                existsOpenId = true;
            }
            List<MemberSocialAccount> unionIdList = member.memberSocialAccounts
                .Where(m => m.valid == 1 && m.num.Trim().Equals(unionId.Trim()) && m.type.Equals("wechat_unionid"))
                .ToList();
            if (unionIdList != null && unionIdList.Count > 0)
            {
                existsUnionId = true;
            }
            if (!existsOpenId)
            {
                await AddMemberInfo(member.id, openId, "wechat_oa_openid");
            }
            if (!existsUnionId)
            {
                await AddMemberInfo(member.id, unionId, "wechat_unionid");
            }
        }
        [NonAction]
        public async Task AddMemberInfo(int memberId, string num, string type)
        {
            List<MemberSocialAccount> msaList = await _db.memberSocailAccount
                .Where(m => m.type.Equals(type) && m.member_id == memberId).ToListAsync();
            bool exists = false;
            for (int i = 0; i < msaList.Count; i++)
            {
                MemberSocialAccount msa = msaList[i];
                if (msa.num.Trim().Equals(num))
                {
                    exists = true;
                    if (msa.valid == 0)
                    {
                        msa.valid = 1;
                        msa.update_date = DateTime.Now;
                        CoreDataModLog log = new CoreDataModLog()
                        {
                            id = 0,
                            table_name = "member_social_account",
                            field_name = "valid",
                            key_value = msa.id,
                            scene = "用户扫码关注公众号",
                            member_id = memberId,
                            staff_id = null,
                            prev_value = "0",
                            current_value = "1",
                            is_manual = 0
                        };
                        _db.memberSocailAccount.Entry(msa).State = EntityState.Modified;
                        await _db.dataLog.AddAsync(log);
                    }
                }
                else
                {
                    if (type.Equals("wechat_oa_openid") || type.Equals("wechat_unioniud"))
                    {
                        if (msa.valid == 1)
                        {
                            msa.valid = 0;
                            msa.update_date = DateTime.Now;
                            CoreDataModLog log = new CoreDataModLog()
                            {
                                id = 0,
                                table_name = "member_social_account",
                                field_name = "valid",
                                key_value = msa.id,
                                scene = "用户扫码关注公众号",
                                member_id = memberId,
                                staff_id = null,
                                prev_value = "1",
                                current_value = "0",
                                is_manual = 0
                            };
                            _db.memberSocailAccount.Entry(msa).State = EntityState.Modified;
                            await _db.dataLog.AddAsync(log);
                        }
                    }
                }
            }
            if (!exists)
            {
                MemberSocialAccount msa = new MemberSocialAccount()
                {
                    id = 0,
                    member_id = memberId,
                    type = type,
                    valid = 1,
                    num = num,
                    create_date = DateTime.Now
                };
                await _db.memberSocailAccount.AddAsync(msa);
            }
            await _db.SaveChangesAsync();
        }
        [NonAction]
        public async Task<Member> CreateMember(string openId, string unionId)
        {
            Member member = new Member()
            {
                id = 0,
                memberSocialAccounts = new List<MemberSocialAccount>(),
                create_date = DateTime.Now
            };
            member.memberSocialAccounts.Add(new MemberSocialAccount()
            {
                id = 0,
                member_id = member.id,
                type = "wechat_oa_openId",
                num = openId.Trim(),
                valid = 1,
                create_date = DateTime.Now
            });
            member.memberSocialAccounts.Add(new MemberSocialAccount()
            {
                id = 0,
                member_id = member.id,
                type = "wechat_unionid",
                num = unionId.Trim(),
                valid = 1,
                create_date = DateTime.Now
            });
            await _db.member.AddAsync(member);
            await _db.SaveChangesAsync();
            CoreDataModLog log = new CoreDataModLog()
            {
                id = 0,
                table_name = "member",
                field_name = null,
                key_value = member.id,
                scene = "用户扫码关注公众号",
                member_id = member.id,
                staff_id = null,
                prev_value = null,
                current_value = null,
                is_manual = 1
            };
            await _db.dataLog.AddAsync(log);
            await _db.SaveChangesAsync();
            return member;
        }
        [NonAction]
        public async Task<Member> GetMember(string num, string type = "")
        {

            type = type.Trim();
            int memberId = 0;

            var msaList = await _db.memberSocailAccount
                        .Where(a => (a.valid == 1 && a.num.Trim().Equals(num) && a.type.Trim().Equals(type)))
                        .OrderByDescending(a => a.id).ToListAsync();
            if (msaList.Count == 0)
            {
                return null;
            }
            memberId = msaList[0].member_id;
            if (memberId == 0)
            {
                return null;
            }
            Member member = await _db.member.Include(m => m.memberSocialAccounts)
                .Where(m => m.id == memberId).FirstAsync();
            return member;
        }


    }
}
