using finalSE.Models;
using finalSE.Repository.Interface;
using finalSE.Service.Interface;
using finalSE.UnitOfWork.Interface;

namespace finalSE.Service.Application
{
    public class InvitationService : IInvitationService
    {
        private readonly IInvitationRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;

        public InvitationService(
            IInvitationRepository repo,
            IUnitOfWork uow,
            EmailService emailService,
            IConfiguration config)
        {
            _repo = repo;
            _uow = uow;
            _emailService = emailService;
            _config = config;
        }

        // ================= SEND INVITATION =================
        public async Task SendInvitationAsync(string email, int roleId)
        {
            var token = Guid.NewGuid().ToString();

            var invitation = new Invitation
            {
                Email = email,
                RoleId = roleId,
                Token = token,
                IsUsed = false,
                CreatedAt = DateTime.Now,
                ExpireDate = DateTime.Now.AddDays(1)
            };

            await _repo.AddAsync(invitation);
            await _uow.SaveChangesAsync();

            // 🔥 DYNAMIC LINK (FIXED)
            string link = $"{_config["AppUrl"]}/Account/RegisterWithToken?token={token}";

            string body = $@"
                <h2>You're Invited!</h2>
                <p>Click below to join:</p>
                <a href='{link}'>Register Here</a>
                <p>This link expires in 24 hours.</p>
            ";

            await _emailService.SendEmailAsync(email, "Invitation Link", body);
        }

        // ================= VALIDATE TOKEN =================
        public async Task<Invitation?> ValidateTokenAsync(string token)
        {
            var invitation = _repo.GetByToken(token);

            if (invitation == null) return null;
            if (invitation.IsUsed) return null;
            if (invitation.ExpireDate < DateTime.Now) return null;

            return invitation;
        }

        // ================= ACCEPT INVITATION =================
        public async Task<bool> AcceptInvitationAsync(string token)
        {
            var invitation = _repo.GetByToken(token);

            if (invitation == null)
                return false;

            invitation.IsUsed = true;

            _repo.Update(invitation);
            await _uow.SaveChangesAsync();

            return true;
        }

        // ================= GET ALL =================
        public async Task<List<Invitation>> GetAllAsync()
        {
            return _repo.GetAll();
        }

        // ================= RESEND EMAIL =================
        public async Task ResendInvitationEmailAsync(string token, string email)
        {
            var invitation = _repo.GetByToken(token);
            if (invitation == null || invitation.IsUsed) return;

            // Generate link dynamically using configured AppUrl or fallback to request context if needed
            string link = $"{_config["AppUrl"]}/Account/RegisterWithToken?token={token}";

            string body = $@"
                <h2>You're Invited!</h2>
                <p>Click below to join:</p>
                <a href='{link}'>Register Here</a>
                <p>This link expires in 24 hours.</p>
            ";

            await _emailService.SendEmailAsync(email, "Invitation Link", body);
        }
    }
}