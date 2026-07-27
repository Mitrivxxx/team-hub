using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using team_hub_bff.Graph;
using team_hub_bff.Graph.DataLoaders;
using team_hub_bff.Services;

namespace team_hub_bff.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBffServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<GrpcOptions>()
            .Bind(configuration.GetSection(GrpcOptions.SectionName))
            .ValidateOnStart();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) =>
            {
                var jwtOptions = jwtOptionsAccessor.Value;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    NameClaimType = JwtRegisteredClaimNames.Sub
                };
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IAuthUserProfileClient, AuthUserProfileClient>();
        services.AddSingleton<IOrganizationMemberClient, OrganizationMemberClient>();

        services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddQueryType<Query>()
            .AddTypeExtension<OrganizationMemberExtensions>()
            .AddDataLoader<UserByIdDataLoader>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true);

        services.AddHealthChecks();
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? ["http://localhost:4200", "https://localhost:4200"];
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
