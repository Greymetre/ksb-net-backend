using Application.DTOs.Products;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private const int MaxRows = 50000;
    private readonly AppDbContext _dbContext;

    public ProductRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ProductSegmentDto>> GetSegmentsAsync(string? search, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = SegmentQuery(includeInactive);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.CategoryName.Contains(search.Trim()));
        return await ProjectSegments(query.OrderByDescending(x => x.Id).Take(MaxRows)).ToListAsync(cancellationToken);
    }

    public Task<ProductSegmentDto?> GetSegmentAsync(ulong id, CancellationToken cancellationToken) =>
        ProjectSegments(SegmentQuery(true).Where(x => x.Id == id)).FirstOrDefaultAsync(cancellationToken);

    public async Task<ProductSegmentDto> CreateSegmentAsync(ProductSegmentRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var segment = new ProductCategory
        {
            Active = NormalizeActive(request.Active),
            CategoryName = request.Name!.Trim(),
            CategoryImage = string.Empty,
            SapCode = NormalizeText(request.SapCode),
            CreatedBy = actorUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _dbContext.ProductCategories.AddAsync(segment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetSegmentAsync(segment.Id, cancellationToken))!;
    }

    public async Task<ProductSegmentDto?> UpdateSegmentAsync(ulong id, ProductSegmentRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var segment = await _dbContext.ProductCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (segment is null) return null;
        if (!string.IsNullOrWhiteSpace(request.Name)) segment.CategoryName = request.Name.Trim();
        if (request.SapCode is not null) segment.SapCode = NormalizeText(request.SapCode);
        segment.Active = NormalizeActive(request.Active, segment.Active);
        segment.UpdatedBy = actorUserId;
        segment.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetSegmentAsync(id, cancellationToken);
    }

    public async Task<ProductSegmentDto?> SetSegmentActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var segment = await _dbContext.ProductCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (segment is null) return null;
        segment.Active = NormalizeActive(active, Toggle(segment.Active));
        segment.UpdatedBy = actorUserId;
        segment.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetSegmentAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteSegmentAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var segment = await _dbContext.ProductCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (segment is null) return false;
        segment.Active = "N";
        segment.DeletedAt = DateTime.UtcNow;
        segment.UpdatedBy = actorUserId;
        segment.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<ProductFamilyDto>> GetFamiliesAsync(ulong? segmentId, string? search, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = FamilyQuery(includeInactive);
        if (segmentId.HasValue) query = query.Where(x => x.CategoryId == segmentId);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.SubcategoryName.Contains(search.Trim()));
        return await ProjectFamilies(query.OrderByDescending(x => x.Id).Take(MaxRows)).ToListAsync(cancellationToken);
    }

    public Task<ProductFamilyDto?> GetFamilyAsync(ulong id, CancellationToken cancellationToken) =>
        ProjectFamilies(FamilyQuery(true).Where(x => x.Id == id)).FirstOrDefaultAsync(cancellationToken);

    public async Task<ProductFamilyDto> CreateFamilyAsync(ProductFamilyRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var family = new ProductFamily
        {
            Active = NormalizeActive(request.Active),
            SubcategoryName = request.Name!.Trim(),
            SubcategoryImage = string.Empty,
            CategoryId = request.SegmentId,
            SapCode = NormalizeText(request.SapCode),
            CreatedBy = actorUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _dbContext.ProductFamilies.AddAsync(family, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetFamilyAsync(family.Id, cancellationToken))!;
    }

    public async Task<ProductFamilyDto?> UpdateFamilyAsync(ulong id, ProductFamilyRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var family = await _dbContext.ProductFamilies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (family is null) return null;
        if (!string.IsNullOrWhiteSpace(request.Name)) family.SubcategoryName = request.Name.Trim();
        if (request.SegmentId.HasValue) family.CategoryId = request.SegmentId;
        if (request.SapCode is not null) family.SapCode = NormalizeText(request.SapCode);
        family.Active = NormalizeActive(request.Active, family.Active);
        family.UpdatedBy = actorUserId;
        family.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetFamilyAsync(id, cancellationToken);
    }

    public async Task<ProductFamilyDto?> SetFamilyActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var family = await _dbContext.ProductFamilies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (family is null) return null;
        family.Active = NormalizeActive(active, Toggle(family.Active));
        family.UpdatedBy = actorUserId;
        family.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetFamilyAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteFamilyAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var family = await _dbContext.ProductFamilies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (family is null) return false;
        family.Active = "N";
        family.DeletedAt = DateTime.UtcNow;
        family.UpdatedBy = actorUserId;
        family.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<ProductDto>> GetProductsAsync(ulong? segmentId, ulong? familyId, string? search, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = ProductQuery(includeInactive);
        if (segmentId.HasValue) query = query.Where(x => x.CategoryId == segmentId);
        if (familyId.HasValue) query = query.Where(x => x.SubcategoryId == familyId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(x => x.ProductName.Contains(value) || x.PartNo.Contains(value));
        }
        return await ProjectProducts(query.OrderByDescending(x => x.Id).Take(MaxRows)).ToListAsync(cancellationToken);
    }

    public Task<ProductDto?> GetProductAsync(ulong id, CancellationToken cancellationToken) =>
        ProjectProducts(ProductQuery(true).Where(x => x.Id == id)).FirstOrDefaultAsync(cancellationToken);

    public async Task<ProductDto> CreateProductAsync(ProductRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var product = new Product
        {
            Active = NormalizeActive(request.Active),
            CategoryId = request.SegmentId,
            SubcategoryId = request.FamilyId,
            ProductName = request.ProductName!.Trim(),
            DisplayName = request.ProductName.Trim(),
            PartNo = request.PartNo!.Trim(),
            ProductImage = NormalizeText(request.Attachment) ?? string.Empty,
            CreatedBy = actorUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _dbContext.Products.AddAsync(product, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SaveProductDetailAsync(product.Id, request.Mrp, cancellationToken);
        return (await GetProductAsync(product.Id, cancellationToken))!;
    }

    public async Task<ProductDto?> UpdateProductAsync(ulong id, ProductRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null) return null;
        if (request.SegmentId.HasValue) product.CategoryId = request.SegmentId;
        if (request.FamilyId.HasValue) product.SubcategoryId = request.FamilyId;
        if (!string.IsNullOrWhiteSpace(request.ProductName))
        {
            product.ProductName = request.ProductName.Trim();
            product.DisplayName = product.ProductName;
        }
        if (!string.IsNullOrWhiteSpace(request.PartNo)) product.PartNo = request.PartNo.Trim();
        if (request.Attachment is not null) product.ProductImage = NormalizeText(request.Attachment) ?? string.Empty;
        product.Active = NormalizeActive(request.Active, product.Active);
        product.UpdatedBy = actorUserId;
        product.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SaveProductDetailAsync(product.Id, request.Mrp, cancellationToken);
        return await GetProductAsync(id, cancellationToken);
    }

    public async Task<ProductDto?> SetProductActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null) return null;
        product.Active = NormalizeActive(active, Toggle(product.Active));
        product.UpdatedBy = actorUserId;
        product.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetProductAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteProductAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null) return false;
        product.Active = "N";
        product.DeletedAt = DateTime.UtcNow;
        product.UpdatedBy = actorUserId;
        product.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<ProductCategory> SegmentQuery(bool includeInactive)
    {
        var query = includeInactive ? _dbContext.ProductCategories.IgnoreQueryFilters().Where(x => x.DeletedAt == null) : _dbContext.ProductCategories.AsQueryable();
        return includeInactive ? query : query.Where(x => x.Active == "Y");
    }

    private IQueryable<ProductFamily> FamilyQuery(bool includeInactive)
    {
        var query = includeInactive ? _dbContext.ProductFamilies.IgnoreQueryFilters().Where(x => x.DeletedAt == null) : _dbContext.ProductFamilies.AsQueryable();
        return includeInactive ? query : query.Where(x => x.Active == "Y");
    }

    private IQueryable<Product> ProductQuery(bool includeInactive)
    {
        var query = includeInactive ? _dbContext.Products.IgnoreQueryFilters().Where(x => x.DeletedAt == null) : _dbContext.Products.AsQueryable();
        return includeInactive ? query : query.Where(x => x.Active == "Y");
    }

    private IQueryable<ProductSegmentDto> ProjectSegments(IQueryable<ProductCategory> query) =>
        from segment in query.AsNoTracking()
        join createdBy in _dbContext.Users.AsNoTracking() on segment.CreatedBy equals createdBy.Id into userJoin
        from createdBy in userJoin.DefaultIfEmpty()
        select new ProductSegmentDto
        {
            Id = segment.Id,
            Active = segment.Active,
            Name = segment.CategoryName,
            SapCode = segment.SapCode,
            CreatedBy = segment.CreatedBy,
            CreatedByName = createdBy == null ? null : createdBy.Name,
            CreatedAt = segment.CreatedAt
        };

    private IQueryable<ProductFamilyDto> ProjectFamilies(IQueryable<ProductFamily> query) =>
        from family in query.AsNoTracking()
        join segment in _dbContext.ProductCategories.IgnoreQueryFilters().AsNoTracking() on family.CategoryId equals segment.Id into segmentJoin
        from segment in segmentJoin.DefaultIfEmpty()
        join createdBy in _dbContext.Users.AsNoTracking() on family.CreatedBy equals createdBy.Id into userJoin
        from createdBy in userJoin.DefaultIfEmpty()
        select new ProductFamilyDto
        {
            Id = family.Id,
            Active = family.Active,
            Name = family.SubcategoryName,
            SegmentId = family.CategoryId,
            SegmentName = segment == null ? null : segment.CategoryName,
            SapCode = family.SapCode,
            CreatedBy = family.CreatedBy,
            CreatedByName = createdBy == null ? null : createdBy.Name,
            CreatedAt = family.CreatedAt
        };

    private IQueryable<ProductDto> ProjectProducts(IQueryable<Product> query) =>
        from product in query.AsNoTracking()
        join segment in _dbContext.ProductCategories.IgnoreQueryFilters().AsNoTracking() on product.CategoryId equals segment.Id into segmentJoin
        from segment in segmentJoin.DefaultIfEmpty()
        join family in _dbContext.ProductFamilies.IgnoreQueryFilters().AsNoTracking() on product.SubcategoryId equals family.Id into familyJoin
        from family in familyJoin.DefaultIfEmpty()
        join createdBy in _dbContext.Users.AsNoTracking() on product.CreatedBy equals createdBy.Id into userJoin
        from createdBy in userJoin.DefaultIfEmpty()
        let detail = _dbContext.ProductDetails.AsNoTracking().Where(x => x.ProductId == product.Id).OrderByDescending(x => x.Id).FirstOrDefault()
        select new ProductDto
        {
            Id = product.Id,
            Active = product.Active,
            SegmentId = product.CategoryId,
            SegmentName = segment == null ? null : segment.CategoryName,
            FamilyId = product.SubcategoryId,
            FamilyName = family == null ? null : family.SubcategoryName,
            PartNo = product.PartNo,
            ProductName = product.ProductName,
            Mrp = detail == null ? null : detail.Mrp,
            Attachment = product.ProductImage,
            CreatedBy = product.CreatedBy,
            CreatedByName = createdBy == null ? null : createdBy.Name,
            CreatedAt = product.CreatedAt
        };

    private async Task SaveProductDetailAsync(ulong productId, decimal? mrp, CancellationToken cancellationToken)
    {
        mrp ??= 0;
        var detail = await _dbContext.ProductDetails.FirstOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (detail is null)
        {
            detail = new ProductDetail { ProductId = productId, Active = "Y", CreatedAt = DateTime.UtcNow };
            await _dbContext.ProductDetails.AddAsync(detail, cancellationToken);
        }
        detail.Mrp = mrp;
        detail.Price = mrp;
        detail.SellingPrice = mrp;
        detail.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeActive(string? active, string fallback = "Y") =>
        string.IsNullOrWhiteSpace(active) ? fallback : active.Trim().Equals("N", StringComparison.OrdinalIgnoreCase) ? "N" : "Y";

    private static string Toggle(string active) => active.Equals("Y", StringComparison.OrdinalIgnoreCase) ? "N" : "Y";

    private static string? NormalizeText(string? value)
    {
        if (value is null) return null;
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
