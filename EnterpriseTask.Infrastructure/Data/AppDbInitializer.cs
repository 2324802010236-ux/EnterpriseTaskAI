using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseTask.Infrastructure.Data;

public static class AppDbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();
        await DbSeeder.SeedAsync(serviceProvider);
    }
}
