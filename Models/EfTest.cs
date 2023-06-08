using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SnowmeetOfficialAccount.Models
{
    [Table("ef_test")]
    public class EfTest
    {
        [Key]
        public int id { get; set; }

        [Column("field_value1")]
        public string Value { get; set; }

        public DateTime create_date { get; set; }
    }
}