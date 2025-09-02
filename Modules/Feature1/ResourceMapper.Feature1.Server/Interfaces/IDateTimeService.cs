using ResourceMapper.Feature1.Shared.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMapper.Feature1.Server.Interfaces
{
    public interface IDateTimeService
    {
        Task<GetDateTimeResponse> GetDateTime();
    }
}
