using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace GreenCycle.Domain.Entities;

public partial class ScrapYard
{
    public int YardId { get; set; }

    public int UserId { get; set; }

    public string ScrapYardName { get; set; } = null!;

    public string Address { get; set; } = null!;

    public Geometry Location { get; set; } = null!;

    public string? OperatingHours { get; set; }

    public decimal? Ranking { get; set; }

    public bool? IsOpening { get; set; }

    public virtual ICollection<DropOffOrder> DropOffOrders { get; set; } = new List<DropOffOrder>();

    public virtual ICollection<PricingMatrix> PricingMatrices { get; set; } = new List<PricingMatrix>();

    public virtual User User { get; set; } = null!;
}
