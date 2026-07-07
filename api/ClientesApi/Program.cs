using ClientesApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "Clientes API",
        Version     = "v1",
        Description = "API de modernização do cadastro de clientes — Cooperativa Financeira Alfa"
    });
});

// Injeção de dependência
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<ICobolService, CobolService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clientes API v1");
    c.RoutePrefix = string.Empty; // Swagger na raiz: http://localhost:5000/
});

app.UseAuthorization();
app.MapControllers();

app.Run();

// Torna a classe acessível para testes de integração (xUnit WebApplicationFactory)
public partial class Program { }
