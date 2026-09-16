using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class Collector
{
    public int CollectorId { get; set; }

    public int UserId { get; set; }

    public string? VehicleType { get; set; }

    public string? LicensePlate { get; set; }

    public string? CurrentStatus { get; set; }

    public decimal? Rating { get; set; }

    public DateTime? JoinedDate { get; set; }

    public virtual CollectorLiveLocation? CollectorLiveLocation { get; set; }

    public virtual ICollection<PickUpOrder> PickUpOrders { get; set; } = new List<PickUpOrder>();

    public virtual User User { get; set; } = null!;
}
