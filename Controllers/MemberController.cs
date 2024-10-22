using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SnowmeetOfficialAccount.Models;
using Microsoft.Extensions.Configuration;
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
