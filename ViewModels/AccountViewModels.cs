using System.ComponentModel.DataAnnotations;

namespace NexusServiceMarketingSystem.ViewModels;

/// <summary>Sign in form.</summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }
}

/// <summary>Registration form of a new customer.</summary>
public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(150, MinimumLength = 3)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your phone number.")]
    [StringLength(25)]
    [Display(Name = "Phone number")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your address.")]
    [StringLength(400)]
    [Display(Name = "Installation address")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose your city.")]
    [Display(Name = "City")]
    public int CityId { get; set; }

    [Required(ErrorMessage = "Please enter a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must contain at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please repeat the password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool AcceptedTerms { get; set; }
}

/// <summary>Form of the forgotten password request.</summary>
public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>Form of the password reset.</summary>
public class ResetPasswordViewModel
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a new password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must contain at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please repeat the new password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>Form of the password change of a signed in user.</summary>
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Please enter your current password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a new password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must contain at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please repeat the new password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "The two passwords do not match.")]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>Registration request of a new retail partner.</summary>
public class VendorRegistrationViewModel
{
    [Required(ErrorMessage = "Please enter the business name.")]
    [StringLength(150)]
    [Display(Name = "Business name")]
    public string BusinessName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the owner name.")]
    [StringLength(150)]
    [Display(Name = "Owner name")]
    public string OwnerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the phone number.")]
    [StringLength(25)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the business address.")]
    [StringLength(400)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose your city.")]
    [Display(Name = "City")]
    public int CityId { get; set; }

    [Required(ErrorMessage = "Please describe your business.")]
    [StringLength(2000)]
    [Display(Name = "Business details")]
    public string Details { get; set; } = string.Empty;

    // The partner chooses the password of the future login now. The account stays locked
    // until the administrator approves the registration.
    [Required(ErrorMessage = "Please enter a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must contain at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please repeat the password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool Submitted { get; set; }
}
