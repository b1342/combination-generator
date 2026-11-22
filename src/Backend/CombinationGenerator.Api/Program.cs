using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using CombinationGenerator.Api.Errors;
using CombinationGenerator.Application.Generators;
using CombinationGenerator.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. API: Controllers, JSON ושגיאות
// ============================================================
builder.Services
    .AddControllers()
    // enum-ים נשלחים ומתקבלים כטקסט ("Next", "All"). מספרים (0, 1, 99) נדחים ב-400:
    // בלי allowIntegerValues: false, הערך 99 היה עובר והופך לשגיאה פנימית (500)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));

        // מספר חייב להגיע כמספר: "n": "3" (טקסט) נדחה. ברירת המחדל של ASP.NET הייתה מקבלת אותו בשקט
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    })
    // קלט שלא נקרא: אותו פורמט ואותו שדה "code" כמו כל שאר השגיאות, ובלי לחשוף שמות של מחלקות פנימיות.
    // מזהה בכתובת שאינו GUID מקבל קוד משלו; כל השאר (JSON שבור, סוג לא נכון) הם INVALID_REQUEST.
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
        ApiErrors.ToResult(context.ModelState.TryGetValue("id", out var id) && id.Errors.Count > 0
            ? ApiErrors.InvalidSessionId
            : ApiErrors.InvalidRequest));

// שגיאות לא צפויות יוחזרו בפורמט ProblemDetails, כמו כל שאר השגיאות
builder.Services.AddProblemDetails();

// ============================================================
// 2. Application: ה-Service וההגדרות שלו
// ============================================================
// אותו דפוס כמו SessionStoreOptions: קושרים מההגדרות, ובודקים בעליית השרת (ולא בבקשה הראשונה)
builder.Services.AddOptions<GeneratorOptions>()
    .Bind(builder.Configuration.GetSection(GeneratorOptions.SectionName))
    .Validate(options => options.IsValid, $"{GeneratorOptions.SectionName}:MaxPageSize must be at least 1.")
    .ValidateOnStart();

// GeneratorService מקבל את GeneratorOptions עצמו, בלי תלות ב-IOptions
builder.Services.AddSingleton(provider => provider.GetRequiredService<IOptions<GeneratorOptions>>().Value);

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
