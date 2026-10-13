namespace Rolayther.Services
{
    public interface IEmailService
    {
        Task SendMasterApprovedEmailAsync(string toEmail, string masterName);
        Task SendMasterRejectedEmailAsync(string toEmail, string masterName, string? reason);
    }
}
