using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopElegance.Core.Entities;

namespace ShopElegance.Infrastructure.Data.Configurations;

public class WhiteCoatVariantConfiguration : IEntityTypeConfiguration<WhiteCoatVariant>
{
    public void Configure(EntityTypeBuilder<WhiteCoatVariant> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.CoatType).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Size).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Fabric).IsRequired().HasMaxLength(100);
        builder.Property(v => v.Color).IsRequired().HasMaxLength(50);
        builder.Property(v => v.SKU).IsRequired().HasMaxLength(100);
        builder.Property(v => v.Barcode).HasMaxLength(100);
        builder.Property(v => v.PriceAdjustment).HasPrecision(18, 2);
        builder.Property(v => v.RowVersion).IsRowVersion();

        builder.HasIndex(v => v.SKU).IsUnique();
        builder.HasIndex(v => v.ProductId);
        builder.HasIndex(v => v.IsActive);
        builder.HasIndex(v => new { v.ProductId, v.Size, v.Color, v.Fabric, v.CoatType });

        builder.HasOne(v => v.Product)
               .WithMany()
               .HasForeignKey(v => v.ProductId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}

public class WhiteCoatVariantImageConfiguration : IEntityTypeConfiguration<WhiteCoatVariantImage>
{
    public void Configure(EntityTypeBuilder<WhiteCoatVariantImage> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Url).IsRequired().HasMaxLength(500);
        builder.Property(i => i.AltText).HasMaxLength(200);

        builder.HasOne(i => i.WhiteCoatVariant)
               .WithMany(v => v.Images)
               .HasForeignKey(i => i.WhiteCoatVariantId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}

public class EmbroideryFontConfiguration : IEntityTypeConfiguration<EmbroideryFont>
{
    public void Configure(EntityTypeBuilder<EmbroideryFont> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Name).IsRequired().HasMaxLength(100);
        builder.Property(f => f.CssFontFamily).IsRequired().HasMaxLength(200);
        builder.Property(f => f.PreviewImageUrl).HasMaxLength(500);
        builder.HasIndex(f => f.Name).IsUnique();

        builder.HasQueryFilter(f => !f.IsDeleted);
    }
}

public class LogoConfiguration : IEntityTypeConfiguration<Logo>
{
    public void Configure(EntityTypeBuilder<Logo> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name).IsRequired().HasMaxLength(100);
        builder.Property(l => l.ImageUrl).IsRequired().HasMaxLength(500);
        builder.HasIndex(l => l.Type);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}

public class CustomizationConfiguration : IEntityTypeConfiguration<Customization>
{
    public void Configure(EntityTypeBuilder<Customization> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.StudentName).HasMaxLength(150);
        builder.Property(c => c.DoctorName).HasMaxLength(150);
        builder.Property(c => c.Faculty).HasMaxLength(150);
        builder.Property(c => c.Department).HasMaxLength(150);
        builder.Property(c => c.EmbroideryTextColor).HasMaxLength(7);
        builder.Property(c => c.CustomLogoUrl).HasMaxLength(500);
        builder.Property(c => c.PreviewImageUrl).HasMaxLength(500);
        builder.Property(c => c.PriceAdjustment).HasPrecision(18, 2);
        builder.Property(c => c.EmbroideryCost).HasPrecision(18, 2);
        builder.Property(c => c.LogoCost).HasPrecision(18, 2);

        builder.HasOne(c => c.WhiteCoatVariant)
               .WithMany(v => v.Customizations)
               .HasForeignKey(c => c.WhiteCoatVariantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.EmbroideryFont)
               .WithMany()
               .HasForeignKey(c => c.EmbroideryFontId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Logo)
               .WithMany()
               .HasForeignKey(c => c.LogoId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

public class CustomizationHistoryConfiguration : IEntityTypeConfiguration<CustomizationHistory>
{
    public void Configure(EntityTypeBuilder<CustomizationHistory> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.ChangedField).IsRequired().HasMaxLength(100);
        builder.Property(h => h.OldValue).HasMaxLength(1000);
        builder.Property(h => h.NewValue).HasMaxLength(1000);
        builder.HasIndex(h => h.CustomizationId);

        builder.HasOne(h => h.Customization)
               .WithMany(c => c.History)
               .HasForeignKey(h => h.CustomizationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(h => !h.IsDeleted);
    }
}

public class ShippingCustomizationConfiguration : IEntityTypeConfiguration<ShippingCustomization>
{
    public void Configure(EntityTypeBuilder<ShippingCustomization> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.HandlingInstructions).HasMaxLength(500);
        builder.Property(s => s.ExtraShippingFee).HasPrecision(18, 2);
        builder.HasIndex(s => s.CustomizationId).IsUnique();

        builder.HasOne(s => s.Customization)
               .WithOne(c => c.ShippingCustomization)
               .HasForeignKey<ShippingCustomization>(s => s.CustomizationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}

/// <summary>Extends the existing ProductConfiguration with an optimistic concurrency token for checkout stock safety.</summary>
public class ProductConcurrencyConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}

/// <summary>Extends the existing CartItem configuration with White Coat FKs.</summary>
public class CartItemWhiteCoatConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasOne(ci => ci.WhiteCoatVariant)
               .WithMany()
               .HasForeignKey(ci => ci.WhiteCoatVariantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ci => ci.Customization)
               .WithMany()
               .HasForeignKey(ci => ci.CustomizationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Extends the existing OrderItem configuration with White Coat FKs.</summary>
public class OrderItemWhiteCoatConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasOne(oi => oi.WhiteCoatVariant)
               .WithMany()
               .HasForeignKey(oi => oi.WhiteCoatVariantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(oi => oi.Customization)
               .WithOne()
               .HasForeignKey<OrderItem>(oi => oi.CustomizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
