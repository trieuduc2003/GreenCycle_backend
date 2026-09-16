using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace GreenCycle.Domain.Entities;

public partial class CollectorLiveLocation
{
    public int CollectorId { get; set; }

    public Geometry Location { get; set; } = null!;

    public DateTime? LastUpdated { get; set; }

    public virtual Collector Collector { get; set; } = null!;
}
