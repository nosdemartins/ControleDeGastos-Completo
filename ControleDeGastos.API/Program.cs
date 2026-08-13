using ControleDeGastos.API.Data;
using ControleDeGastos.API.Security;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Controle de Gastos API",
        Version = "v1",
        Description = "API para gerenciamento de receitas e despesas pessoais."
    });
});

// Banco de dados PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não configurada em appsettings.json.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// CORS: em produção, restringe às origens configuradas em appsettings.json (Cors:AllowedOrigins)
var origensPermitidas = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        if (origensPermitidas.Length > 0)
        {
            policy.WithOrigins(origensPermitidas).AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            policy.AllowAnyMethod().AllowAnyHeader().SetIsOriginAllowed(_ => true);
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Controle de Gastos API v1");
    });
}
else
{
    // Em produção, evita vazar stack traces/detalhes internos em respostas de erro
    app.UseExceptionHandler(tratador =>
    {
        tratador.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { mensagem = "Ocorreu um erro interno. Tente novamente mais tarde." });
        });
    });
}

// Middleware de segurança: deve rodar cedo no pipeline, antes do roteamento/controllers
app.UseAntiInvasao();

app.UseCors("PermitirFrontend");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Aplica migrations pendentes e popula dados de exemplo (apenas se o banco estiver vazio)
using (var scope = app.Services.CreateScope())
{
    var contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    contexto.Database.Migrate();
    SeedData.Popular(contexto);
}

app.Run();
