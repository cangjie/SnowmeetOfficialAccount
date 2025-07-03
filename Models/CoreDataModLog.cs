using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SnowmeetOfficialAccount.Models
{
    [Table("core_data_mod_log")]
    public class CoreDataModLog
    {
        [Key]
        public int id { get; set; }
        public string table_name {get; set;}
        public string? field_name { get; set; } = null;
        public int key_value {get; set;}
        public string scene {get; set;}
        public int? member_id { get; set; } = null;
        public int? staff_id {get; set;}  = null;
        public string? prev_value {get; set;}  = null;
        public string? current_value {get; set;}  = null;
        public long trace_id { get; set; } = 0;
        public int is_manual {get; set;} = 0;
        public string? manual_memo {get; set;} = null;
        public DateTime create_date {get; set;} = DateTime.Now;
        [NotMapped]
        public string simpleMemo 
        {
            get
            {
                switch(table_name)
                {
                    case "retail":
                        switch(field_name)
                        {
                            case "order_type":
                                return "修改订单类型";
                            case "mi7_code":
                                return "修改七色米订单号";
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
                return "";
            }
        }

    }
}