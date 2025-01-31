﻿namespace Insolvency.CalculationsEngine.Redundancy.BL.DTOs.BasicAward
{
    public class BasicAwardCalculationResponseDTO
    {
        public decimal GrossEntitlement { get; set; }

        public bool IsTaxable { get; set; }

        public decimal TaxDeducted { get; set; }

        public decimal NIDeducted { get; set; }

        public decimal NetEntitlement { get; set; }
        public decimal PreferentialClaim { get; set; }
        public decimal NonPreferentialClaim { get; set; }
    }
}
