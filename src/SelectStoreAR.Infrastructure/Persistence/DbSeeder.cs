using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext)
    {
        if (dbContext.Categories.Any())
        {
            return;
        }

        Category[] rootCategories =
        [
            Category.Create("Celulares", sortOrder: 1),
            Category.Create("Consolas", sortOrder: 2),
            Category.Create("Perfumes", sortOrder: 3),
            Category.Create("Tecnologia", sortOrder: 4),
            Category.Create("Accesorios", sortOrder: 5),
        ];

        await dbContext.Categories.AddRangeAsync(rootCategories).ConfigureAwait(false);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        Category celulares = rootCategories.First(c => c.Slug.Value == "celulares");
        Category consolas = rootCategories.First(c => c.Slug.Value == "consolas");

        Category[] subCategories =
        [
            Category.Create("Samsung", celulares.Id, 1),
            Category.Create("Apple", celulares.Id, 2),
            Category.Create("Xiaomi", celulares.Id, 3),
            Category.Create("PlayStation", consolas.Id, 1),
            Category.Create("Nintendo", consolas.Id, 2),
        ];

        await dbContext.Categories.AddRangeAsync(subCategories).ConfigureAwait(false);

        if (!dbContext.ExchangeRates.Any())
        {
            ExchangeRate exchangeRate = ExchangeRate.Create(1250.00m, "blue");
            await dbContext.ExchangeRates.AddAsync(exchangeRate).ConfigureAwait(false);
        }

        if (!dbContext.SiteConfigs.Any())
        {
            SiteConfig[] configs =
            [
                SiteConfig.Create("whatsapp_phone", "+5493881234567"),
                SiteConfig.Create("global_markup", "25"),
                SiteConfig.Create("site_name", "SelectStoreAR"),
                SiteConfig.Create("instagram_url", "https://instagram.com/selectstorear"),
                SiteConfig.Create("delivery_days", "7"),
            ];

            await dbContext.SiteConfigs.AddRangeAsync(configs).ConfigureAwait(false);
        }

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
