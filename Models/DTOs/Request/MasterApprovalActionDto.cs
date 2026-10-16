using System.ComponentModel.DataAnnotations;

namespace Rolayther.Models.DTOs.Request
{
    public class MasterApprovalActionDto
    {
        // Rifiuto: motivo OBBLIGATORIO. Viene mandato per email al candidato, che deve capire cosa è successo.
        [MaxLength(1000)]
        public string? Reason { get; set; }

        // Approvazione: messaggio FACOLTATIVO di benvenuto, mostrato nell'email al nuovo Master.
        [MaxLength(1000)]
        public string? Message { get; set; }
    }
}
