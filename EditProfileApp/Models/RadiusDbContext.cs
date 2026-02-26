using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace EditProfileApp.Models;

public partial class RadiusDbContext : DbContext
{
    public RadiusDbContext()
    {
    }

    public RadiusDbContext(DbContextOptions<RadiusDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Radcheck> Radchecks { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Radcheck>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__radcheck__3213E83F6A0213D5");

            entity.ToTable("radcheck");

            entity.HasIndex(e => e.Username, "username_idx");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Attribute)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasDefaultValue("Cleartext-Password")
                .HasColumnName("attribute");
            entity.Property(e => e.Op)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue(":=")
                .IsFixedLength()
                .HasColumnName("op");
            entity.Property(e => e.Username)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("username");
            entity.Property(e => e.Value)
                .HasMaxLength(253)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("value");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__user_pro__2A33069ABA77FC3B");

            entity.ToTable("user_profiles");

            entity.Property(e => e.StudentId)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("student_id");
            entity.Property(e => e.Department)
                .HasMaxLength(100)
                .HasColumnName("department");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.EmergencyMobile)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("emergency_mobile");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.Nickname)
                .HasMaxLength(100)
                .HasColumnName("nickname");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
