using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SnowmeetOfficialAccount.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        public string open_id { get; set; }

        public int member_id { get; set; } = 0;
        public string union_id { get; set; } = "";
    }
}
