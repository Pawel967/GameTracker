using System.ComponentModel.DataAnnotations;

namespace Game_MVC.Models.Auth
{
    public class UpdateMeViewModel
    {
        public string? Username { get; set; }
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? Email { get; set; }
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        public string? Password { get; set; }
    }
}
