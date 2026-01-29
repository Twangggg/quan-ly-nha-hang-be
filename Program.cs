using FoodHub.Application;
using FoodHub.Infrastructure;
using FoodHub.Infrastructure.Persistence;
using FoodHub.Presentation.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(opt =>
{
    opt.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FoodHub API", Version = "v1" });
    
    // Config JWT in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Register Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

//Setting for CORS
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowReact",
        policy => policy
            .WithOrigins("http://localhost:3000") // Explicit Origin required for Credentials
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); // Allow Cookies
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

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Use Swashbuckle Swagger
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Auto Migrate & Seed Data
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            var initializer = services.GetRequiredService<DbInitializer>();
            
            context.Database.Migrate();
            initializer.Initialize();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        }
    }
}

app.UseCors("AllowReact");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
