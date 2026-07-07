using Microsoft.AspNetCore.Mvc;
using ClientesApi.Models;
using ClientesApi.Services;

namespace ClientesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IClienteRepository _repository;
    private readonly ICobolService _cobol;
    private readonly ILogger<ClientesController> _logger;

    public ClientesController(
        IClienteRepository repository,
        ICobolService cobol,
        ILogger<ClientesController> logger)
    {
        _repository = repository;
        _cobol      = cobol;
        _logger     = logger;
    }

    /// <summary>Consulta um cliente pelo código.</summary>
    [HttpGet("{codigo}")]
    [ProducesResponseType(typeof(Cliente), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(string codigo)
    {
        // 1. Busca no banco via .NET
        var cliente = await _repository.BuscarPorCodigoAsync(codigo);
        if (cliente is null)
            return NotFound(new { mensagem = $"Cliente '{codigo}' não encontrado." });

        // 2. Envia ao COBOL para processamento/validação
        var resultado = await _cobol.ConsultarAsync(cliente);

        if (resultado.NaoEncontrado)
            return NotFound(new { mensagem = $"Cliente '{codigo}' não encontrado (COBOL)." });

        if (!resultado.Sucesso)
        {
            _logger.LogError("Erro COBOL na consulta: {Erro}", resultado.Erro);
            return StatusCode(500, new { mensagem = "Erro no processamento COBOL.", detalhe = resultado.Erro });
        }

        return Ok(resultado.Cliente);
    }

    /// <summary>Atualiza telefone e/ou e-mail de um cliente.</summary>
    [HttpPut("{codigo}")]
    [ProducesResponseType(typeof(Cliente), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Put(string codigo, [FromBody] AtualizarClienteRequest request)
    {
        // 1. Verifica se cliente existe
        var cliente = await _repository.BuscarPorCodigoAsync(codigo);
        if (cliente is null)
            return NotFound(new { mensagem = $"Cliente '{codigo}' não encontrado." });

        // 2. COBOL valida e processa a atualização
        var resultado = await _cobol.AtualizarAsync(cliente, request.Telefone, request.Email);

        if (resultado.NaoEncontrado)
            return NotFound(new { mensagem = $"Cliente '{codigo}' não encontrado (COBOL)." });

        if (!resultado.Sucesso)
        {
            _logger.LogError("Erro COBOL na atualização: {Erro}", resultado.Erro);
            return StatusCode(500, new { mensagem = "Erro no processamento COBOL.", detalhe = resultado.Erro });
        }

        // 3. .NET persiste no banco após confirmação do COBOL
        bool persistido = await _repository.AtualizarContatoAsync(codigo, resultado.NovoTelefone, resultado.NovoEmail);
        if (!persistido)
            return StatusCode(500, new { mensagem = "Falha ao persistir alterações no banco de dados." });

        return Ok(new Cliente
        {
            Codigo   = cliente.Codigo,
            Nome     = cliente.Nome,
            Telefone = resultado.NovoTelefone,
            Email    = resultado.NovoEmail
        });
    }
}
