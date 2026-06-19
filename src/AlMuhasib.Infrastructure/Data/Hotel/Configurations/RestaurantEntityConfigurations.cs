using AlMuhasib.Core.Entities.Hotel.Restaurant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Hotel.Configurations;

public class RestaurantIngredientConfiguration : IEntityTypeConfiguration<RestaurantIngredient>
{
    public void Configure(EntityTypeBuilder<RestaurantIngredient> builder)
    {
        builder.ToTable("RestaurantIngredients");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Unit).HasMaxLength(50);
        builder.Property(x => x.MinQuantity).HasPrecision(18, 4);
        builder.Property(x => x.AverageCost).HasPrecision(18, 4);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasIndex(x => x.Name);
    }
}

public class RestaurantIngredientStockConfiguration : IEntityTypeConfiguration<RestaurantIngredientStock>
{
    public void Configure(EntityTypeBuilder<RestaurantIngredientStock> builder)
    {
        builder.ToTable("RestaurantIngredientStocks");
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.HasIndex(x => x.RestaurantIngredientId).IsUnique();
        builder.HasOne(x => x.Ingredient).WithOne(i => i.Stock).HasForeignKey<RestaurantIngredientStock>(x => x.RestaurantIngredientId);
    }
}

public class RestaurantMenuCategoryConfiguration : IEntityTypeConfiguration<RestaurantMenuCategory>
{
    public void Configure(EntityTypeBuilder<RestaurantMenuCategory> builder)
    {
        builder.ToTable("RestaurantMenuCategories");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ColorHex).HasMaxLength(20);
    }
}

public class RestaurantRecipeConfiguration : IEntityTypeConfiguration<RestaurantRecipe>
{
    public void Configure(EntityTypeBuilder<RestaurantRecipe> builder)
    {
        builder.ToTable("RestaurantRecipes");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(1000);
    }
}

public class RestaurantMenuItemConfiguration : IEntityTypeConfiguration<RestaurantMenuItem>
{
    public void Configure(EntityTypeBuilder<RestaurantMenuItem> builder)
    {
        builder.ToTable("RestaurantMenuItems");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Barcode).HasMaxLength(50);
        builder.Property(x => x.SalePrice).HasPrecision(18, 2);
        builder.Property(x => x.ImagePath).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasOne(x => x.Category).WithMany(c => c.MenuItems).HasForeignKey(x => x.RestaurantMenuCategoryId);
        builder.HasOne(x => x.Recipe).WithOne(r => r.MenuItem).HasForeignKey<RestaurantMenuItem>(x => x.RecipeId).IsRequired(false);
    }
}

public class RestaurantRecipeLineConfiguration : IEntityTypeConfiguration<RestaurantRecipeLine>
{
    public void Configure(EntityTypeBuilder<RestaurantRecipeLine> builder)
    {
        builder.ToTable("RestaurantRecipeLines");
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.HasOne(x => x.Recipe).WithMany(r => r.Lines).HasForeignKey(x => x.RestaurantRecipeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Ingredient).WithMany(i => i.RecipeLines).HasForeignKey(x => x.RestaurantIngredientId);
    }
}

public class RestaurantTableConfiguration : IEntityTypeConfiguration<RestaurantTable>
{
    public void Configure(EntityTypeBuilder<RestaurantTable> builder)
    {
        builder.ToTable("RestaurantTables");
        builder.Property(x => x.TableNumber).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.TableNumber).IsUnique();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(500);
    }
}

public class RestaurantOrderConfiguration : IEntityTypeConfiguration<RestaurantOrder>
{
    public void Configure(EntityTypeBuilder<RestaurantOrder> builder)
    {
        builder.ToTable("RestaurantOrders");
        builder.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.Property(x => x.OrderType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.KitchenStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.SubTotal).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.CogsAmount).HasPrecision(18, 2);
        builder.Property(x => x.GrossProfit).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasOne(x => x.Table).WithMany(t => t.Orders).HasForeignKey(x => x.RestaurantTableId).IsRequired(false);
        builder.HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationId).IsRequired(false);
        builder.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).IsRequired(false);
        builder.HasOne(x => x.Guest).WithMany().HasForeignKey(x => x.GuestId).IsRequired(false);
        builder.HasOne(x => x.ReservationCharge).WithMany().HasForeignKey(x => x.ReservationChargeId).IsRequired(false);
        builder.HasIndex(x => x.OrderDate);
        builder.HasIndex(x => x.Status);
    }
}

public class RestaurantOrderLineConfiguration : IEntityTypeConfiguration<RestaurantOrderLine>
{
    public void Configure(EntityTypeBuilder<RestaurantOrderLine> builder)
    {
        builder.ToTable("RestaurantOrderLines");
        builder.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);
        builder.Property(x => x.CogsAmount).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasOne(x => x.Order).WithMany(o => o.Lines).HasForeignKey(x => x.RestaurantOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.MenuItem).WithMany().HasForeignKey(x => x.RestaurantMenuItemId);
    }
}

public class RestaurantOrderPaymentConfiguration : IEntityTypeConfiguration<RestaurantOrderPayment>
{
    public void Configure(EntityTypeBuilder<RestaurantOrderPayment> builder)
    {
        builder.ToTable("RestaurantOrderPayments");
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasOne(x => x.Order).WithMany(o => o.Payments).HasForeignKey(x => x.RestaurantOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.HotelCashBox).WithMany().HasForeignKey(x => x.HotelCashBoxId).IsRequired(false);
    }
}

public class RestaurantStockMovementConfiguration : IEntityTypeConfiguration<RestaurantStockMovement>
{
    public void Configure(EntityTypeBuilder<RestaurantStockMovement> builder)
    {
        builder.ToTable("RestaurantStockMovements");
        builder.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.UnitCost).HasPrecision(18, 4);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasOne(x => x.Ingredient).WithMany(i => i.StockMovements).HasForeignKey(x => x.RestaurantIngredientId);
        builder.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.RestaurantOrderId).IsRequired(false);
        builder.HasIndex(x => x.MovementDate);
    }
}
