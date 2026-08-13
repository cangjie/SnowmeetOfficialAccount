using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SnowmeetOfficialAccount.Models
{
	[Table("oa_user_info")]
	public class OAUserInfo
	{
		[Key]
		public string openid { get; set; }

		public int subscribe { get; set; }
		public string language { get; set;}
		public long subscribe_time { get; set; }
		public string unionid { get; set; }
		public string remark { get; set; }
		public int groupid { get; set; }
		public string? tagid_list_str { get; set; }
		public long qr_scene { get; set; }
		public string qr_scene_str { get; set; }

        [NotMapped]
        public int[] tagid_list  { get; set; }
    }
}

