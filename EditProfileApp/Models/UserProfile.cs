using System;
using System.Collections.Generic;

namespace EditProfileApp.Models;

public partial class UserProfile
{
    public string StudentId { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Nickname { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? EmergencyMobile { get; set; }

    public string? Department { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
