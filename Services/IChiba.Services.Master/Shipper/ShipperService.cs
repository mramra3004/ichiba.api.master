﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IChiba.Caching;
using IChiba.Core;
using IChiba.Core.Domain;
using IChiba.Core.Domain.Master;
using IChiba.Core.Infrastructure;
using IChiba.Data;
using LinqToDB;

namespace IChiba.Services.Master
{
    public class ShipperService : IShipperService
    {
        #region Constants



        #endregion

        #region Fields

        private readonly IRepository<Shipper> _shipperRepository;
        private readonly IRepository<PaymentTerm> _paymentTermRepository;
        private readonly IRepository<LocalizedProperty> _localizedPropertyRepository;
        private readonly IIChibaCacheManager _cacheManager;
        private readonly GetDataSettings _getDataSettings;

        #endregion

        #region Ctor

        public ShipperService(
            IIChibaCacheManager cacheManager,
            GetDataSettings getDataSettings)
        {
            _shipperRepository = EngineContext.Current.Resolve<IRepository<Shipper>>(DataConnectionHelper.ConnectionStringNames.Master);
            _paymentTermRepository = EngineContext.Current.Resolve<IRepository<PaymentTerm>>(DataConnectionHelper.ConnectionStringNames.Master);
            _localizedPropertyRepository = EngineContext.Current.Resolve<IRepository<LocalizedProperty>>(DataConnectionHelper.ConnectionStringNames.Master);
            _cacheManager = cacheManager;
            _getDataSettings = getDataSettings;
        }

        #endregion

        #region Methods

        public virtual async Task<int> InsertAsync(Shipper entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var result = await _shipperRepository.InsertAsync(entity);

            await _cacheManager.HybridProvider.RemoveByPrefixAsync(MasterCacheKeys.Shippers.PrefixCacheKey);

            return result;
        }

        public virtual async Task<int> UpdateAsync(Shipper entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var result = await _shipperRepository.UpdateAsync(entity);

            await _cacheManager.HybridProvider.RemoveByPrefixAsync(MasterCacheKeys.Shippers.PrefixCacheKey);

            return result;
        }

        public virtual async Task<int> DeletesAsync(IEnumerable<string> ids)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            var result = await _shipperRepository.DeleteAsync(ids);

            await _cacheManager.HybridProvider.RemoveByPrefixAsync(MasterCacheKeys.Shippers.PrefixCacheKey);

            return result;
        }

        public virtual IList<Shipper> GetAll(bool showHidden = false)
        {
            var key = MasterCacheKeys.Shippers.AllCacheKey.FormatWith(showHidden);
            var entities = _cacheManager.GetToDb(key, () =>
            {
                var query = from p in _shipperRepository.Table select p;
                if (!showHidden)
                    query =
                        from p in query
                        where p.Active
                        select p;

                query =
                    from p in query
                    select p;

                return query.ToList();
            }, CachingDefaults.MonthCacheTime);

            return entities;
        }

        public virtual IPagedList<Shipper> Get(ShipperSearchContext ctx)
        {
            ctx.Keywords = ctx.Keywords?.Trim();

            var query = from s in _shipperRepository.Table
                        join p in _paymentTermRepository.Table on s.PaymentTermId equals p.Id into res
                        from pt in res.DefaultIfEmpty()
                        select new Shipper
                        {
                            Active = s.Active,
                            Code = s.Code,
                            Id = s.Id,
                            LocalName = s.LocalName,
                            Name = s.Name,
                            PaymentTermId = s.PaymentTermId,
                            StorageFreeDays = s.StorageFreeDays,
                            VatNumber = s.VatNumber,
                            Website = s.Website,
                            PaymentTerm = pt == null ? null : new PaymentTerm
                            {
                                Id = pt.Id,
                                Code = pt.Code,
                                Name = pt.Name
                            },
                        };

            if (ctx.Keywords.HasValue())
            {
                query = query.LeftJoin(_localizedPropertyRepository.Table,
                        (e, l) => e.Id == l.EntityId,
                        (e, l) => new { e, l })
                    .Where(
                        el =>
                            el.e.Code.Contains(ctx.Keywords) ||
                            el.e.Name.Contains(ctx.Keywords) ||
                            el.e.LocalName.Contains(ctx.Keywords) ||
                            (el.l.LanguageId == ctx.LanguageId &&
                             el.l.LocaleKeyGroup == nameof(Shipper) &&
                             el.l.LocaleKey == nameof(Shipper.Name) &&
                             el.l.LocaleValue.Contains(ctx.Keywords)))
                    .Select(el => el.e).Distinct();
            }
            if (!string.IsNullOrEmpty(ctx.PaymentTermId))
            {
                query =
                    from p in query
                    where p.PaymentTermId == ctx.PaymentTermId
                    select p;
            }
            if (ctx.Status == (int)ActiveStatus.Activated)
            {
                query =
                    from p in query
                    where p.Active
                    select p;
            }
            if (ctx.Status == (int)ActiveStatus.Deactivated)
            {
                query =
                    from p in query
                    where !p.Active
                    select p;
            }

            query =
                from p in query
                orderby p.Code
                select p;

            return new PagedList<Shipper>(query, ctx.PageIndex, ctx.PageSize);
        }

        public virtual async Task<Shipper> GetByIdAsync(string id)
        {
            if (id.IsEmpty())
                return null;

            return await _shipperRepository.GetByIdAsync(id);
        }

        public virtual async Task<int> ActivatesAsync(IEnumerable<string> ids, bool active)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            var result = await _shipperRepository.Table
                .Where(w => ids.Contains(w.Id))
                .Set(x => x.Active, active)
                .UpdateAsync();

            await _cacheManager.HybridProvider.RemoveByPrefixAsync(MasterCacheKeys.Shippers.PrefixCacheKey);

            return result;
        }

        public virtual async Task<bool> ExistsAsync(string code)
        {
            return await _shipperRepository.Table
                .AnyAsync(
                    a =>
                        !string.IsNullOrEmpty(a.Code)
                        && a.Code.Equals(code));
        }

        public virtual async Task<bool> ExistsAsync(string oldCode, string newCode)
        {
            return await _shipperRepository.Table
                .AnyAsync(
                    a =>
                        !string.IsNullOrEmpty(a.Code)
                        && a.Code.Equals(newCode)
                        && !a.Code.Equals(oldCode));
        }

        public virtual IList<Shipper> GetByPaymentTermId(string paymentTermId, bool showHidden = false)
        {
            var key = MasterCacheKeys.Shippers.AllCacheKey.FormatWith(paymentTermId, showHidden);
            var entities = _cacheManager.GetToDb(key, () =>
            {
                var query = from p in _shipperRepository.Table select p;
                if (string.IsNullOrEmpty(paymentTermId))
                {
                    query = from p in _shipperRepository.Table
                            where p.PaymentTermId == paymentTermId
                            select p;
                }
                if (!showHidden)
                    query =
                        from p in query
                        where p.Active
                        select p;

                query =
                    from p in query
                    select p;

                return query.ToList();
            }, CachingDefaults.MonthCacheTime);

            return entities;
        }

        public virtual async Task<IList<Shipper>> GetTop_SelectAsync(ShipperSearchContext ctx)
        {
            ctx.Keywords = ctx.Keywords?.Trim();
            if (ctx.PageSize > _getDataSettings.MaxTopSize)
                ctx.PageSize = _getDataSettings.MaxTopSize;

            var query = from p in _shipperRepository.Table select p;

            if (ctx.Keywords.HasValue())
            {
                query =
                    from p in query
                    where p.Code.Contains(ctx.Keywords) || p.Name.Contains(ctx.Keywords) || p.LocalName.Contains(ctx.Keywords)
                    select p;
            }
            if (ctx.Status == (int)ActiveStatus.Activated)
            {
                query =
                    from p in query
                    where p.Active
                    select p;
            }
            if (ctx.Status == (int)ActiveStatus.Deactivated)
            {
                query =
                    from p in query
                    where !p.Active
                    select p;
            }

            query =
                from p in query
                orderby p.Code
                select p;

            return await query.Take(ctx.PageSize).ToListAsync();
        }

        #endregion
    }
}
