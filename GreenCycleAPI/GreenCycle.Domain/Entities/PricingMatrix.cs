using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class PricingMatrix
{
    public int PriceId { get; set; }

    public int CategoryId { get; set; }

    public int? YardId { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? UpdatePrice { get; set; }

    public virtual WasteCategory Category { get; set; } = null!;

    public virtual ScrapYard? Yard { get; set; }
}
