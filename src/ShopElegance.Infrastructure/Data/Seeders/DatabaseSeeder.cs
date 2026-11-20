using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Enums;
using ShopElegance.Infrastructure.Data;

namespace ShopElegance.Infrastructure.Data.Seeders;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            try
            {
                await context.Database.MigrateAsync();
            }
            catch (InvalidOperationException)
            {
                await context.Database.EnsureCreatedAsync();
            }

            await SeedRolesAsync(roleManager);
            await SeedAdminUserAsync(userManager, env, config, logger);
            await SeedCategoriesAsync(context);
            await FixLegacyCategoryNamesAsync(context, logger);
            await SeedWelcomeCouponAsync(context);
            await SeedProductsAsync(context);
            await SeedCouponsAsync(context);

            logger.LogInformation("Database seeded successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "Admin", "Manager", "Customer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    /// <summary>
    /// Seeds a starter Admin account so the site is usable immediately after first run.
    ///
    /// SECURITY: in Development, a well-known email/password is used purely for local
    /// convenience. In every other environment (Staging/Production) the same well-known
    /// password must NEVER be created automatically — that would leave every deployment
    /// with a publicly guessable admin login. In non-Development environments this method
    /// only creates an admin account when both "AdminSeed:Email" and "AdminSeed:Password"
    /// are supplied via configuration (environment variables / secret manager), and it
    /// requires the password to meet the same complexity policy as any other account.
    /// If they are not supplied, no admin account is created and a warning is logged so the
    /// deployer knows they must create the first Admin account manually (e.g. via a one-time
    /// registration + manual role grant, or by setting AdminSeed:Email/AdminSeed:Password).
    /// </summary>
    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager, IHostEnvironment env, IConfiguration config, ILogger logger)
    {
        string adminEmail;
        string adminPassword;

        if (env.IsDevelopment())
        {
            adminEmail = config["AdminSeed:Email"] ?? "admin@practora.com";
            adminPassword = config["AdminSeed:Password"] ?? "Admin@123456!";
        }
        else
        {
            var configuredEmail = config["AdminSeed:Email"];
            var configuredPassword = config["AdminSeed:Password"];

            if (string.IsNullOrWhiteSpace(configuredEmail) || string.IsNullOrWhiteSpace(configuredPassword))
            {
                logger.LogWarning(
                    "No admin account was seeded because AdminSeed:Email / AdminSeed:Password are not " +
                    "configured for this environment. Set them as environment variables " +
                    "(AdminSeed__Email / AdminSeed__Password) on first deploy, then remove them once the " +
                    "admin account exists.");
                return;
            }

            adminEmail = configuredEmail;
            adminPassword = configuredPassword;
        }

        if (await userManager.FindByEmailAsync(adminEmail) != null) return;

        var admin = new ApplicationUser
        {
            UserName = adminEmail, Email = adminEmail,
            FirstName = "Practora", LastName = "Admin",
            EmailConfirmed = true, IsActive = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
        else
        {
            logger.LogError("Failed to seed admin account: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        // IgnoreQueryFilters(): a fully soft-deleted category table would make a filtered
        // AnyAsync() return false, causing this method to try to re-insert all 9 categories —
        // colliding with the still-present (soft-deleted) rows' unique Slugs. See the detailed
        // explanation on FixLegacyCategoryNamesAsync below for the same root cause.
        if (await context.Categories.IgnoreQueryFilters().AnyAsync()) return;

        var categories = new List<Category>
        {
            new() { Name = "Engineering Preparation", Slug = "engineering-foundation", IsActive = true, SortOrder = 1 },
            new() { Name = "Electronics", Slug = "electronics", IsActive = true, SortOrder = 2 },
            new() { Name = "Medicine", Slug = "human-medicine", IsActive = true, SortOrder = 3 },
            new() { Name = "Pharmacy", Slug = "pharmacy", IsActive = true, SortOrder = 4 },
            new() { Name = "Dentistry", Slug = "dentistry", IsActive = true, SortOrder = 5 },
            new() { Name = "Veterinary Medicine", Slug = "veterinary-medicine", IsActive = true, SortOrder = 6 },
            new() { Name = "Nursing", Slug = "nursing", IsActive = true, SortOrder = 7 },
            new() { Name = "Physical Therapy", Slug = "physical-therapy", IsActive = true, SortOrder = 8 },
            new() { Name = "Design Your White Coat", Slug = "design-white-coat", IsActive = true, SortOrder = 9 }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// One-time-safe fixup for databases that were originally seeded with the
    /// generic template category set (Electronics/Fashion/Home & Living/Books/
    /// Digital Downloads/Sports & Outdoors) before this project was customized
    /// for Practora. Runs on every startup but only touches rows that still
    /// need it, so it's a no-op once fixed — safe to leave in place.
    ///
    /// IMPORTANT: existence checks here are keyed by <b>Slug</b>, not Name, and query
    /// ALL rows including soft-deleted ones (<c>IgnoreQueryFilters()</c>). The database's
    /// unique index is on Slug (Category.Name has no uniqueness constraint at all), and it
    /// applies to soft-deleted rows too (it isn't a filtered index) — so Slug is the only
    /// column that can actually tell us "would inserting this row violate the constraint?".
    /// Two situations otherwise make a Name-based, filtered-query check unreliable and cause
    /// a duplicate-key exception on 'IX_Categories_Slug':
    ///   1. An admin renames a category's Name via the admin panel without changing its Slug
    ///      (Category.Name and Category.Slug are independent, editable fields — see
    ///      CategoryService.UpdateCategoryAsync). The row's Name no longer matches what this
    ///      method looks for, so a Name-only check thinks the category is "missing" and tries
    ///      to insert a new row — colliding with the existing row's still-unique Slug.
    ///   2. A category is soft-deleted (Category.IsDeleted = true). EF Core's global query
    ///      filter hides it from a normal `context.Categories` query, so it looks "missing" —
    ///      but the row (and its Slug) still physically exists in the table, and the unique
    ///      index still enforces against it.
    /// </summary>
    private static async Task FixLegacyCategoryNamesAsync(ApplicationDbContext context, ILogger logger)
    {
        var renameMap = new Dictionary<string, string>
        {
            ["Fashion"] = "Medicine",
            ["Home & Living"] = "Pharmacy",
            ["Books"] = "Dentistry",
            ["Digital Downloads"] = "Veterinary Medicine",
            ["Sports & Outdoors"] = "Nursing"
        };

        // IgnoreQueryFilters(): we need to see every row that could collide with the unique
        // Slug index, including soft-deleted ones — the index doesn't care about IsDeleted.
        //
        // NOTE: Category.Name has no uniqueness constraint in the database (only Slug does),
        // so real data can legitimately contain two rows with the exact same Name (e.g. two
        // "Drawing Tools" categories created independently). Building these lookups with
        // `.ToDictionary()` throws ArgumentException the instant it meets a duplicate key —
        // which happened here. We build them manually instead, keeping the first row we see
        // for any duplicated Name/Slug rather than crashing on it. This method never needs to
        // uniquely identify a row by Name anyway — Name is only ever used as a soft "does a
        // category already have this display name" hint alongside the authoritative Slug check.
        var existing = await context.Categories.IgnoreQueryFilters().ToListAsync();
        var byName = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        var bySlug = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        foreach (var cat in existing)
        {
            byName.TryAdd(cat.Name, cat);
            bySlug.TryAdd(cat.Slug, cat);
        }
        var changed = false;

        void Reindex(Category cat, string? oldName, string? oldSlug)
        {
            if (oldName != null) byName.Remove(oldName);
            if (oldSlug != null) bySlug.Remove(oldSlug);
            byName[cat.Name] = cat;
            bySlug[cat.Slug] = cat;
        }

        // --- Step 1: rename legacy categories to their Practora equivalents. ---
        // Renaming only ever changes Name, never Slug, so it can never collide with the
        // unique index by itself — but we still skip it if a category with the target Name
        // already exists elsewhere, to avoid ending up with two categories with the same Name.
        foreach (var cat in existing.ToList())
        {
            if (renameMap.TryGetValue(cat.Name, out var newName)
                && !byName.ContainsKey(newName))
            {
                var oldName = cat.Name;
                cat.Name = newName;
                cat.UpdatedAt = DateTime.UtcNow;
                Reindex(cat, oldName, null);
                changed = true;
            }
        }

        // --- Step 2: ensure every target category exists, keyed by Slug (the authoritative,
        // constraint-backed identity), reusing/reconciling any row we find instead of ever
        // blindly inserting. ---
        var targetCategories = new List<(string Name, string Slug, int SortOrder)>
        {
            ("Engineering Preparation", "engineering-foundation", 1),
            ("Electronics", "electronics", 2),
            ("Medicine", "human-medicine", 3),
            ("Pharmacy", "pharmacy", 4),
            ("Dentistry", "dentistry", 5),
            ("Veterinary Medicine", "veterinary-medicine", 6),
            ("Nursing", "nursing", 7),
            ("Physical Therapy", "physical-therapy", 8),
            ("Design Your White Coat", "design-white-coat", 9)
        };

        foreach (var target in targetCategories)
        {
            if (bySlug.TryGetValue(target.Slug, out var found))
            {
                // A row with this Slug already exists (possibly soft-deleted, possibly under a
                // different Name) — reuse and reconcile it rather than inserting a duplicate.
                var oldName = found.Name;
                var needsSave = false;

                if (found.IsDeleted)
                {
                    found.IsDeleted = false;
                    found.DeletedAt = null;
                    needsSave = true;
                }
                if (!string.Equals(found.Name, target.Name, StringComparison.OrdinalIgnoreCase))
                {
                    found.Name = target.Name;
                    needsSave = true;
                }
                if (found.SortOrder != target.SortOrder)
                {
                    found.SortOrder = target.SortOrder;
                    needsSave = true;
                }
                if (!found.IsActive)
                {
                    found.IsActive = true;
                    needsSave = true;
                }

                if (needsSave)
                {
                    found.UpdatedAt = DateTime.UtcNow;
                    Reindex(found, oldName, null);
                    changed = true;
                }
            }
            else if (!byName.ContainsKey(target.Name))
            {
                // No row with this Slug (and none under this Name either) — genuinely new.
                var newCat = new Category
                {
                    Name = target.Name,
                    Slug = target.Slug,
                    IsActive = true,
                    SortOrder = target.SortOrder
                };
                context.Categories.Add(newCat);
                bySlug[target.Slug] = newCat;
                byName[target.Name] = newCat;
                changed = true;
            }
            // else: a row exists under this exact Name but with a different Slug. We deliberately
            // leave it alone rather than touch its Slug — changing a live category's Slug can
            // break bookmarked/shared category URLs, and it's not needed to satisfy "every target
            // category exists by Slug or Name" for the storefront/homepage links that depend on this.
        }

        if (!changed) return;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Defense in depth: if a genuinely concurrent seed/admin edit still manages to race
            // us here, don't take the whole application down over a cosmetic category fixup —
            // log it and continue. Every check above is already keyed off the unique Slug column
            // and re-reads before writing, so this should be unreachable in normal operation.
            context.ChangeTracker.Clear();
            logger.LogWarning(ex,
                "FixLegacyCategoryNamesAsync could not save category changes (likely a concurrent " +
                "edit); continuing startup without them. This is safe to ignore unless it repeats " +
                "on every restart.");
        }
    }

    /// <summary>
    /// The homepage promo strip advertises "10% Off Your First Order — code
    /// WELCOME10", but nothing ever created that coupon in the database, so
    /// entering it at checkout always failed. Idempotent — safe on every startup.
    /// </summary>
    private static async Task SeedWelcomeCouponAsync(ApplicationDbContext context)
    {
        // IgnoreQueryFilters(): Coupon.Code is also uniquely indexed and unfiltered at the DB
        // level, so a soft-deleted WELCOME10 row would otherwise cause the same duplicate-key
        // failure as the categories case above.
        var exists = await context.Coupons.IgnoreQueryFilters().AnyAsync(c => c.Code == "WELCOME10");
        if (exists) return;

        context.Coupons.Add(new Coupon
        {
            Code = "WELCOME10",
            Description = "10% off your first order",
            Type = CouponType.Percentage,
            Value = 10,
            IsActive = true,
            ExpiresAt = null,
            UsageLimit = null
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(ApplicationDbContext context)
    {
        // Same soft-delete blind spot as SeedCategoriesAsync — Product.Slug is also unique
        // and the index isn't filtered, so it still blocks inserts against soft-deleted rows.
        if (await context.Products.IgnoreQueryFilters().AnyAsync()) return;

        var categories = await context.Categories.ToListAsync();
        var products = new List<Product>();

        foreach (var category in categories)
        {
            for (int i = 1; i <= 5; i++)
            {
                products.Add(new Product
                {
                    Name = $"{category.Name} Product {i}",
                    Slug = $"{category.Slug}-product-{i}",
                    ShortDescription = $"Short desc for {category.Name} product {i}.",
                    Description = $"<p>Full description for {category.Name} product {i}.</p>",
                    Price = 100m + (i * 10),
                    StockQuantity = 50,
                    IsActive = true,
                    Type = ProductType.Physical,
                    SKU = $"{category.Slug.ToUpper()}-00{i}",
                    CategoryId = category.Id
                });
            }
        }

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds a couple of extra demo coupons (SAVE20/SUMMER25) alongside WELCOME10.
    ///
    /// NOTE: this used to guard on a blanket `Coupons.AnyAsync()` — but SeedWelcomeCouponAsync
    /// runs immediately before this method and always creates WELCOME10 on a fresh database,
    /// which made that blanket check true from the very first startup and silently skipped
    /// SAVE20/SUMMER25 forever. Checking existence per-coupon (by Code, unfiltered so a
    /// soft-deleted coupon isn't re-inserted into its still-unique Code slot) fixes both that
    /// and the same duplicate-key class of bug described on FixLegacyCategoryNamesAsync.
    /// </summary>
    private static async Task SeedCouponsAsync(ApplicationDbContext context)
    {
        var coupons = new List<Coupon>
        {
            new()
            {
                Code = "WELCOME10", Description = "10% off your first order",
                Type = CouponType.Percentage, Value = 10,
                IsActive = true, UsageLimit = 1000
            },
            new()
            {
                Code = "SAVE20", Description = "$20 off orders over $100",
                Type = CouponType.FixedAmount, Value = 20,
                MinimumOrderAmount = 100, IsActive = true, UsageLimit = 500
            },
            new()
            {
                Code = "SUMMER25", Description = "25% off summer sale",
                Type = CouponType.Percentage, Value = 25,
                MaximumDiscountAmount = 50, IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddMonths(3)
            }
        };

        var existingCodes = await context.Coupons.IgnoreQueryFilters()
            .Select(c => c.Code)
            .ToListAsync();
        var existingCodeSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = coupons.Where(c => !existingCodeSet.Contains(c.Code)).ToList();
        if (toAdd.Count == 0) return;

        await context.Coupons.AddRangeAsync(toAdd);
        await context.SaveChangesAsync();
    }
}
