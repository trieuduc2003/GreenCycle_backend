using System;
using System.Collections.Generic;
using GreenCycle.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace GreenCycle.Infrastructure.Data;

public partial class GreenCycleDbContext : DbContext
{
    public GreenCycleDbContext()
    {
    }

    public GreenCycleDbContext(DbContextOptions<GreenCycleDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Collector> Collectors { get; set; }

    public virtual DbSet<CollectorLiveLocation> CollectorLiveLocations { get; set; }

    public virtual DbSet<DropOffOrder> DropOffOrders { get; set; }

    public virtual DbSet<EscrowAccount> EscrowAccounts { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OrderMethod> OrderMethods { get; set; }

    public virtual DbSet<OrderStatus> OrderStatuses { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PickUpOrder> PickUpOrders { get; set; }

    public virtual DbSet<PricingMatrix> PricingMatrices { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<ScrapYard> ScrapYards { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAddress> UserAddresses { get; set; }

    public virtual DbSet<UserVoucher> UserVouchers { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }

    public virtual DbSet<WasteCategory> WasteCategories { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Server=LAPTOP-CVQ2ADHO\\SQLEXPRESS;Database=GreenCycle;User Id=sa;Password=123;TrustServerCertificate=True;", x => x.UseNetTopologySuite());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Collector>(entity =>
        {
            entity.HasKey(e => e.CollectorId).HasName("PK__Collecto__2B20BB0B058C791A");

            entity.HasIndex(e => e.UserId, "UQ__Collecto__1788CCAD9984EC32").IsUnique();

            entity.Property(e => e.CollectorId).HasColumnName("CollectorID");
            entity.Property(e => e.CurrentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("OFFLINE");
            entity.Property(e => e.JoinedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.LicensePlate).HasMaxLength(20);
            entity.Property(e => e.Rating)
                .HasDefaultValue(5.0m)
                .HasColumnType("decimal(3, 2)");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.VehicleType).HasMaxLength(50);

            entity.HasOne(d => d.User).WithOne(p => p.Collector)
                .HasForeignKey<Collector>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Collector__UserI__4AB81AF0");
        });

        modelBuilder.Entity<CollectorLiveLocation>(entity =>
        {
            entity.HasKey(e => e.CollectorId).HasName("PK__Collecto__2B20BB0BE72A31A6");

            entity.Property(e => e.CollectorId)
                .ValueGeneratedNever()
                .HasColumnName("CollectorID");
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Collector).WithOne(p => p.CollectorLiveLocation)
                .HasForeignKey<CollectorLiveLocation>(d => d.CollectorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Collector__Colle__5070F446");
        });

        modelBuilder.Entity<DropOffOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__DropOffO__C3905BAFFC170F1F");

            entity.Property(e => e.OrderId)
                .ValueGeneratedNever()
                .HasColumnName("OrderID");
            entity.Property(e => e.YardId).HasColumnName("YardID");

            entity.HasOne(d => d.Order).WithOne(p => p.DropOffOrder)
                .HasForeignKey<DropOffOrder>(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DropOffOr__Order__6477ECF3");

            entity.HasOne(d => d.Yard).WithMany(p => p.DropOffOrders)
                .HasForeignKey(d => d.YardId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DropOffOr__YardI__656C112C");
        });

        modelBuilder.Entity<EscrowAccount>(entity =>
        {
            entity.HasKey(e => e.EscrowId).HasName("PK__EscrowAc__557665342ACA2DF8");

            entity.Property(e => e.EscrowId).HasColumnName("EscrowID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ReferenceType).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("HOLDING");
            entity.Property(e => e.WalletId).HasColumnName("WalletID");

            entity.HasOne(d => d.Order).WithMany(p => p.EscrowAccounts)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__EscrowAcc__Order__0E6E26BF");

            entity.HasOne(d => d.Wallet).WithMany(p => p.EscrowAccounts)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EscrowAcc__Walle__0D7A0286");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BAF3DCFA14C");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.MethodId).HasColumnName("MethodID");
            entity.Property(e => e.PlatformFee)
                .HasDefaultValue(0.0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SellerId).HasColumnName("SellerID");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.TotalActualAmount)
                .HasDefaultValue(0.0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalEstimatedAmount)
                .HasDefaultValue(0.0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Method).WithMany(p => p.Orders)
                .HasForeignKey(d => d.MethodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__MethodID__5BE2A6F2");

            entity.HasOne(d => d.Seller).WithMany(p => p.Orders)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__SellerID__5AEE82B9");

            entity.HasOne(d => d.Status).WithMany(p => p.Orders)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__StatusID__5CD6CB2B");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D30C7383C7A8");

            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.ActualSubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ActualWeight).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.EstimatedSubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.EstimatedWeight).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderDeta__Categ__6E01572D");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderDeta__Order__6D0D32F4");
        });

        modelBuilder.Entity<OrderMethod>(entity =>
        {
            entity.HasKey(e => e.MethodId).HasName("PK__OrderMet__FC681FB1B82B3DB7");

            entity.Property(e => e.MethodId).HasColumnName("MethodID");
            entity.Property(e => e.MethodName).HasMaxLength(50);
        });

        modelBuilder.Entity<OrderStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__OrderSta__C8EE20433C0D123C");

            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.StatusName).HasMaxLength(50);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A58CD92CE3F");

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.ProviderTransactionId)
                .HasMaxLength(100)
                .HasColumnName("ProviderTransactionID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__Payments__OrderI__14270015");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__UserID__1332DBDC");
        });

        modelBuilder.Entity<PickUpOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__PickUpOr__C3905BAF62ECB1C6");

            entity.Property(e => e.OrderId)
                .ValueGeneratedNever()
                .HasColumnName("OrderID");
            entity.Property(e => e.CollectorId).HasColumnName("CollectorID");
            entity.Property(e => e.PickupAddressId).HasColumnName("PickupAddressID");

            entity.HasOne(d => d.Collector).WithMany(p => p.PickUpOrders)
                .HasForeignKey(d => d.CollectorId)
                .HasConstraintName("FK__PickUpOrd__Colle__6A30C649");

            entity.HasOne(d => d.Order).WithOne(p => p.PickUpOrder)
                .HasForeignKey<PickUpOrder>(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PickUpOrd__Order__68487DD7");

            entity.HasOne(d => d.PickupAddress).WithMany(p => p.PickUpOrders)
                .HasForeignKey(d => d.PickupAddressId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PickUpOrd__Picku__693CA210");
        });

        modelBuilder.Entity<PricingMatrix>(entity =>
        {
            entity.HasKey(e => e.PriceId).HasName("PK__PricingM__4957584F45CA192B");

            entity.ToTable("PricingMatrix");

            entity.Property(e => e.PriceId).HasColumnName("PriceID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.EffectiveDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatePrice).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.YardId).HasColumnName("YardID");

            entity.HasOne(d => d.Category).WithMany(p => p.PricingMatrices)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PricingMa__Categ__70DDC3D8");

            entity.HasOne(d => d.Yard).WithMany(p => p.PricingMatrices)
                .HasForeignKey(d => d.YardId)
                .HasConstraintName("FK__PricingMa__YardI__71D1E811");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<ScrapYard>(entity =>
        {
            entity.HasKey(e => e.YardId).HasName("PK__ScrapYar__A0D30D2E73EC1E6B");

            entity.HasIndex(e => e.UserId, "UQ__ScrapYar__1788CCADFC47230C").IsUnique();

            entity.Property(e => e.YardId).HasColumnName("YardID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.IsOpening).HasDefaultValue(true);
            entity.Property(e => e.OperatingHours).HasMaxLength(100);
            entity.Property(e => e.Ranking)
                .HasDefaultValue(0.0m)
                .HasColumnType("decimal(2, 1)");
            entity.Property(e => e.ScrapYardName).HasMaxLength(100);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithOne(p => p.ScrapYard)
                .HasForeignKey<ScrapYard>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ScrapYard__UserI__44FF419A");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK_User");

            entity.HasIndex(e => e.Phone, "UQ__Users__5C7E359EC3552D8B").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PassWordHash).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.UserName).HasMaxLength(50);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Role_User");
        });

        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__UserAddr__091C2A1BCAE38B14");

            entity.Property(e => e.AddressId).HasColumnName("AddressID");
            entity.Property(e => e.AddressLabel).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FullAddress).HasMaxLength(255);
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.UserAddresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserAddre__UserI__3F466844");
        });

        modelBuilder.Entity<UserVoucher>(entity =>
        {
            entity.HasKey(e => e.UserVoucherId).HasName("PK__UserVouc__8017D4B981294D81");

            entity.HasIndex(e => e.VoucherCode, "UQ__UserVouc__7F0ABCA9F58EA170").IsUnique();

            entity.Property(e => e.UserVoucherId).HasColumnName("UserVoucherID");
            entity.Property(e => e.IsUsed).HasDefaultValue(false);
            entity.Property(e => e.ReceivedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.VoucherCode).HasMaxLength(50);
            entity.Property(e => e.VoucherId).HasColumnName("VoucherID");

            entity.HasOne(d => d.User).WithMany(p => p.UserVouchers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserVouch__UserI__02084FDA");

            entity.HasOne(d => d.Voucher).WithMany(p => p.UserVouchers)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserVouch__Vouch__02FC7413");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId).HasName("PK__Vouchers__3AEE79C1BF66B247");

            entity.Property(e => e.VoucherId).HasColumnName("VoucherID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PointCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Quantity).HasDefaultValue(0);
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("PK__Wallets__84D4F92EDA952E46");

            entity.HasIndex(e => new { e.UserId, e.WalletType }, "UQ_User_WalletType").IsUnique();

            entity.Property(e => e.WalletId).HasColumnName("WalletID");
            entity.Property(e => e.Balance)
                .HasDefaultValue(0.00m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.WalletType).HasMaxLength(30);

            entity.HasOne(d => d.User).WithMany(p => p.Wallets)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Wallets__UserID__787EE5A0");
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__WalletTr__55433A4BE18CB6E4");

            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.ReferenceOrderId).HasColumnName("ReferenceOrderID");
            entity.Property(e => e.ReferenceVoucherId).HasColumnName("ReferenceVoucherID");
            entity.Property(e => e.TransactionType).HasMaxLength(50);
            entity.Property(e => e.WalletId).HasColumnName("WalletID");

            entity.HasOne(d => d.ReferenceOrder).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.ReferenceOrderId)
                .HasConstraintName("FK__WalletTra__Refer__08B54D69");

            entity.HasOne(d => d.ReferenceVoucher).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.ReferenceVoucherId)
                .HasConstraintName("FK__WalletTra__Refer__09A971A2");

            entity.HasOne(d => d.Wallet).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WalletTra__Walle__07C12930");
        });

        modelBuilder.Entity<WasteCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__WasteCat__19093A2BA13CC073");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.Co2ReductionFactor)
                .HasDefaultValue(0.0m)
                .HasColumnType("decimal(10, 4)");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Unit).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
