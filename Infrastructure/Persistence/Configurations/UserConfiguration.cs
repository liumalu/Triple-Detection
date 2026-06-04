using System.Data.Entity.ModelConfiguration;
using TripleDetection.Domain.Entities;

namespace TripleDetection.Infrastructure.Persistence.Configurations
{

public class UserConfiguration : EntityTypeConfiguration<User>
{
    public UserConfiguration()
    {
        ToTable("Users");
        HasKey(u => u.Id);
        Property(u => u.Username).HasMaxLength(100).IsRequired();
        Property(u => u.RealName).HasMaxLength(200);
        Property(u => u.Password).HasMaxLength(500);
        Property(u => u.PasswordSalt).HasMaxLength(100);
        Property(u => u.PasswordHash).HasMaxLength(100);
        Property(u => u.Role).HasMaxLength(50);
        Ignore(u => u.StatusText);
    }
}
}