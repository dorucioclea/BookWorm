﻿using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BookWorm.Chassis.Endpoints;

public static class RegisterEndpointExtensions
{
    public static void AddEndpoints(this IServiceCollection services, Type type)
    {
        services.Scan(scan =>
            scan.FromAssembliesOf(type)
                .AddClasses(classes =>
                    classes
                        .AssignableTo<IEndpoint>()
                        .Where(typeInfo => typeInfo is { IsAbstract: false, IsInterface: false })
                )
                .AsImplementedInterfaces()
                .WithTransientLifetime()
        );
    }

    public static void MapEndpoints(
        this WebApplication app,
        ApiVersionSet apiVersionSet,
        string? resourceName = null
    )
    {
        using var scope = app.Services.CreateScope();

        var endpoints = scope.ServiceProvider.GetRequiredService<IEnumerable<IEndpoint>>();

        var builder = app.MapGroup(
                resourceName is null or { Length: 0 }
                    ? "/api/v{version:apiVersion}"
                    : $"/api/v{{version:apiVersion}}/{resourceName}"
            )
            .WithApiVersionSet(apiVersionSet);

        if (app.Environment.IsDevelopment())
        {
            builder.DisableAntiforgery();
        }

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(builder);
        }
    }
}
