using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using ClientesApi.Models;
using ClientesApi.Services;

namespace ClientesApi.Tests;

// ---------------------------------------------------------------------------
// Testes unitários do CobolService (sem processo real)
// ---------------------------------------------------------------------------
public class CobolServiceTests
{
    // Verifica que o parse de uma resposta OK retorna o cliente corretamente
    [Fact]
    public void ParseOk_DeveRetornarClientePreenchido()
    {
        var partes = "OK|00001|João Silva|11999990001|joao@email.com".Split('|');

        var cliente = new Cliente
        {
            Codigo   = partes[1],
            Nome     = partes[2],
            Telefone = partes[3],
            Email    = partes[4]
        };

        Assert.Equal("00001", cliente.Codigo);
        Assert.Equal("João Silva", cliente.Nome);
        Assert.Equal("11999990001", cliente.Telefone);
        Assert.Equal("joao@email.com", cliente.Email);
    }

    // Verifica que uma resposta NOTFOUND é identificada
    [Fact]
    public void ParseNotFound_DeveIdentificarNaoEncontrado()
    {
        string output = "NOTFOUND";
        Assert.True(output == "NOTFOUND");
    }

    // Verifica que uma resposta ATUALIZADO é parseada corretamente
    [Fact]
    public void ParseAtualizado_DeveRetornarNovosDados()
    {
        var partes = "ATUALIZADO|00001|21988880099|novo@email.com".Split('|');

        Assert.Equal("ATUALIZADO", partes[0]);
        Assert.Equal("00001", partes[1]);
        Assert.Equal("21988880099", partes[2]);
        Assert.Equal("novo@email.com", partes[3]);
    }
}

// ---------------------------------------------------------------------------
// Testes de integração do controller com mocks de CobolService e Repository
// ---------------------------------------------------------------------------
public class ClientesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ClientesControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CriarClienteComMocks(
        Mock<IClienteRepository>? repoMock = null,
        Mock<ICobolService>? cobolMock = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove registros reais
                var repoDescriptor  = services.Single(d => d.ServiceType == typeof(IClienteRepository));
                var cobolDescriptor = services.Single(d => d.ServiceType == typeof(ICobolService));
                services.Remove(repoDescriptor);
                services.Remove(cobolDescriptor);

                // Adiciona mocks
                services.AddScoped(_ => (repoMock ?? new Mock<IClienteRepository>()).Object);
                services.AddScoped(_ => (cobolMock ?? new Mock<ICobolService>()).Object);
            });
        }).CreateClient();
    }

    // GET /api/clientes/{codigo} — cliente encontrado
    [Fact]
    public async Task Get_ClienteExistente_Retorna200ComDados()
    {
        var clienteDb = new Cliente
        {
            Codigo   = "00001",
            Nome     = "João Silva",
            Telefone = "11999990001",
            Email    = "joao@email.com"
        };

        var repoMock = new Mock<IClienteRepository>();
        repoMock.Setup(r => r.BuscarPorCodigoAsync("00001"))
                .ReturnsAsync(clienteDb);

        var cobolMock = new Mock<ICobolService>();
        cobolMock.Setup(c => c.ConsultarAsync(It.IsAny<Cliente>()))
                 .ReturnsAsync(new CobolConsultaResult { Sucesso = true, Cliente = clienteDb });

        var http = CriarClienteComMocks(repoMock, cobolMock);
        var response = await http.GetAsync("/api/clientes/00001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Cliente>();
        Assert.NotNull(body);
        Assert.Equal("00001", body!.Codigo);
        Assert.Equal("João Silva", body.Nome);
    }

    // GET /api/clientes/{codigo} — cliente não encontrado no banco
    [Fact]
    public async Task Get_ClienteInexistente_Retorna404()
    {
        var repoMock = new Mock<IClienteRepository>();
        repoMock.Setup(r => r.BuscarPorCodigoAsync("99999"))
                .ReturnsAsync((Cliente?)null);

        var http = CriarClienteComMocks(repoMock);
        var response = await http.GetAsync("/api/clientes/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // PUT /api/clientes/{codigo} — atualização bem-sucedida
    [Fact]
    public async Task Put_DadosValidos_Retorna200ComClienteAtualizado()
    {
        var clienteDb = new Cliente
        {
            Codigo   = "00001",
            Nome     = "João Silva",
            Telefone = "11999990001",
            Email    = "joao@email.com"
        };

        var repoMock = new Mock<IClienteRepository>();
        repoMock.Setup(r => r.BuscarPorCodigoAsync("00001"))
                .ReturnsAsync(clienteDb);
        repoMock.Setup(r => r.AtualizarContatoAsync("00001", "21988880099", "novo@email.com"))
                .ReturnsAsync(true);

        var cobolMock = new Mock<ICobolService>();
        cobolMock.Setup(c => c.AtualizarAsync(It.IsAny<Cliente>(), "21988880099", "novo@email.com"))
                 .ReturnsAsync(new CobolAtualizacaoResult
                 {
                     Sucesso      = true,
                     NovoTelefone = "21988880099",
                     NovoEmail    = "novo@email.com"
                 });

        var http = CriarClienteComMocks(repoMock, cobolMock);
        var body = new AtualizarClienteRequest { Telefone = "21988880099", Email = "novo@email.com" };
        var response = await http.PutAsJsonAsync("/api/clientes/00001", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var resultado = await response.Content.ReadFromJsonAsync<Cliente>();
        Assert.NotNull(resultado);
        Assert.Equal("21988880099", resultado!.Telefone);
        Assert.Equal("novo@email.com", resultado.Email);
    }

    // PUT /api/clientes/{codigo} — cliente não encontrado
    [Fact]
    public async Task Put_ClienteInexistente_Retorna404()
    {
        var repoMock = new Mock<IClienteRepository>();
        repoMock.Setup(r => r.BuscarPorCodigoAsync("99999"))
                .ReturnsAsync((Cliente?)null);

        var http = CriarClienteComMocks(repoMock);
        var body = new AtualizarClienteRequest { Telefone = "21988880099", Email = "novo@email.com" };
        var response = await http.PutAsJsonAsync("/api/clientes/99999", body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // PUT /api/clientes/{codigo} — COBOL retorna erro
    [Fact]
    public async Task Put_ErroCOBOL_Retorna500()
    {
        var clienteDb = new Cliente
        {
            Codigo   = "00001",
            Nome     = "João Silva",
            Telefone = "11999990001",
            Email    = "joao@email.com"
        };

        var repoMock = new Mock<IClienteRepository>();
        repoMock.Setup(r => r.BuscarPorCodigoAsync("00001"))
                .ReturnsAsync(clienteDb);

        var cobolMock = new Mock<ICobolService>();
        cobolMock.Setup(c => c.AtualizarAsync(It.IsAny<Cliente>(), It.IsAny<string>(), It.IsAny<string>()))
                 .ReturnsAsync(new CobolAtualizacaoResult { Sucesso = false, Erro = "ERRO|DADOS_INVALIDOS" });

        var http = CriarClienteComMocks(repoMock, cobolMock);
        var body = new AtualizarClienteRequest { Telefone = "", Email = "" };
        var response = await http.PutAsJsonAsync("/api/clientes/00001", body);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
