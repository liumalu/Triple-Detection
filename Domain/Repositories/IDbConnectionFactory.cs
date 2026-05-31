using System.Data.Common;

namespace TripleDetection.Domain.Repositories;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}