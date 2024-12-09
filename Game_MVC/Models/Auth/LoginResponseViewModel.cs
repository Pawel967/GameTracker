namespace Game_MVC.Models.Auth
{
    public class LoginResponseViewModel
    {
        public UserViewModel User { get; set; } = new();
        public string Token { get; set; } = string.Empty;
    }
}
