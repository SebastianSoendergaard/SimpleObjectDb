using Basses.SimpleDocumentStore;

namespace SimpleObjectDbWebApi.ModuleA;

public static class Module
{
    public static IServiceCollection AddModuleA(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<Repository>();
        return services;
    }

    public static void RegisterModuleAEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/a/{id:int}", async (Repository repository, CancellationToken cancellationToken, int id) =>
        {
            var a = await repository.GetByIdAsync(id, cancellationToken);
            return a == null ? Results.NotFound() : Results.Ok(a);
        })
        .WithName("GetA")
        .WithTags("Module A");

        app.MapPost("/a", async (Repository repository, CancellationToken cancellationToken, A a) =>
        {
            var existingA = await repository.GetByIdAsync(a.Id, cancellationToken);
            if (existingA == null)
            {
                await repository.CreateAsync(a, cancellationToken);
            }
            else
            {
                await repository.UpdateAsync(a, cancellationToken);
            }

            return Results.Ok();
        })
        .WithName("SetA")
        .WithTags("Module A");
    }

    public static void ConfigureDocumentStoreForModuleA(this DocumentStoreConfiguration config)
    {
        config.RegisterDataType<A>(a => a.Id, "module_a");
    }
}
