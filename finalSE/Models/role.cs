using System.Collections.Generic;

namespace finalSE.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }

        public ICollection<User> Users { get; set; }
    }
}