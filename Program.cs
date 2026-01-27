using FoodHub.Application;
using FoodHub.Application.Interfaces;
using FoodHub.Infrastructure;
using FoodHub.Infrastructure.Persistence;
using FoodHub.Presentation.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(opt =>
{
    opt.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});


// Swagger Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

//Setting for CORS
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowReact",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

//Setting for JWT Authentication
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],

            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["SecretKey"]!))
        };
    });





var app = builder.Build();

// Auto-apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    
    db.Database.Migrate();
    await DbSeeder.SeedAsync(db, hasher);
}

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FoodHub API v1");
        c.RoutePrefix = "swagger"; // Access at /swagger
    });
}

app.UseCors("AllowReact");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
