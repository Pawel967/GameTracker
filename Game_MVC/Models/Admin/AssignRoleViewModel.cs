using System.ComponentModel.DataAnnotations;

namespace Game_MVC.Models.Admin
{
    public class AssignRoleViewModel
    {
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string RoleName { get; set; } = string.Empty;

        public List<string> AvailableRoles { get; set; } = new();
    }
}
