using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class DropOffOrder
{
    public int OrderId { get; set; }

    public int YardId { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ScrapYard Yard { get; set; } = null!;
}
