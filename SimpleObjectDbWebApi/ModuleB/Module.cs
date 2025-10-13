using Basses.SimpleDocumentStore;

namespace SimpleObjectDbWebApi.ModuleB;

public static class Module
{
    public static IServiceCollection AddModuleB(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<Repository>();
        return services;
    }

    public static void RegisterModuleBEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/b/{id:int}", async (Repository repository, CancellationToken cancellationToken, int id) =>
        {
            var b = await repository.GetByIdAsync(id, cancellationToken);
            return b == null ? Results.NotFound() : Results.Ok(b);
        })
        .WithName("GetB")
        .WithTags("Module B");

        app.MapPost("/b", async (Repository repository, CancellationToken cancellationToken, B b) =>
        {
            var existingA = await repository.GetByIdAsync(b.Id, cancellationToken);
            if (existingA == null)
            {
                await repository.CreateAsync(b, cancellationToken);
            }
            else
            {
                await repository.UpdateAsync(b, cancellationToken);
            }

            return Results.Ok();
        })
        .WithName("SetB")
        .WithTags("Module B");
    }

    public static void ConfigureDocumentStoreForModuleB(this DocumentStoreConfiguration config)
    {
        config.RegisterDataType<B>(b => b.Id, "module_b");
    }
}
