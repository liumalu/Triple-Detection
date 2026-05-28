using System;
using TripleDetection.Data.Repositories;

namespace TripleDetection.Data.Entities
{
    /// <summary>
    /// 用户查询条件
    /// </summary>
    public class UserQuery : PagedQuery
    {
        public string Username { get; set; }
        public string Role { get; set; }
        public string StatusText { get; set; }
    }
}