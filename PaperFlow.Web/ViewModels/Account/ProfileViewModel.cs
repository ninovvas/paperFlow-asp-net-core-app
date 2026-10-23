namespace PaperFlow.Web.ViewModels.Account
{
    using System.ComponentModel.DataAnnotations;
    public sealed class ProfileViewModel
    {
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone number")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(32)]
        public string? PhoneNumber { get; set; }
    }
}

