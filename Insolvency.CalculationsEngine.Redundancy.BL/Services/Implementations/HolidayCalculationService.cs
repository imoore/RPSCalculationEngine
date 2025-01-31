﻿using Insolvency.CalculationsEngine.Redundancy.BL.Calculations.Holiday.Extensions;
using Insolvency.CalculationsEngine.Redundancy.BL.DTOs.Common;
using Insolvency.CalculationsEngine.Redundancy.BL.DTOs.Holiday;
using Insolvency.CalculationsEngine.Redundancy.BL.Services.Interfaces;
using Insolvency.CalculationsEngine.Redundancy.Common.ConfigLookups;
using Insolvency.CalculationsEngine.Redundancy.Common.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Insolvency.CalculationsEngine.Redundancy.BL.Services.Implementations
{
    public class HolidayCalculationService : IHolidayCalculationService
    {
        private readonly IHolidayPayAccruedCalculationService _hpaService;
        private readonly IHolidayTakenNotPaidCalculationService _htnpService;

        public HolidayCalculationService(IHolidayPayAccruedCalculationService hpaService, 
            IHolidayTakenNotPaidCalculationService htnpService)
        {
            _hpaService = hpaService;
            _htnpService = htnpService;
        }

        public async Task<HolidayCalculationResponseDTO> PerformHolidayCalculationAsync(HolidayCalculationRequestModel data, IOptions<ConfigLookupRoot> options)
        {
            if (data.Hpa != null)
                return await PerformHolidayCalculationWithHPAAsync(data, options);
            else if (data.Htnp != null && data.Htnp.Any())
                return await PerformHolidayCalculationWithoutHPAAsync(data, options);
            else
                return new HolidayCalculationResponseDTO();
        }

        public async Task<HolidayCalculationResponseDTO> PerformHolidayCalculationWithHPAAsync(HolidayCalculationRequestModel data, IOptions<ConfigLookupRoot> options)
        {
            string selectedInputString = null;
            var result = new HolidayCalculationResponseDTO();

            // select the input source with lowest count in the currrent holiday year
            if (data.Htnp != null && data.Htnp.Any())
            {
                var firstHtnp = data.Htnp.First();

                var holidayYearStart = await data.Hpa.GetHolidayYearStart();
                var htnpEndDate = firstHtnp.DismissalDate.Date < firstHtnp.InsolvencyDate.Date ? firstHtnp.DismissalDate.Date : firstHtnp.InsolvencyDate.Date;

                var tweleveMonthsPrior = firstHtnp.InsolvencyDate.Date.AddMonths(-12).AddDays(1);
                var numRp1HtnpDays = (await data.Htnp.GetHTNPDays(InputSource.Rp1, tweleveMonthsPrior, htnpEndDate)).Count;
                var numRp14aHtnpDays = (await data.Htnp.GetHTNPDays(InputSource.Rp14a, tweleveMonthsPrior, htnpEndDate)).Count;

                if ((numRp1HtnpDays > 0 && numRp1HtnpDays < numRp14aHtnpDays) ||
                       (numRp14aHtnpDays == 0 && numRp1HtnpDays > 0))
                    selectedInputString = InputSource.Rp1;
                else
                    selectedInputString = InputSource.Rp14a;

                // align HPA day taken with htnp days 
                int numHTNPDaysInCurrentHolYear = (await data.Htnp.GetHTNPDays(selectedInputString, holidayYearStart, htnpEndDate)).Count;
                data.Hpa.DaysTaken = Math.Max(data.Hpa.DaysTaken.Value, numHTNPDaysInCurrentHolYear);
            }

            result.Hpa = await _hpaService.PerformHolidayPayAccruedCalculationAsync(data.Hpa, options);

            if (data.Htnp != null && data.Htnp.Any())
            {
                result.Htnp = new HolidayTakenNotPaidAggregateOutput();
                result.Htnp.SelectedInputSource = selectedInputString;

                var firstHtnp = data.Htnp.First();
                var maximumHolidayEntitlement = 6m * firstHtnp.ShiftPattern.Count;

                var holidayYearStart = await data.Hpa.GetHolidayYearStart();

                decimal statHolEntitlement = 0.00m;
                statHolEntitlement = await statHolEntitlement.GetStatutoryHolidayEntitlement(data.Hpa.ShiftPattern);
                var holidayEntitlementForCurrentYear = (await statHolEntitlement.GetAdjustedHolidayEntitlement(data.Hpa.ContractedHolEntitlement.Value)) + data.Hpa.DaysCFwd.Value;

                var maximumHTNPDaysInHolidayYear = Math.Min(holidayEntitlementForCurrentYear, maximumHolidayEntitlement) - (result.Hpa?.ProRataAccruedDays ?? 0);
                var maximumHTNPDaysInTotal = maximumHolidayEntitlement - (result.Hpa?.ProRataAccruedDays ?? 0);

                result.Htnp.RP1ResultsList = await _htnpService.PerformCalculationAsync(
                    data.Htnp,
                    InputSource.Rp1,
                    (selectedInputString == InputSource.Rp1) ? maximumHTNPDaysInHolidayYear : 0,
                    (selectedInputString == InputSource.Rp1) ? maximumHTNPDaysInTotal : 0,
                    holidayYearStart,
                    options);
                result.Htnp.RP14aResultsList = await _htnpService.PerformCalculationAsync(
                    data.Htnp,
                    InputSource.Rp14a,
                    (selectedInputString == InputSource.Rp14a) ? maximumHTNPDaysInHolidayYear : 0,
                    (selectedInputString == InputSource.Rp14a) ? maximumHTNPDaysInTotal : 0,
                    holidayYearStart,
                    options);
            }
            return result;
        }

        public async Task<HolidayCalculationResponseDTO> PerformHolidayCalculationWithoutHPAAsync(HolidayCalculationRequestModel data, IOptions<ConfigLookupRoot> options)
        {
            string selectedInputString = null;
            var result = new HolidayCalculationResponseDTO();

            var firstHtnp = data.Htnp.First();
            var htnpEndDate = firstHtnp.DismissalDate.Date < firstHtnp.InsolvencyDate.Date ? firstHtnp.DismissalDate.Date : firstHtnp.InsolvencyDate.Date;
            var maximumHolidayEntitlement = 6m * firstHtnp.ShiftPattern.Count;

            var tweleveMonthsPrior = firstHtnp.InsolvencyDate.Date.AddMonths(-12).AddDays(1);
            var numRp1HtnpDays = (await data.Htnp.GetHTNPDays(InputSource.Rp1, tweleveMonthsPrior, htnpEndDate)).Count;
            var numRp14aHtnpDays = (await data.Htnp.GetHTNPDays(InputSource.Rp14a, tweleveMonthsPrior, htnpEndDate)).Count;

            if ((numRp1HtnpDays > 0 && numRp1HtnpDays < numRp14aHtnpDays) ||
                   (numRp14aHtnpDays == 0 && numRp1HtnpDays > 0))
                selectedInputString = InputSource.Rp1;
            else
                selectedInputString = InputSource.Rp14a;

            result.Htnp = new HolidayTakenNotPaidAggregateOutput();
            result.Htnp.SelectedInputSource = selectedInputString;

            result.Htnp.RP1ResultsList = await _htnpService.PerformCalculationAsync(
                data.Htnp,
                InputSource.Rp1,
                0,
                (selectedInputString == InputSource.Rp1) ? maximumHolidayEntitlement : 0,
                null,
                options);
            result.Htnp.RP14aResultsList = await _htnpService.PerformCalculationAsync(
                data.Htnp,
                InputSource.Rp14a,
                0,
                (selectedInputString == InputSource.Rp14a) ? maximumHolidayEntitlement : 0,
                null,
                options);

            return result;
        }
    }
}

