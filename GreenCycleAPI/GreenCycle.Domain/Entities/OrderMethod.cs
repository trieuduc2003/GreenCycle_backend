using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class OrderMethod
{
    public int MethodId { get; set; }

    public string MethodName { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
