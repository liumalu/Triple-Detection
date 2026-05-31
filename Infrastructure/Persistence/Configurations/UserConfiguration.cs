using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDetection.Domain.Entities;

namespace TripleDetection.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).HasMaxLength(100).IsRequired();
        builder.Property(u => u.RealName).HasMaxLength(200);
        builder.Property(u => u.Password).HasMaxLength(500);
        builder.Property(u => u.PasswordSalt).HasMaxLength(100);
        builder.Property(u => u.PasswordHash).HasMaxLength(100);
        builder.Property(u => u.Role).HasMaxLength(50);
        builder.Property(u => u.IsEnabled).HasDefaultValue(true);
        builder.Property(u => u.IsLocked).HasDefaultValue(false);
        builder.Ignore(u => u.StatusText);
    }
}