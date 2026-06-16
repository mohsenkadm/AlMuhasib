using AlMuhasib.Core.Entities.Hotel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Hotel.Configurations;

public class HotelSettingsConfiguration : IEntityTypeConfiguration<HotelSettings>
{
    public void Configure(EntityTypeBuilder<HotelSettings> builder)
    {
        builder.ToTable("HotelSettings");
        builder.Property(x => x.HotelName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.CancellationPolicy).HasMaxLength(2000);
        builder.Property(x => x.Currency).HasMaxLength(10);
    }
}

public class FloorConfiguration : IEntityTypeConfiguration<Floor>
{
    public void Configure(EntityTypeBuilder<Floor> builder)
    {
        builder.ToTable("Floors");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
    }
}

public class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
{
    public void Configure(EntityTypeBuilder<RoomType> builder)
    {
        builder.ToTable("RoomTypes");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.BasePrice).HasPrecision(18, 2);
    }
}

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.Property(x => x.RoomNumber).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.RoomNumber).IsUnique();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasOne(x => x.Floor).WithMany(f => f.Rooms).HasForeignKey(x => x.FloorId);
        builder.HasOne(x => x.RoomType).WithMany(t => t.Rooms).HasForeignKey(x => x.RoomTypeId);
    }
}

public class GuestConfiguration : IEntityTypeConfiguration<Guest>
{
    public void Configure(EntityTypeBuilder<Guest> builder)
    {
        builder.ToTable("Guests");
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.IdNumber).HasMaxLength(50);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(2000);
    }
}

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");
        builder.Property(x => x.ReservationNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.ReservationNumber).IsUnique();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.AmountPaid).HasPrecision(18, 2);
        builder.Property(x => x.RemainingAmount).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasOne(x => x.Guest).WithMany(g => g.Reservations).HasForeignKey(x => x.GuestId);
        builder.HasOne(x => x.Room).WithMany(r => r.Reservations).HasForeignKey(x => x.RoomId).IsRequired(false);
        builder.HasIndex(x => x.CheckInDate);
        builder.HasIndex(x => x.Status);
    }
}

public class ReservationChargeConfiguration : IEntityTypeConfiguration<ReservationCharge>
{
    public void Configure(EntityTypeBuilder<ReservationCharge> builder)
    {
        builder.ToTable("ReservationCharges");
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasOne(x => x.Reservation).WithMany(r => r.Charges).HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReservationPaymentConfiguration : IEntityTypeConfiguration<ReservationPayment>
{
    public void Configure(EntityTypeBuilder<ReservationPayment> builder)
    {
        builder.ToTable("ReservationPayments");
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.PaymentMethod).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasOne(x => x.Reservation).WithMany(r => r.Payments).HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.HotelCashBox).WithMany().HasForeignKey(x => x.HotelCashBoxId).IsRequired(false);
    }
}

public class HotelCashBoxConfiguration : IEntityTypeConfiguration<HotelCashBox>
{
    public void Configure(EntityTypeBuilder<HotelCashBox> builder)
    {
        builder.ToTable("HotelCashBoxes");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.OpeningBalance).HasPrecision(18, 2);
        builder.Property(x => x.CurrentBalance).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
    }
}

public class HotelVoucherConfiguration : IEntityTypeConfiguration<HotelVoucher>
{
    public void Configure(EntityTypeBuilder<HotelVoucher> builder)
    {
        builder.ToTable("HotelVouchers");
        builder.Property(x => x.VoucherNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.VoucherNumber).IsUnique();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasOne(x => x.HotelCashBox).WithMany().HasForeignKey(x => x.HotelCashBoxId);
        builder.HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationId).IsRequired(false);
        builder.HasOne(x => x.HotelExpense).WithMany().HasForeignKey(x => x.HotelExpenseId).IsRequired(false);
    }
}

public class HotelExpenseTypeConfiguration : IEntityTypeConfiguration<HotelExpenseType>
{
    public void Configure(EntityTypeBuilder<HotelExpenseType> builder)
    {
        builder.ToTable("HotelExpenseTypes");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(500);
    }
}

public class HotelExpenseConfiguration : IEntityTypeConfiguration<HotelExpense>
{
    public void Configure(EntityTypeBuilder<HotelExpense> builder)
    {
        builder.ToTable("HotelExpenses");
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasOne(x => x.ExpenseType).WithMany(t => t.Expenses).HasForeignKey(x => x.HotelExpenseTypeId);
        builder.HasOne(x => x.HotelCashBox).WithMany().HasForeignKey(x => x.HotelCashBoxId).IsRequired(false);
    }
}

public class RatePlanConfiguration : IEntityTypeConfiguration<RatePlan>
{
    public void Configure(EntityTypeBuilder<RatePlan> builder)
    {
        builder.ToTable("RatePlans");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.BasePrice).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasOne(x => x.RoomType).WithMany().HasForeignKey(x => x.RoomTypeId);
    }
}

public class RatePlanSeasonConfiguration : IEntityTypeConfiguration<RatePlanSeason>
{
    public void Configure(EntityTypeBuilder<RatePlanSeason> builder)
    {
        builder.ToTable("RatePlanSeasons");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PricePerNight).HasPrecision(18, 2);
        builder.HasOne(x => x.RatePlan).WithMany(p => p.Seasons).HasForeignKey(x => x.RatePlanId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class HousekeepingTaskConfiguration : IEntityTypeConfiguration<HousekeepingTask>
{
    public void Configure(EntityTypeBuilder<HousekeepingTask> builder)
    {
        builder.ToTable("HousekeepingTasks");
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.AssignedTo).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId);
    }
}
