using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Rolayther.Data;
using Rolayther.Exceptions;
using Rolayther.Helpers;
using Rolayther.Models.Entities;
using Rolayther.Services;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

//Colegamento con frontend 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});


//Configurazione di Serilog per il logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

Log.Information("Registering services...");

try
{

// Add services to the container.

//builder.Services.AddControllers();
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

//Stringa di connessione al database, impostata nel file appsettings.json
string connectionString = string.Empty;

//try catch per gestire l'eventuale assenza della stringa di connessione
try
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new Exception("Stringa di connessione non trovata");
}
catch (Exception ex)
{
    Log.Fatal(ex.Message);
    await Log.CloseAndFlushAsync();
    Environment.Exit(1);
}

//dependency injection del contesto del database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
);

//Configurazione dell'autenticazione JWT impostata nel file appsettings.json
var jwt = builder.Configuration.GetSection("Jwt");

//Aggiunta del servizio di autenticazione JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });

//Configurazione di Identity per la gestione degli utenti
builder.Services.AddIdentityCore<ApplicationUser>(option =>
{
    option.SignIn.RequireConfirmedPhoneNumber = false;
    option.SignIn.RequireConfirmedEmail = false;
    option.SignIn.RequireConfirmedAccount = false;
    option.Password.RequiredLength = 8;
    option.Password.RequireDigit = false;
    option.Password.RequireUppercase = true;
    option.Password.RequireLowercase = true;
    option.Password.RequireNonAlphanumeric = false;
    option.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager<SignInManager<ApplicationUser>>();

//Qui da aggiungere le future dipendenze dei servizi
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<MasterService>();
builder.Services.AddScoped<GenreService>();
builder.Services.AddScoped<PlatformService>();
builder.Services.AddScoped<PlayerService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<MasterGamesService>();
builder.Services.AddScoped<MasterPlatformsService>();

// Servizio email: per ora logga soltanto (vedi ConsoleEmailService), da sostituire con Brevo
builder.Services.AddScoped<IEmailService, ConsoleEmailService>();

// Storage file: implementazione locale su disco (dev). Da sostituire con una versione Supabase Storage prima del deploy.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IFileStorageService, LocalDiskFileStorageService>();
builder.Services.AddScoped<SessionResourceService>();

// Controlli di proprietà (chi può modificare/eliminare cosa): Admin sempre, altrimenti solo il proprietario
builder.Services.AddScoped<ResourceAccessService>();

// Rate limiting sugli endpoint di autenticazione: max 10 richieste al minuto per IP
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("AuthPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
// Configurazione di Swagger per la documentazione delle API e l'autenticazione JWT
builder.Services.AddSwaggerGen(option =>
{
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    option.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }

    });

});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    //Inizializzazione del database in ambiente di sviluppo
    try
    {
        await DbHelper.InitializeDatabaseAsync<ApplicationDbContext>(app);
    }
    catch (DbInizializationException ex)
    {
        Log.Fatal(ex.Message);
        await Log.CloseAndFlushAsync();
        Environment.Exit(1);

    }
    catch (Exception ex)
    {
        Log.Fatal(ex.Message);
        await Log.CloseAndFlushAsync();
        Environment.Exit(1);
    }

}

//app.UseHttpsRedirection();

app.UseStaticFiles(); // serve wwwroot/uploads (avatar, cover) — i file privati NON passano da qui

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();


//Esecuzione dell'applicazione con logging dell'avvio dell'applicazione stesso
Log.Information("Running application...");

app.Run();

}
catch (Exception ex)
{
    // Rete di sicurezza: stampa tipo, messaggio e stack trace completo di QUALSIASI eccezione
    // non gestita durante l'avvio o l'esecuzione, invece di lasciar morire il processo in silenzio.
    Log.Fatal(ex, "Errore fatale non gestito durante l'avvio o l'esecuzione dell'applicazione");
    await Log.CloseAndFlushAsync();
    Environment.Exit(1);
}