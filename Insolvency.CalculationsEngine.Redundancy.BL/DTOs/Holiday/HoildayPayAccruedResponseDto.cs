﻿using System.Collections.Generic;

namespace Insolvency.CalculationsEngine.Redundancy.BL.DTOs.Holiday
{
    public class HolidayPayAccruedResponseDTO
    {
        public HolidayPayAccruedResponseDTO()
        {
            WeeklyResults = new List<HolidayPayAccruedWeeklyResult>();
        }

        public decimal StatutoryMax { get; set; }
        public decimal HolidaysOwed { get; set; }
        public decimal BusinessDaysInClaim { get; set; }
        public decimal WorkingDaysInClaim { get; set; }
        public decimal ProRataAccruedDays { get; set; }
        public List<HolidayPayAccruedWeeklyResult> WeeklyResults { get; set; }
    }
}