using System;
using System.Collections.Generic;

namespace EditProfileApp.Models;

public partial class Radcheck
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Attribute { get; set; } = null!;

    public string Op { get; set; } = null!;

    public string Value { get; set; } = null!;
}
