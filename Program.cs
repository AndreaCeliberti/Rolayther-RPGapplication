using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Rolayther.Data;
using Rolayther.Exceptions;
using Rolayther.Helpers;
using Rolayther.Models.Entities;
using Rolayther.Services;
using Rolayther.Services.Mail;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

// PostgreSQL (driver Npgsql): salva le date e le ore così come arrivano, senza convertirle in UTC, come faceva SQL Server
// (l'app è nata su SQL Server). Senza questa riga Npgsql rifiuterebbe le date senza fuso orario, come quella
// del modulo di creazione sessione. Va impostata prima di qualsiasi uso del database (anche per Add-Migration).
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Siti autorizzati a chiamare l'API (CORS): uno o più indirizzi separati da virgola, senza "/" finale.
// Online va impostato Cors__AllowedOrigins con l'indirizzo del sito pubblicato (es. https://rolayther.netlify.app);
// se manca si usa il sito di sviluppo in locale.
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(origin => origin.TrimEnd('/'))
    .Where(origin => origin.Length > 0)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (allowedOrigins.Length == 0)
    allowedOrigins = new[] { "http://localhost:5173" };

//Colegamento con frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins(allowedOrigins)
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

// Un indirizzo scritto male (senza https://, con un percorso) non verrebbe mai riconosciuto dal browser: meglio dirlo subito
foreach (var origin in allowedOrigins)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
        || (originUri.Scheme != Uri.UriSchemeHttp && originUri.Scheme != Uri.UriSchemeHttps)
        || originUri.AbsolutePath.Length > 1)
    {
        Log.Warning("CORS: \"{Origin}\" non è un indirizzo valido (serve per esempio https://rolayther.netlify.app, senza percorso): il browser non lo riconoscerà", origin);
    }
}

Log.Information("CORS: il sito può chiamare l'API da {Origins}", string.Join(", ", allowedOrigins));

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

//Stringa di connessione al database PostgreSQL: in locale nei User Secrets, online nella variabile d'ambiente
//ConnectionStrings__DefaultConnection (mai nel repository: contiene la password)
string connectionString = string.Empty;

//try catch per gestire l'eventuale assenza o il formato errato della stringa di connessione
try
{
    var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(configuredConnection))
        throw new Exception("Stringa di connessione al database non trovata: in locale va nei User Secrets (ConnectionStrings:DefaultConnection), online nella variabile d'ambiente ConnectionStrings__DefaultConnection");

    // Accetta sia "Host=...;Port=...;Database=...;Username=...;Password=..." sia l'indirizzo postgresql://... di Supabase
    connectionString = ConnectionStringHelper.Normalize(configuredConnection);
}
catch (Exception ex)
{
    Log.Fatal(ex.Message);
    await Log.CloseAndFlushAsync();
    Environment.Exit(1);
}

//dependency injection del contesto del database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
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

// ===== Email (conferma registrazione, approvazione Master, iscrizioni e creazione sessioni) =====
// Impostazioni nella sezione "Email" di appsettings (vedi docs/EMAIL.md). Controllate all'avvio:
// con Provider = "Brevo" senza chiave API l'applicazione non parte e dice cosa manca.
builder.Services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
builder.Services.AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName))
    .ValidateOnStart();

// Trasporto: "Brevo" = invio reale con l'API web di Brevo (su Render gratuito le porte SMTP sono bloccate);
// "Console" (predefinito) = non spedisce nulla, salva ogni email come file HTML in App_Data/emails.
var emailOptions = builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
if (emailOptions.IsBrevo)
    builder.Services.AddHttpClient<IEmailService, BrevoEmailService>(client => client.Timeout = TimeSpan.FromSeconds(15));
else
    builder.Services.AddSingleton<IEmailService, ConsoleEmailService>();

builder.Services.AddSingleton<EmailConfirmationTokenService>(); // link di conferma firmati (nessun dato nel database)
builder.Services.AddSingleton<ResendThrottle>();                // antispam sul "reinvia conferma"
builder.Services.AddScoped<EmailNotificationService>();         // sceglie il modello e spedisce

// Le email delle sessioni (e il reinvio della conferma) partono in background, dopo la risposta all'utente
builder.Services.AddSingleton<MailJobQueue>();
builder.Services.AddSingleton<IMailJobQueue>(sp => sp.GetRequiredService<MailJobQueue>());
builder.Services.AddScoped<IMailJobHandler, MailJobProcessor>();
builder.Services.AddHostedService<MailJobWorker>();

// ===== Archivio dei file (avatar, copertine, materiale delle sessioni) =====
// Sezione "Storage" di appsettings (vedi docs/DEPLOY.md). "Local" (predefinito) = file su disco, per lo sviluppo.
// "Supabase" = Supabase Storage, da usare online: il disco di Render gratuito si svuota a ogni riavvio.
// Controllata all'avvio: con Provider = "Supabase" senza indirizzo o chiave l'applicazione non parte e dice cosa manca.
builder.Services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();
builder.Services.AddOptions<StorageOptions>()
    .Bind(builder.Configuration.GetSection(StorageOptions.SectionName))
    .ValidateOnStart();

var storageOptions = builder.Configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();

builder.Services.AddHttpContextAccessor();
if (storageOptions.IsSupabase)
{
    builder.Services.AddHttpClient<IFileStorageService, SupabaseFileStorageService>((sp, client) =>
        SupabaseFileStorageService.ConfigureClient(client, sp.GetRequiredService<IOptions<StorageOptions>>().Value.Supabase));

    // Controllo informativo all'avvio: i due bucket esistono e hanno la visibilità giusta? (solo log, non blocca nulla)
    builder.Services.AddHttpClient(SupabaseBucketCheck.HttpClientName, (sp, client) =>
        SupabaseFileStorageService.ConfigureClient(client, sp.GetRequiredService<IOptions<StorageOptions>>().Value.Supabase));
    builder.Services.AddHostedService<SupabaseBucketCheck>();
}
else
{
    builder.Services.AddScoped<IFileStorageService, LocalDiskFileStorageService>();
}
builder.Services.AddScoped<SessionResourceService>();

// Controlli di proprietà (chi può modificare/eliminare cosa): Admin sempre, altrimenti solo il proprietario
builder.Services.AddScoped<ResourceAccessService>();

// Dietro un proxy (Render) l'indirizzo che il server vede è quello del proxy, uguale per tutti: senza correzione il limite
// di tentativi qui sotto sarebbe UNICO per l'intero sito. Se Hosting:ClientIpHeader è valorizzato, l'IP di chi si collega
// si legge da quell'intestazione. Su Render si usa CF-Connecting-IP: la scrive Cloudflare, che sta davanti a Render, e
// Cloudflare respinge (403) le richieste in cui è il visitatore a scriverla. NON si usa X-Forwarded-For, dove il primo
// valore lo può inventare chi chiama. Va lasciato vuoto dove non c'è un proxy fidato (in locale): lì chiunque potrebbe
// scriverla a mano e aggirare il limite.
var clientIpHeader = builder.Configuration["Hosting:ClientIpHeader"]?.Trim();
if (!string.IsNullOrEmpty(clientIpHeader))
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedForHeaderName = clientIpHeader;
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
        options.ForwardLimit = 1;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

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

    // Reinvio dell'email di conferma: limite più stretto (l'endpoint è pubblico e fa partire una email)
    options.AddPolicy("ResendPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Controllo di salute con database (/health/db): pubblico, perché lo chiama il servizio di ping esterno.
    // Il limite basta e avanza per un ping ogni pochi minuti e impedisce di tenere occupate le connessioni al database.
    options.AddPolicy("HealthPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
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

// Il token di accesso (JWT) non funziona senza chiave, emittente e destinatario: meglio fermarsi subito con un messaggio
// chiaro che far partire il sito e poi rispondere con errori a chiunque provi ad accedere.
// (Il controllo sta dopo Build() di proposito: gli strumenti di Entity Framework, come Add-Migration, si fermano a Build.)
var jwtSettings = app.Configuration.GetSection("Jwt");
var jwtProblems = new List<string>();
if ((jwtSettings["Key"] ?? string.Empty).Trim().Length < 16)
    jwtProblems.Add("Jwt:Key mancante o più corta di 16 caratteri (consigliati 32 o più, casuali)");
if (string.IsNullOrWhiteSpace(jwtSettings["Issuer"]))
    jwtProblems.Add("Jwt:Issuer mancante");
if (string.IsNullOrWhiteSpace(jwtSettings["Audience"]))
    jwtProblems.Add("Jwt:Audience mancante");
if (!double.TryParse(jwtSettings["AccessTokenExpirationMinutes"], out var accessTokenMinutes) || accessTokenMinutes <= 0)
    jwtProblems.Add("Jwt:AccessTokenExpirationMinutes mancante o non valido (minuti, per esempio 300)");

if (jwtProblems.Count > 0)
{
    Log.Fatal("Impostazioni del token di accesso non valide: {Problems}. Online vanno nelle variabili d'ambiente (Jwt__Key, Jwt__Issuer, Jwt__Audience), in locale nei User Secrets", string.Join("; ", jwtProblems));
    await Log.CloseAndFlushAsync();
    Environment.Exit(1);
}

Log.Information("Ambiente: {Environment}. Database PostgreSQL: {Database}", app.Environment.EnvironmentName, ConnectionStringHelper.Describe(connectionString));

if (storageOptions.IsSupabase)
    Log.Information("Archivio file: Supabase Storage ({Url})", storageOptions.Supabase.BaseUrl);
else if (app.Environment.IsDevelopment())
    Log.Information("Archivio file: disco locale (wwwroot/uploads e App_Data/private-uploads)");
else
    Log.Warning("Archivio file: \"Local\" in un ambiente non di sviluppo. I file caricati sparirebbero a ogni riavvio del server: imposta Storage:Provider = Supabase (vedi docs/DEPLOY.md)");

if (emailOptions.IsBrevo)
    Log.Information("Email: invio reale con Brevo (mittente {From})", emailOptions.FromEmail);
else if (app.Environment.IsDevelopment())
    Log.Information("Email: modalità sviluppo, nessun invio reale. Le email vengono salvate in {Folder}", emailOptions.PreviewFolder);
else
    Log.Warning("Email: provider \"{Provider}\" in un ambiente non di sviluppo. Le email NON vengono spedite: imposta Email:Provider = Brevo (vedi docs/EMAIL.md)", emailOptions.Provider);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Inizializzazione del database: migrazioni, ruoli e utente Admin. In sviluppo parte sempre. Online parte solo con
// Database__InitializeOnStartup = true (Render gratuito non ha un terminale dove lanciare le migrazioni a mano:
// così ogni nuova versione aggiorna da sola le tabelle all'avvio).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:InitializeOnStartup"))
{
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
        // Con l'eccezione intera si vede anche la causa interna (password errata, host irraggiungibile, tabella mancante...)
        Log.Fatal(ex, "Inizializzazione del database non riuscita: {Message}", ex.Message);
        await Log.CloseAndFlushAsync();
        Environment.Exit(1);
    }
}
else
{
    Log.Information("Database: migrazioni e dati iniziali non eseguiti all'avvio (Database:InitializeOnStartup non attivo)");
}

//app.UseHttpsRedirection();

if (!string.IsNullOrEmpty(clientIpHeader))
    app.UseForwardedHeaders(); // prima di tutto il resto, così il limite di tentativi vede l'IP vero (vedi Hosting:ClientIpHeader)

app.UseStaticFiles(); // serve wwwroot/uploads (avatar, cover) — i file privati NON passano da qui

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// Controlli di salute. /health risponde subito, senza toccare il database (lo usa Render per sapere che il server è vivo).
// /health/db interroga anche il database: è l'indirizzo da far chiamare al servizio di ping (per esempio UptimeRobot)
// ogni 5 minuti, così il server di Render gratuito non si addormenta e il progetto Supabase non va in pausa per inattività.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/health/db", async (ApplicationDbContext db) =>
{
    try
    {
        await db.Database.ExecuteSqlRawAsync("SELECT 1");
        return Results.Ok(new { status = "ok", database = "ok" });
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Controllo di salute: il database non risponde");
        return Results.Json(new { status = "error", database = "down" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).RequireRateLimiting("HealthPolicy");


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