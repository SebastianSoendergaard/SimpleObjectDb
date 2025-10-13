using Basses.SimpleDocumentStore;
using Basses.SimpleDocumentStore.PostgreSql;
using SimpleObjectDbWebApi.ModuleA;
using SimpleObjectDbWebApi.ModuleB;

namespace SimpleObjectDbWebApi;

public static class Modules
{
    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleA(configuration);
        services.AddModuleB(configuration);

        var connectionString = configuration.GetValue<string>("ConnectionString") ?? "";
        var dbConfig = new DocumentStoreConfiguration();
        dbConfig.ConfigureDocumentStoreForModuleA();
        dbConfig.ConfigureDocumentStoreForModuleB();
        services.AddSingleton<IDocumentStore>(sp => new PostgreSqlDocumentStore(connectionString, dbConfig));

        return services;
    }

    public static void RegisterEndpoints(this IEndpointRouteBuilder app)
    {
        app.RegisterModuleAEndpoints();
        app.RegisterModuleBEndpoints();
    }
}
