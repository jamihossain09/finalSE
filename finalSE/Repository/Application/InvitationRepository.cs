using finalSE.Models;
using finalSE.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace finalSE.Repository.Application
{
    public class InvitationRepository : IInvitationRepository
    {
        private readonly MyDBContext _context;

        public InvitationRepository(MyDBContext context)
        {
            _context = context;
        }

        // ================= ADD =================
        public async Task AddAsync(Invitation invitation)
        {
            await _context.Invitations.AddAsync(invitation);
        }

        // ================= GET BY TOKEN =================
        public async Task<Invitation?> GetByTokenAsync(string token)
        {
            return await _context.Invitations
                .Include(i => i.Role)
                .FirstOrDefaultAsync(i => i.Token == token);
        }

        public Invitation? GetByToken(string token)
        {
            return _context.Invitations
                .Include(i => i.Role)
                .FirstOrDefault(i => i.Token == token);
        }

        // ================= GET ALL =================
        public async Task<List<Invitation>> GetAllAsync()
        {
            return await _context.Invitations
                .Include(i => i.Role)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public List<Invitation> GetAll()
        {
            return _context.Invitations
                .Include(i => i.Role)
                .OrderByDescending(i => i.CreatedAt)
                .ToList();
        }

        // ================= UPDATE =================
        public void Update(Invitation invitation)
        {
            _context.Invitations.Update(invitation);
        }
    }
}