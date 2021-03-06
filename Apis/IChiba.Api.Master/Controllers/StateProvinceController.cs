﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExtreme.AspNet.Data.ResponseModel;
using DevExtreme.AspNet.Mvc;
using IChiba.Core.Domain.Master;
using IChiba.Services.Localization;
using IChiba.Services.Master;
using IChiba.SharedMvc;
using IChiba.SharedMvc.Models.Master;
using IChiba.Web.Framework.Controllers;
using IChiba.Web.Framework.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IChiba.Api.Master.Controllers
{
    [Route("state-province")]
    [ApiController]
    public class StateProvinceController : AdminControllerBase
    {
        #region Fields

        private readonly IStateProvinceService _stateProvinceService;
        private readonly ICountryService _countryService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly ILocalizedEntityService _localizedEntityService;

        #endregion

        #region Ctor

        public StateProvinceController(
            IStateProvinceService stateProvinceService,
            ILanguageService languageService,
            ILocalizationService localizationService,
            ILocalizedEntityService localizedEntityService,
            ICountryService countryService)
        {
            _stateProvinceService = stateProvinceService;
            _languageService = languageService;
            _localizationService = localizationService;
            _localizedEntityService = localizedEntityService;
            _countryService = countryService;
        }

        #endregion

        #region Methods

        [Route("create")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new StateProvinceModel();

            // Locales
            AddLocales(_languageService, model.Locales, model.LocaleLabels,
                (labels, languageId) =>
                {
                    var label = _localizationService.GetResource("Admin.StateProvinces.Fields.Name", languageId);
                    labels.Add(nameof(StateProvinceLocalizedModel.Name), label);
                });

            PrepareModel(model);

            return Ok(new IChibaResult
            {
                data = model
            });
        }

        [Route("create")]
        [HttpPost]
        public async Task<IActionResult> Create(StateProvinceModel model)
        {
            if (!ModelState.IsValid)
                return InvalidModelResult();

            if (await _stateProvinceService.ExistsAsync(model.Code))
                return Ok(new IChibaResult
                {
                    success = false,
                    message = string.Format(
                        _localizationService.GetResource("Common.Notify.AlreadyExist"),
                        _localizationService.GetResource("Admin.StateProvinces.Fields.Code"))
                });

            var entity = model.ToEntity();
            entity.Code = model.Code;

            await _stateProvinceService.InsertAsync(entity);

            // Locales
            UpdateLocales(entity, model);

            return Ok(new IChibaResult
            {
                message = string.Format(
                    _localizationService.GetResource("Common.Notify.Added"),
                    _localizationService.GetResource("Common.StateProvince"))
            });
        }

        [Route("edit")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var entity = await _stateProvinceService.GetByIdAsync(id);
            if (entity == null)
                return Ok(new IChibaResult
                {
                    success = false,
                    message = string.Format(
                        _localizationService.GetResource("Common.Notify.DoesNotExist"),
                        _localizationService.GetResource("Common.StateProvince"))
                });

            var model = entity.ToModel();

            // Locales
            AddLocales(_languageService, model.Locales, model.LocaleLabels,
                (labels, languageId) =>
                {
                    var label = _localizationService.GetResource("Admin.StateProvinces.Fields.Name", languageId);
                    labels.Add(nameof(StateProvince.Name), label);
                },
                (locale, languageId) =>
                {
                    locale.Name = entity.GetLocalized(x => x.Name, languageId, false, false);
                });

            PrepareModel(model);

            return Ok(new IChibaResult
            {
                data = model
            });
        }

        [Route("edit")]
        [HttpPost]
        public async Task<IActionResult> Edit(StateProvinceModel model)
        {
            ModelState.Remove("Code");
            if (!ModelState.IsValid)
                return InvalidModelResult();

            var entity = await _stateProvinceService.GetByIdAsync(model.Id);
            if (entity == null)
                return Ok(new IChibaResult
                {
                    success = false,
                    message = string.Format(
                        _localizationService.GetResource("Common.Notify.DoesNotExist"),
                        _localizationService.GetResource("Common.StateProvince"))
                });

            entity = model.ToEntity(entity);

            await _stateProvinceService.UpdateAsync(entity);

            // Locales
            UpdateLocales(entity, model);

            return Ok(new IChibaResult
            {
                message = string.Format(
                    _localizationService.GetResource("Common.Notify.Updated"),
                    _localizationService.GetResource("Common.StateProvince"))
            });
        }

        [Route("deletes")]
        [HttpPost]
        public async Task<IActionResult> Deletes(IEnumerable<string> ids)
        {
            if (ids == null || !ids.Any())
            {
                return Ok(new IChibaResult
                {
                    success = false,
                    message = _localizationService.GetResource("Common.Notify.NoItemsSelected")
                });
            }

            await _stateProvinceService.DeletesAsync(ids);

            return Ok(new IChibaResult
            {
                message = string.Format(
                    _localizationService.GetResource("Common.Notify.Deleted"),
                    _localizationService.GetResource("Common.StateProvince"))
            });
        }

        [Route("activates")]
        [HttpPost]
        public async Task<IActionResult> Activates(ActivatesModel model)
        {
            if (model?.Ids == null || !model.Ids.Any())
            {
                return Ok(new IChibaResult
                {
                    success = false,
                    message = _localizationService.GetResource("Common.Notify.NoItemsSelected")
                });
            }

            await _stateProvinceService.ActivatesAsync(model.Ids, model.Active);

            return Ok(new IChibaResult
            {
                message = model.Active
                    ? string.Format(_localizationService.GetResource("Common.Notify.Activated"), _localizationService.GetResource("Common.StateProvince"))
                    : string.Format(_localizationService.GetResource("Common.Notify.Deactivated"), _localizationService.GetResource("Common.StateProvince"))
            });
        }

        #endregion

        #region Lists

        [Route("get-by-country-id")]
        [HttpGet]
        public IActionResult GetByCountryId(string countryId, bool showHidden = false)
        {
            var provinces = _stateProvinceService.GetByCountryId(countryId, showHidden);

            var models = provinces.Select(p =>
            {
                var m = p.ToModel();
                m.Name = p.GetLocalized(x => x.Name);
                return m;
            });

            return Ok(new IChibaResult
            {
                data = models
            });
        }

        [Route("get")]
        [HttpGet]
        public IActionResult Get([FromQuery] DataSourceLoadOptions loadOptions, [FromQuery] StateProvinceSearchModel searchModel)
        {
            var searchContext = new StateProvinceSearchContext
            {
                Keywords = searchModel.Keywords,
                Status = (int)searchModel.Status,
                PageIndex = loadOptions.Skip / loadOptions.Take,
                PageSize = loadOptions.Take,
                LanguageId = searchModel.LanguageId,
                CountryId = searchModel.CountryId
            };

            var entities = _stateProvinceService.Get(searchContext);
            var models = entities.Select(s =>
            {
                var m = s.ToModel();
                m.Name = s.GetLocalized(x => x.Name, searchContext.LanguageId);
                m.Country = new CountryModel
                {
                    Id = s.Country.Id,
                    Name = s.Country.GetLocalized(x => x.Name, searchContext.LanguageId),
                    Code = s.Country.Code
                };
                return m;
            });

            return Ok(new IChibaResult
            {
                data = new LoadResult
                {
                    data = models,
                    totalCount = entities.TotalCount
                }
            });
        }

        #endregion

        #region Helpers



        #endregion

        #region Utilities

        private void UpdateLocales(StateProvince entity, StateProvinceModel model)
        {
            model.Locales.Each(localized =>
            {
                _localizedEntityService.SaveLocalizedValue(entity, x => x.Name, localized.Name, localized.LanguageId);
            });
        }

        private void PrepareModel(StateProvinceModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.SelectCountries = _countryService.GetAll(true)
                .Where(w => w.Active || w.Id == model.CountryId)
                .Select(s => new CountryModel
                {
                    Id = s.Id,
                    Code = s.Code,
                    Name = s.GetLocalized(x => x.Name)
                }).ToList();
        }

        #endregion
    }
}
