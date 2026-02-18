using System.ComponentModel.DataAnnotations;

namespace Rolayther.Models.DTOs.Request
{
    public class MasterUpdateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Surname { get; set; } = string.Empty;
        [Required]
        public string NickName { get; set; } = string.Empty;
        [Required]
        public DateOnly DateOfBirth { get; set; }
        public string? AvatarImgUrl { get; set; }
        [Required]
        public string Email { get; set; } = string.Empty;
        
        public string? BioMaster { get; set; }
    }
}
