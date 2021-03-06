﻿using IChiba.Core;
using IChiba.Core.Domain.Master;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IChiba.Services.Master
{
    public interface IPostOfficeService
    {
        Task<int> InsertAsync(PostOffice entity);

        Task<int> UpdateAsync(PostOffice entity);

        Task<int> DeletesAsync(IEnumerable<string> ids);

        IList<PostOffice> GetAll(bool showHidden = false);

        IPagedList<PostOffice> Get(PostOfficeSearchContext ctx);

        Task<PostOffice> GetByIdAsync(string id);

        Task<int> ActivatesAsync(IEnumerable<string> ids, bool active);

        Task<bool> ExistsAsync(string code);

        Task<bool> ExistsAsync(string oldCode, string newCode);
    }
}
