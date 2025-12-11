using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
namespace SnowmeetOfficialAccount.Models
{
	[Table("template_message_log")]
	public class TemplateMessage
	{
		[Key]
		public int id { get; set; }
		public string template_id { get; set; }
		public string from { get; set; }
		public string to { get; set; }
		public string first { get; set; }
		public string keywords { get; set; }
		public string remark { get; set; }
		public string url { get; set; }
		public string ret_message { get; set; }
	}
	[Table("template_model")]
    public class TemplateModel
    {
		[Key]
        public string template_id {get; set;}
        public string title {get; set;}
        public string content {get; set;}
		public string param_list {get; set;}
		public DateTime? update_date {get; set;} = null;
		public DateTime create_date {get; set;} = DateTime.Now;
		[NotMapped]
		public List<string> templateParams 
		{
			get
			{
				List<string> list = new List<string>();
				Regex reg = new Regex("\\{\\{.+\\}\\}");
				MatchCollection mc = reg.Matches(content);
				foreach (Match m in mc)
				{
					string p = m.Value.Trim();
					p = p.Replace("{{", "").Replace("}}", "").Replace(".DATA", "").Trim();
					list.Add(p);
				}
				return list;
			}
		}

    }
	public class TemplateModelList
	{
		public List<TemplateModel> template_list { get; set;}
	}
}