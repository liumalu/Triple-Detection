using System.Collections.Generic;
using TripleDetection.Domain.Entities;

namespace TripleDetection.Domain.Repositories
{

public interface ISystemConfigRepository : IRepository<SystemConfig>
{
    SystemConfig GetByCategoryAndKey(string category, string key);
    void SaveOrUpdate(SystemConfig config);
    new IEnumerable<SystemConfig> GetAll();
}
}