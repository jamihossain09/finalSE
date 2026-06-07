using finalSE.Models;

namespace finalSE.Service.Interface
{
    public interface IInvitationService
    {
        Task SendInvitationAsync(string email, int roleId);

        Task<Invitation?> ValidateTokenAsync(string token);

        Task<List<Invitation>> GetAllAsync();

        Task<bool> AcceptInvitationAsync(string token);

        Task ResendInvitationEmailAsync(string token, string email);
    }
}