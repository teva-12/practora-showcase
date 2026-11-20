using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Enums;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Core.Interfaces.Services;
using ShopElegance.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using ShopElegance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopElegance.Tests.Unit.Services;

public class OrderServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ILogger<OrderService>> _loggerMock;
    private readonly Mock<IPriceCalculationService> _priceServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _orderRepoMock = new Mock<IOrderRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        _loggerMock = new Mock<ILogger<OrderService>>();
        _priceServiceMock = new Mock<IPriceCalculationService>();
        _emailServiceMock = new Mock<IEmailService>();
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _uowMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
        _uowMock.Setup(u => u.Products).Returns(_productRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _sut = new OrderService(_uowMock.Object, _userManagerMock.Object, _loggerMock.Object, _priceServiceMock.Object, _emailServiceMock.Object, context);
    }

    private static ShippingAddressDto MakeAddress() => new()
    {
        FirstName = "John", LastName = "Doe", Email = "john@test.com",
        Phone = "555-1234", AddressLine1 = "123 Main St",
        City = "New York", State = "NY", PostalCode = "10001", Country = "United States"
    };

    // ─── GetOrderByIdAsync ───────────────────────────────────────────────
    [Fact]
    public async Task GetOrderByIdAsync_ExistingOrder_ReturnsDto()
    {
        var order = TestDataBuilder.OrderFaker.Generate();
        order.Id = 1;
        order.Items = new List<OrderItem>();
        order.StatusHistories = new List<OrderStatusHistory>();
        _orderRepoMock.Setup(r => r.GetWithDetailsAsync(1)).ReturnsAsync(order);

        var result = await _sut.GetOrderByIdAsync(1);

        result.Should().NotBeNull();
        result!.OrderNumber.Should().Be(order.OrderNumber);
    }

    [Fact]
    public async Task GetOrderByIdAsync_NonExistingOrder_ReturnsNull()
    {
        _orderRepoMock.Setup(r => r.GetWithDetailsAsync(999)).ReturnsAsync((Order?)null);

        var result = await _sut.GetOrderByIdAsync(999);

        result.Should().BeNull();
    }

    // ─── CreateOrderAsync ────────────────────────────────────────────────
    [Fact]
    public async Task CreateOrderAsync_ValidDto_CreatesOrder()
    {
        var product = new Product
        {
            Id = 1, Name = "Laptop", Price = 999.99m, StockQuantity = 10,
            TrackInventory = true, Type = ProductType.Physical,
            Description = "d", ShortDescription = "s"
        };
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _orderRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>()))
                      .ReturnsAsync(false);
        _orderRepoMock.Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

        var dto = new CreateOrderDto
        {
            UserId = "user1",
            ShippingAddress = MakeAddress(),
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 2, UnitPrice = 999.99m }
            },
            ShippingCost = 9.99m,
            TaxAmount = 16.00m
        };

        var result = await _sut.CreateOrderAsync(dto);

        result.Should().NotBeNull();
        result.Total.Should().BeApproximately(2025.97m, 0.01m);
        product.StockQuantity.Should().Be(8); // decremented
        product.SalesCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateOrderAsync_ProductNotFound_ThrowsKeyNotFoundException()
    {
        _productRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Product?)null);
        _orderRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>()))
                      .ReturnsAsync(false);

        var dto = new CreateOrderDto
        {
            UserId = "user1",
            ShippingAddress = MakeAddress(),
            Items = new List<CreateOrderItemDto> { new() { ProductId = 999, Quantity = 1, UnitPrice = 10m } }
        };

        Func<Task> act = () => _sut.CreateOrderAsync(dto);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CreateOrderAsync_EmptyItems_ThrowsInvalidOperationException()
    {
        var dto = new CreateOrderDto
        {
            UserId = "user1",
            ShippingAddress = MakeAddress(),
            Items = new List<CreateOrderItemDto>()
        };

        Func<Task> act = () => _sut.CreateOrderAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateOrderAsync_InsufficientStock_ThrowsInvalidOperationException_AndDoesNotDecrementStock()
    {
        var product = new Product
        {
            Id = 1, Name = "Laptop", Price = 999.99m, StockQuantity = 1,
            TrackInventory = true, Type = ProductType.Physical,
            Description = "d", ShortDescription = "s"
        };
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _orderRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>()))
                      .ReturnsAsync(false);

        var dto = new CreateOrderDto
        {
            UserId = "user1",
            ShippingAddress = MakeAddress(),
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 5, UnitPrice = 999.99m } }
        };

        Func<Task> act = () => _sut.CreateOrderAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>();
        product.StockQuantity.Should().Be(1); // untouched — nothing should have been decremented
        _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_ValidCoupon_AppliesDiscountAndIncrementsUsage()
    {
        var product = new Product
        {
            Id = 1, Name = "Laptop", Price = 100m, StockQuantity = 10,
            TrackInventory = true, Type = ProductType.Physical,
            Description = "d", ShortDescription = "s"
        };
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _orderRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>()))
                      .ReturnsAsync(false);

        var couponRepoMock = new Mock<ICouponRepository>();
        var coupon = new Coupon { Id = 1, Code = "SAVE10", Type = CouponType.Percentage, Value = 10, IsActive = true, UsageCount = 3 };
        couponRepoMock.Setup(r => r.GetByCodeAsync("SAVE10")).ReturnsAsync(coupon);
        _uowMock.Setup(u => u.Coupons).Returns(couponRepoMock.Object);

        var dto = new CreateOrderDto
        {
            UserId = "user1",
            ShippingAddress = MakeAddress(),
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 1, UnitPrice = 100m } },
            CouponCode = "SAVE10",
            ShippingCost = 0,
            TaxAmount = 0
        };

        var result = await _sut.CreateOrderAsync(dto);

        result.DiscountAmount.Should().Be(10m);
        result.Total.Should().Be(90m);
        coupon.UsageCount.Should().Be(4);
        couponRepoMock.Verify(r => r.Update(coupon), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_ExpiredCoupon_ThrowsInvalidOperationException()
    {
        var product = new Product { Id = 1, Name = "Laptop", Price = 100m, StockQuantity = 10, TrackInventory = true, Type = ProductType.Physical, Description = "d", ShortDescription = "s" };
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _orderRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>())).ReturnsAsync(false);

        var couponRepoMock = new Mock<ICouponRepository>();
        var coupon = new Coupon { Id = 1, Code = "EXPIRED", Type = CouponType.Percentage, Value = 10, IsActive = true, ExpiresAt = DateTime.UtcNow.AddDays(-1) };
        couponRepoMock.Setup(r => r.GetByCodeAsync("EXPIRED")).ReturnsAsync(coupon);
        _uowMock.Setup(u => u.Coupons).Returns(couponRepoMock.Object);

        var dto = new CreateOrderDto
        {
            UserId = "user1",
            ShippingAddress = MakeAddress(),
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 1, UnitPrice = 100m } },
            CouponCode = "EXPIRED"
        };

        Func<Task> act = () => _sut.CreateOrderAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateOrderAsync_WhiteCoatItem_PersistsCustomizationAndDecrementsVariantStock()
    {
        var variantRepoMock = new Mock<IWhiteCoatVariantRepository>();
        var customizationRepoMock = new Mock<ICustomizationRepository>();

        var variant = new WhiteCoatVariant
        {
            Id = 7, SKU = "WC-M-WHT", IsActive = true, StockQuantity = 5, PriceAdjustment = 20m,
            Product = new Product { Id = 1, Name = "White Coat", Price = 200m }
        };
        variantRepoMock.Setup(r => r.GetWithDetailsAsync(7)).ReturnsAsync(variant);
        _uowMock.Setup(u => u.WhiteCoatVariants).Returns(variantRepoMock.Object);
        _uowMock.Setup(u => u.Customizations).Returns(customizationRepoMock.Object);
        _orderRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>())).ReturnsAsync(false);

        var orderRepoMock = new Mock<IOrderRepository>();
        _uowMock.Setup(u => u.Orders).Returns(orderRepoMock.Object);

        Order? capturedOrder = null;
        orderRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => {
                System.Console.WriteLine($"DEBUG: AddAsync called with order: {o.OrderNumber}");
                capturedOrder = o;
            })
            .Returns(Task.CompletedTask);

        _priceServiceMock.Setup(p => p.GetCustomizationCostsAsync(It.IsAny<CreateCustomizationDto>())).ReturnsAsync((100m, 0m));

        var dto = new CreateOrderDto
        {
            UserId = "user1",
            ShippingAddress = MakeAddress(),
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductId = 1, Quantity = 1, UnitPrice = 320m, WhiteCoatVariantId = 7,
                    Customization = new CreateCustomizationDto
                    {
                        WhiteCoatVariantId = 7, EmbroideryEnabled = true, StudentName = "Salma",
                        EmbroideryFontId = 2, EmbroideryPosition = "LeftChest"
                    }
                }
            },
            ShippingCost = 0,
            TaxAmount = 0
        };

        var result = await _sut.CreateOrderAsync(dto);

        variant.StockQuantity.Should().Be(4);
        capturedOrder.Should().NotBeNull();
        var customization = capturedOrder!.Items.Single().Customization;
        customization.Should().NotBeNull();
        customization!.StudentName.Should().Be("Salma");
        customization.PriceAdjustment.Should().Be(100m);
        result.Items.Single().UnitPrice.Should().Be(320m); // 200 base + 20 variant + 100 fee
        result.Items.Single().WhiteCoatVariantId.Should().Be(7);
    }

    [Fact]
    public async Task CreateOrderAsync_WhiteCoatVariantInsufficientStock_ThrowsAndDoesNotCreateOrder()
    {
        var variantRepoMock = new Mock<IWhiteCoatVariantRepository>();
        var variant = new WhiteCoatVariant { Id = 7, SKU = "WC-M-WHT", IsActive = true, StockQuantity = 0, Product = new Product { Id = 1, Price = 200m } };
        variantRepoMock.Setup(r => r.GetWithDetailsAsync(7)).ReturnsAsync(variant);
        _uowMock.Setup(u => u.WhiteCoatVariants).Returns(variantRepoMock.Object);
        _orderRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>())).ReturnsAsync(false);

        var dto = new CreateOrderDto
        {
            UserId = "user1",
            ShippingAddress = MakeAddress(),
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 1, WhiteCoatVariantId = 7 } }
        };

        Func<Task> act = () => _sut.CreateOrderAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>();
        _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    // ─── UpdateOrderStatusAsync ──────────────────────────────────────────
    [Fact]
    public async Task UpdateOrderStatusAsync_ValidTransition_UpdatesStatus()
    {
        var order = TestDataBuilder.OrderFaker.Generate();
        order.Id = 1;
        order.Status = OrderStatus.Pending;
        order.Items = new List<OrderItem>();
        order.StatusHistories = new List<OrderStatusHistory>();
        _orderRepoMock.Setup(r => r.GetWithDetailsAsync(1)).ReturnsAsync(order);

        var dto = new UpdateOrderStatusDto
        {
            Status = OrderStatus.Confirmed,
            Comment = "Payment verified",
            ChangedByUserId = "admin1"
        };

        var result = await _sut.UpdateOrderStatusAsync(1, dto);

        result.Status.Should().Be("Confirmed");
        order.StatusHistories.Should().HaveCount(1);
        order.StatusHistories.First().Comment.Should().Be("Payment verified");
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ShippedStatus_SetsShippedAt()
    {
        var order = TestDataBuilder.OrderFaker.Generate();
        order.Id = 1;
        order.Items = new List<OrderItem>();
        order.StatusHistories = new List<OrderStatusHistory>();
        _orderRepoMock.Setup(r => r.GetWithDetailsAsync(1)).ReturnsAsync(order);

        var dto = new UpdateOrderStatusDto
        {
            Status = OrderStatus.Shipped,
            TrackingNumber = "TRACK123456"
        };

        var result = await _sut.UpdateOrderStatusAsync(1, dto);

        order.ShippedAt.Should().NotBeNull();
        order.TrackingNumber.Should().Be("TRACK123456");
        result.Status.Should().Be("Shipped");
    }

    // ─── GenerateOrderNumberAsync ────────────────────────────────────────
    [Fact]
    public async Task GenerateOrderNumberAsync_ReturnsFormattedOrderNumber()
    {
        _orderRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>()))
                      .ReturnsAsync(false);

        var orderNum = await _sut.GenerateOrderNumberAsync();

        orderNum.Should().StartWith("ORD-");
        orderNum.Should().MatchRegex(@"ORD-\d{8}-\d{4}");
    }

    // ─── GetUserOrdersAsync ──────────────────────────────────────────────
    [Fact]
    public async Task GetUserOrdersAsync_UserWithOrders_ReturnsOrders()
    {
        var orders = TestDataBuilder.OrderFaker.Generate(3);
        orders.ForEach(o => { o.Items = new List<OrderItem>(); o.StatusHistories = new List<OrderStatusHistory>(); });
        _orderRepoMock.Setup(r => r.GetUserOrdersAsync("user1")).ReturnsAsync(orders);

        var result = await _sut.GetUserOrdersAsync("user1");

        result.Should().HaveCount(3);
    }

    // ─── Checkout transaction safety ─────────────────────────────────────
    [Fact]
    public async Task CreateOrderAsync_ConcurrencyConflictOnSave_ThrowsFriendlyErrorAndRollsBack()
    {
        var product = new Product { Id = 1, Name = "Laptop", Price = 100m, StockQuantity = 10, TrackInventory = true, Type = ProductType.Physical, Description = "d", ShortDescription = "s" };
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _orderRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>())).ReturnsAsync(false);
        _orderRepoMock.Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

        // Force DbContext (Mocked uow) to throw
        var mockContext = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
        mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new DbUpdateConcurrencyException("conflict"));
        _uowMock.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new DbUpdateConcurrencyException("conflict"));

        var dto = new CreateOrderDto
        {
            UserId = "user1",
            ShippingAddress = MakeAddress(),
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 1, UnitPrice = 100m } }
        };

        Func<Task> action = () => _sut.CreateOrderAsync(dto);
        await action.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}
