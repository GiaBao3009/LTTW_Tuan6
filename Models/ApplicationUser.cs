using Microsoft.AspNetCore.Identity;

namespace LTTW_Tuan6.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string? FullName { get; set; }

        [PersonalData]
        public string? Address { get; set; }
    }
}
