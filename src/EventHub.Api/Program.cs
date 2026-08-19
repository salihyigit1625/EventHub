using EventHub.Api.Authorization;
using EventHub.Api.Extensions;
using EventHub.Api.Filters;
using EventHub.Api.Middleware;
using EventHub.Application;
using EventHub.Repository;
using EventHub.Repository.Context;
using EventHub.Repository.Seed;
using EventHub.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
builder.Services.AddScoped<ValidationFilter>();
builder.Services.AddEventHubSwagger();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddApplication();
builder.Services.AddRepository(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPermissionPolicies();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseEventHubSwagger();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
