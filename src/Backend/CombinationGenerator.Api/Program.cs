using System.Text.Json.Serialization;
using CombinationGenerator.Application.Generators;
using CombinationGenerator.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. API: Controllers, JSON ושגיאות
// ============================================================
builder.Services
    .AddControllers()
    // enum-ים נשלחים ומתקבלים כטקסט ("Next", "All") ולא כמספרים (0, 1)
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// שגיאות לא צפויות יוחזרו בפורמט ProblemDetails, כמו כל שאר השגיאות
builder.Services.AddProblemDetails();

// ============================================================
// 2. Application: ה-Service וההגדרות שלו
// ============================================================
var generatorOptions = builder.Configuration
    .GetSection(GeneratorOptions.SectionName)
    .Get<GeneratorOptions>() ?? new GeneratorOptions();

builder.Services.AddSingleton(generatorOptions);

// Singleton: ה-Service לא שומר שום מצב משלו (הכל במגירה), אז אחד מספיק לכל השרת
builder.Services.AddSingleton<GeneratorService>();

// ============================================================
// 3. Infrastructure: המגירה בזיכרון, ההגדרות שלה והמנקה ברקע
// ============================================================
builder.Services.AddInMemorySessionStore(builder.Configuration);

// ============================================================
// 4. CORS: מאפשרים רק לכתובת של הלקוח (Angular) לפנות לשרת
// ============================================================
const string FrontendCorsPolicy = "Frontend";

string[] allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// ============================================================
// 5. Swagger: דף שמתאר את כל הכתובות ומאפשר לנסות אותן מהדפדפן
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Combination Generator API", Version = "v1" }));

// ============================================================
// בניית השרת וסדר הטיפול בבקשה
// ============================================================
var app = builder.Build();

app.UseExceptionHandler();      // שגיאה לא צפויה → 500 בפורמט ProblemDetails
app.UseStatusCodePages();       // 404 על כתובת שלא קיימת → גם בפורמט ProblemDetails

// Swagger רק בפיתוח: בשרת אמיתי לא חושפים את מפת הכתובות
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();           // /swagger/v1/swagger.json — התיאור של ה-API
    app.UseSwaggerUI();         // /swagger — הדף שאפשר ללחוץ בו
}

app.UseCors(FrontendCorsPolicy);
app.MapControllers();

app.Run();

/// <summary>מאפשר לבדיקות ה-API להפעיל את השרת בתוך הבדיקה.</summary>
public partial class Program
{
}
