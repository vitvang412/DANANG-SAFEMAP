using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaNangSafeMap.Models
{
    [Table("user_profiles")]
    public class UserProfile
    {
        [Key]
        [Column("user_id")]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [Column("full_name")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Column("phone_number")]
        [StringLength(255)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Column("address")]
        public string? Address { get; set; }

        [Column("date_of_birth")]
        public DateTime? DateOfBirth { get; set; }

        [Column("gender")]
        public string? Gender { get; set; } // stored as enum string in DB ('Male','Female','Other')

        // Navigation property
        public virtual User? User { get; set; }
    }
}
