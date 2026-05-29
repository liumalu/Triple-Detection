using System.Data.Entity.ModelConfiguration;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Configuration
{
    /// <summary>
    /// User 实体配置 - Username 作为主键（非自增）
    /// </summary>
    public class UserConfiguration : EntityTypeConfiguration<User>
    {
        public UserConfiguration()
        {
            ToTable("Users");

            // Username 是主键，不自增
            HasKey(u => u.Username);

            Property(u => u.Username).HasMaxLength(100).IsRequired();
            Property(u => u.RealName).HasMaxLength(100);
            Property(u => u.Password).HasMaxLength(256).IsRequired();
            Property(u => u.Role).HasMaxLength(50).IsRequired();

            // StatusText 是计算属性，不映射到数据库
            Ignore(u => u.StatusText);
        }
    }
}