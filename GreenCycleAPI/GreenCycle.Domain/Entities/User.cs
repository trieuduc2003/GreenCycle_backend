using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string? UserName { get; set; }

    public string? PassWordHash { get; set; }

    public string? FullName { get; set; }

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsActive { get; set; }

    public int RoleId { get; set; }

    public virtual Collector? Collector { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Role Role { get; set; } = null!;

    public virtual ScrapYard? ScrapYard { get; set; }

    public virtual ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();

    public virtual ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();

    public virtual ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();
}
