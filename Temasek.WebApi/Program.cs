using Clerk.BackendAPI;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Temasek.WebApi.Clerk;
using Temasek.WebApi.Features.Facilities;
using Temasek.WebApi.Features.FormSg;
using Temasek.WebApi.Features.Rooms.Signboard;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOptions<ClerkOptions>().Bind(builder.Configuration.GetSection("Clerk"));
builder.Services.AddOptions<FormSgOptions>().Bind(builder.Configuration.GetSection("FormSg"));
builder
    .Services.AddOptions<FacilitiesOptions>()
    .Bind(builder.Configuration.GetSection("Facilities"));
builder
    .Services.AddOptions<RoomSignboardEventBusOptions>()
    .Bind(builder.Configuration.GetSection("RoomSignboardEventBus"));

builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<IFreeSql>>();

    return new FreeSql.FreeSqlBuilder()
        .UseConnectionString(
            FreeSql.DataType.MySql,
            builder.Configuration.GetConnectionString("temasek")
        )
        .UseMonitorCommand(cmd => logger.LogSqlCommand(cmd.CommandText))
        .UseAutoSyncStructure(true)
        .Build();
});

builder.Services.AddSingleton<RoomSignboardEventBus>();
builder.Services.AddScoped<RoomSignboardStore>();

builder.Services.AddScoped(sp => new ClerkBackendApi(
    bearerAuth: sp.GetRequiredService<IOptions<ClerkOptions>>().Value.SecretKey
));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173");
        }
        else
        {
            policy.WithOrigins("https://temasek3.cc");
        }

        policy.AllowAnyMethod().AllowAnyHeader().AllowCredentials();
    });
});

builder
    .Services.AddAuthentication(ClerkAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ClerkAuthenticationHandler>(
        ClerkAuthenticationHandler.SchemeName,
        null
    );
builder.Services.AddAuthorization();

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(options =>
{
    options.DocumentSettings = d =>
    {
        d.MarkNonNullablePropsAsRequired();
    };
});

var app = builder.Build();

app.UseCors();
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints();
app.UseSwaggerGen(options =>
{
    options.Path = "/openapi/{documentName}.json";
});

app.MapDefaultEndpoints();
app.MapScalarApiReference();

app.Run();

return;

public static partial class LoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "SQL: {CommandText}")]
    public static partial void LogSqlCommand(this ILogger logger, string commandText);
}
