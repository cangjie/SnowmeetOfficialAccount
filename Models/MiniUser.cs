using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SnowmeetOfficialAccount.Models
{
    [Table("mini_users")]
    public class MiniUser
    {
        [Key]
        public string open_id { get; set; }

        public string union_id { get; set; }
        public int member_id { get; set; }

    }
}
