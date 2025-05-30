using Microsoft.AspNetCore.Identity;

namespace LTTW_Tuan6.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Address { get; set; }
    }
}
