using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Configuration
{
    public class UserConfiguration : EntityTypeConfiguration<User>
    {
        public UserConfiguration()
        {
            ToTable("Users");

            HasKey(u => u.Id);

            Property(u => u.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(u => u.Username).HasMaxLength(100).IsRequired();
            Property(u => u.RealName).HasMaxLength(100);
            Property(u => u.Password).HasMaxLength(256).IsRequired();
            Property(u => u.PasswordSalt).HasMaxLength(64);
            Property(u => u.PasswordHash).HasMaxLength(128);
            Property(u => u.Role).HasMaxLength(50).IsRequired();

            Ignore(u => u.StatusText);
        }
    }
}
