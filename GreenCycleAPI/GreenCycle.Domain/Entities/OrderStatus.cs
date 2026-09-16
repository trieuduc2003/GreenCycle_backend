using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class OrderStatus
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
