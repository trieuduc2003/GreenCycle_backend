using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class PickUpOrder
{
    public int OrderId { get; set; }

    public int PickupAddressId { get; set; }

    public int? CollectorId { get; set; }

    public DateTime? ScheduledTime { get; set; }

    public virtual Collector? Collector { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual UserAddress PickupAddress { get; set; } = null!;
}
