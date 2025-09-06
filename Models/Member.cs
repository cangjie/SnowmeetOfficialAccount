using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SnowmeetOfficialAccount.Models
{
    [Table("member")]
    public class Member
    {
        [Key]
        public int id { get; set; }
        public string real_name { get; set; } = "";
        public string gender { get; set; } = "";
        public int is_merge {get; set; } = 0;
        public int? merge_id {get; set;} = null;
        public string source {get; set; } = "";
        public string channel_to_know { get; set; } = "";
        public int is_staff { get; set; } = 0;
        public int is_manager { get; set;} = 0;
        public int is_admin { get; set; } = 0;
        public int? following_wechat { get; set; } = null;
        public string member_type { get; set; } = "wechat";
        public DateTime? update_date { get; set; } = null;
        public DateTime create_date { get; set; }
        [NotMapped]
        public bool isNew { get; set; } = false;
        public string GetNum(string type)
        {
            string openId = "";
            foreach (MemberSocialAccount msa in memberSocialAccounts)
            {
                if (msa.type.Trim().Equals(type.Trim()))
                {
                    openId = msa.num.Trim();
                    break;
                }
            }
            return openId;
        }

        public string oaOpenId
        {
            get
            {
                string openId = "";
                foreach (MemberSocialAccount msa in memberSocialAccounts)
                {
                    if (msa.type.Trim().Equals("wechat_oa_openid"))
                    {
                        openId = msa.num.Trim();
                        break;
                    }
                }
                return openId;
            }
        }

        
        public List<MemberSocialAccount> memberSocialAccounts { get; set; } = new List<MemberSocialAccount>();

        public List<MemberSocialAccount> GetInfo(string type)
        {
            List<MemberSocialAccount> msaList = new List<MemberSocialAccount>();
            foreach (MemberSocialAccount msa in memberSocialAccounts)
            {
                if (msa.valid == 1 && msa.type.Trim().Equals(type.Trim()))
                {
                    msaList.Add(msa);
                }
            }
            return msaList;
        }

        public string? wechatMiniOpenId
        {
            get
            {
                string? v = null;
                List<MemberSocialAccount> msaList = GetInfo("wechat_mini_openid");
                if (msaList != null && msaList.Count > 0)
                {
                    v = msaList[0].num.Trim();
                }
                return v;
            }
        }

        public string? wechatUnionId
        {
            get
            {
                string? v = null;
                List<MemberSocialAccount> msaList = GetInfo("wechat_unionid");
                if (msaList != null && msaList.Count > 0)
                {
                    v = msaList[0].num.Trim();
                }
                return v;
            }
        }

        public string? cell
        {
            get
            {
                string? v = null;
                List<MemberSocialAccount> msaList = GetInfo("cell");
                if (msaList != null && msaList.Count > 0)
                {
                    v = msaList[0].num.Trim();
                }
                return v;
            }
        }

        public string? wechatId
        {
            get
            {
                string? v = null;
                List<MemberSocialAccount> msaList = GetInfo("wechat_id");
                if (msaList != null && msaList.Count > 0)
                {
                    v = msaList[0].num.Trim();
                }
                return v;
            }
        }


    }
}