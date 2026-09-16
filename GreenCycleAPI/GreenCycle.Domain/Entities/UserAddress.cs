using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace GreenCycle.Domain.Entities;

public partial class UserAddress
{
    public int AddressId { get; set; }

    public int UserId { get; set; }

    public string? AddressLabel { get; set; }

    public string FullAddress { get; set; } = null!;

    public Geometry Location { get; set; } = null!;

    public bool? IsDefault { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<PickUpOrder> PickUpOrders { get; set; } = new List<PickUpOrder>();

    public virtual User User { get; set; } = null!;
}
