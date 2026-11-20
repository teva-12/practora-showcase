using FluentAssertions;
using Moq;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Infrastructure.Services;
using Xunit;

namespace ShopElegance.Tests.Unit.Services;

public class CategoryServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICategoryRepository> _catRepoMock;
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _uowMock     = new Mock<IUnitOfWork>();
        _catRepoMock = new Mock<ICategoryRepository>();
        _uowMock.Setup(u => u.Categories).Returns(_catRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _sut = new CategoryService(_uowMock.Object);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_ReturnsMappedDtos()
    {
        var cats = TestDataBuilder.CategoryFaker.Generate(3);
        int i = 1;
        cats.ForEach(c => { c.Id = i++; c.SubCategories = new List<Category>(); });
        _catRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(cats);

        var result = await _sut.GetAllCategoriesAsync();

        result.Should().HaveCount(3);
        result.All(c => !string.IsNullOrEmpty(c.Name)).Should().BeTrue();
        result.All(c => !string.IsNullOrEmpty(c.Slug)).Should().BeTrue();
    }

    [Fact]
    public async Task GetCategoryBySlugAsync_ExistingSlug_ReturnsDto()
    {
        var cat = TestDataBuilder.CategoryFaker.Generate();
        cat.SubCategories = new List<Category>();
        _catRepoMock.Setup(r => r.GetBySlugAsync(cat.Slug)).ReturnsAsync(cat);

        var result = await _sut.GetCategoryBySlugAsync(cat.Slug);

        result.Should().NotBeNull();
        result!.Slug.Should().Be(cat.Slug);
        result.Name.Should().Be(cat.Name);
    }

    [Fact]
    public async Task GetCategoryBySlugAsync_NonExisting_ReturnsNull()
    {
        _catRepoMock.Setup(r => r.GetBySlugAsync(It.IsAny<string>())).ReturnsAsync((Category?)null);

        var result = await _sut.GetCategoryBySlugAsync("ghost-category");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateCategoryAsync_ValidDto_CreatesCategory()
    {
        _catRepoMock.Setup(r => r.AddAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);

        var dto = new CreateCategoryDto
        {
            Name = "New Category",
            Description = "A fresh category",
            IsActive = true,
            SortOrder = 5
        };

        var result = await _sut.CreateCategoryAsync(dto);

        result.Should().NotBeNull();
        result.Name.Should().Be("New Category");
        result.Slug.Should().Be("new-category");
        _catRepoMock.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryAsync_ExistingCategory_UpdatesFields()
    {
        var existing = TestDataBuilder.CategoryFaker.Generate();
        existing.SubCategories = new List<Category>();
        _catRepoMock.Setup(r => r.GetByIdAsync(existing.Id)).ReturnsAsync(existing);

        var dto = new UpdateCategoryDto
        {
            Id = existing.Id,
            Name = "Updated Name",
            Description = "Updated description",
            IsActive = false,
            SortOrder = 10
        };

        var result = await _sut.UpdateCategoryAsync(existing.Id, dto);

        existing.Name.Should().Be("Updated Name");
        existing.IsActive.Should().BeFalse();
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryAsync_NonExisting_ThrowsKeyNotFoundException()
    {
        _catRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Category?)null);

        Func<Task> act = () => _sut.UpdateCategoryAsync(999, new UpdateCategoryDto { Id = 999, Name = "X" });
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteCategoryAsync_ExistingCategory_SoftDeletes()
    {
        var cat = TestDataBuilder.CategoryFaker.Generate();
        _catRepoMock.Setup(r => r.GetByIdAsync(cat.Id)).ReturnsAsync(cat);

        await _sut.DeleteCategoryAsync(cat.Id);

        cat.IsDeleted.Should().BeTrue();
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
