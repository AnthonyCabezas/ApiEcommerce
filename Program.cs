using System.Text;
using ApiEcommerce.Constants;
using ApiEcommerce.Data;
using ApiEcommerce.Models;
using ApiEcommerce.Reposiroty;
using ApiEcommerce.Reposiroty.IRepository;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var dbConnectionstring = builder.Configuration.GetConnectionString("ConexionSql");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(dbConnectionstring)
    .UseSeeding((context, _) =>
    {
      var appContext = (ApplicationDbContext)context;
      DataSeeder.SeedData(appContext);
    }
    ));
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Mapster
MapsterConfig.RegisterMappings();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

var secretKey = builder.Configuration.GetValue<string>("ApiSettings:SecretKey") ?? throw new ArgumentNullException("ApiSettings:SecretKey is null");
builder.Services.AddResponseCaching(options =>
{
  options.MaximumBodySize = 1024 * 1024;
  options.UseCaseSensitivePaths = true;
});
builder.Services.AddAuthentication(options =>
{
  options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer("Bearer", options =>
{
  options.RequireHttpsMetadata = false;
  options.SaveToken = true;
  options.TokenValidationParameters = new TokenValidationParameters()
  {
    ValidateIssuer = false,
    ValidateAudience = false,
    //ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
  };
});
builder.Services.AddControllers(option =>
    {
      option.CacheProfiles.Add(CacheProfiles.Default10, CacheProfiles.Profile10);
      option.CacheProfiles.Add(CacheProfiles.Default30, CacheProfiles.Profile30);
      // option.CacheProfiles.Add("Default10",
      // new Microsoft.AspNetCore.Mvc.CacheProfile() { Duration = 10 });
      // option.CacheProfiles.Add("Default30",
      // new Microsoft.AspNetCore.Mvc.CacheProfile() { Duration = 30 });
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    options =>
  {
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
      Description = "Nuestra API utiliza la Autenticación JWT usando el esquema Bearer. \n\r\n\r" +
                    "Ingresa la palabra a continuación el token generado en login.\n\r\n\r" +
                    "Ejemplo: \"12345abcdef\"",
      Name = "Authorization",
      In = ParameterLocation.Header,
      Type = SecuritySchemeType.Http,
      Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
      {
        new OpenApiSecurityScheme
        {
          Reference = new OpenApiReference
          {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
          },
          Scheme = "oauth2",
          Name = "Bearer",
          In = ParameterLocation.Header
        },
        new List<string>()
      }
    });
    options.SwaggerDoc("v1", new OpenApiInfo
    {
      Version = "v1",
      Title = "API Ecommerce",
      Description = "API para gestionar productos y usuarios",
      TermsOfService = new Uri("http://example.com/terms"),
      Contact = new OpenApiContact
      {
        Name = "Api Ecommerce",
        Url = new Uri("https://midominio.com/soporte"),//TODO: Cambiar por github
        Email = "anthonycabezasramirez@gmail.com"
      },
      License = new OpenApiLicense
      {
        Name = "Licencia de uso",
        Url = new Uri("http://example.com/license")
      }
    });
    options.SwaggerDoc("v2", new OpenApiInfo
    {
      Version = "v2",
      Title = "API Ecommerce V2",
      Description = "API para gestionar productos y usuarios",
      TermsOfService = new Uri("http://example.com/terms"),
      Contact = new OpenApiContact
      {
        Name = "Soporte Ecommerce",
        Url = new Uri("https://midominio.com/soporte"),//TODO: Cambiar por github
        Email = "anthonycabezasramirez@gmail.com"
      },
      License = new OpenApiLicense
      {
        Name = "Licencia de uso",
        Url = new Uri("http://example.com/license")
      }
    });
  }
);
var apiVersioningBuilder = builder.Services.AddApiVersioning(options =>
{
  options.AssumeDefaultVersionWhenUnspecified = true;
  options.DefaultApiVersion = new ApiVersion(1, 0);
  options.ReportApiVersions = true;
  // options.ApiVersionReader = ApiVersionReader.Combine(
  //   new QueryStringApiVersionReader("api-version"));
});

apiVersioningBuilder.AddApiExplorer(options =>
{
  options.GroupNameFormat = "'v'VVV";
  options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddCors(options =>
{
  options.AddPolicy(PoliceNames.AllowSpecificOrigin, builder =>
  {
    builder.WithOrigins("*")
             .AllowAnyHeader()
             .AllowAnyMethod();
  });
});

var app = builder.Build();
builder.Services.AddResponseCaching();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI(options =>
  {
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Ecommerce v1");
    options.SwaggerEndpoint("/swagger/v2/swagger.json", "API Ecommerce v2");
  });
}
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseCors(PoliceNames.AllowSpecificOrigin);
app.UseResponseCaching();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
