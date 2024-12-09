using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SnowmeetOfficialAccount.Models
{
	[Table("shop_sale_interact")]
	public class ShopSaleInteract
	{
		[Key]
		public int id { get; set; }

		public string staff_mapp_open_id { get; set; }
		public string scaner_oa_open_id { get; set; }
		public int scan { get; set; }
		public string scaner_union_id { get; set; }
		public string scan_type {get; set; }

    }
}

