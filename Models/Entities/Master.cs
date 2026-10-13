using Microsoft.EntityFrameworkCore;
using Rolayther.Models.Entities.Bridges;
using Rolayther.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Rolayther.Models.Entities
{
    public class Master : BaseEntity
    {
        [Key]
        public Guid MasterId { get; set; }
        // Stato della richiesta: Pending finché un Admin non approva/rifiuta.
        // Il ruolo Identity "Master" viene assegnato solo al passaggio ad Approved.
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string Surname { get; set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string NickName { get; set; } = string.Empty;
        [Required]
        public DateOnly DateOfBirth { get; set; }
        public string? AvatarImgUrl { get; set; }
        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
        [Required]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? BioMaster { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<Session> Sessions { get; set; } = new List<Session>();

        public ICollection<MasterGame> MasterGames { get; set; } = new List<MasterGame>();
        public ICollection<MasterPlatform> MasterPlatforms { get; set; } = new List<MasterPlatform>();


    }
}
