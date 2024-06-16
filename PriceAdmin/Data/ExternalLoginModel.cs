using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CTO.Price.Admin.Data
{
    public class ExternalLoginModel
    {
        [Required] [EmailAddress] public string Email { get; set; } = null!;
    }
}
