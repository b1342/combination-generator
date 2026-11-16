// שלב 1: שלד בלבד. השרת עולה ומחזיר תשובה אחת, כדי לוודא שהכל מתקמפל ורץ.
// ה-Controllers, ה-DI וה-CORS יתווספו בשלב 8.
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "Combination Generator API is running");

app.Run();
