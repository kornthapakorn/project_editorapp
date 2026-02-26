namespace EditProfileApp.Models
{
    public class ResetPasswordViewModel
    {
        public string StudentId { get; set; }
        public string Otp { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}