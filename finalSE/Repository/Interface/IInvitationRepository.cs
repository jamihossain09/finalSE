using finalSE.Models;

namespace finalSE.Repository.Interface
{
    public interface IInvitationRepository
    {
        Task AddAsync(Invitation invitation);

        Task<Invitation?> GetByTokenAsync(string token);

        Task<List<Invitation>> GetAllAsync();

        void Update(Invitation invitation);
        Invitation? GetByToken(string token);
        List<Invitation> GetAll();
    }
}