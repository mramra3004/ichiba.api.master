﻿namespace IChiba.Services.Master
{
    public class VesselSearchContext
    {
        public string Keywords { get; set; }

        public int Status { get; set; }

        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public string LanguageId { get; set; }

        public string CountryId { get; set; }
    }
}
