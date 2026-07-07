using System.Diagnostics;
using System.Text;
using ClientesApi.Models;

namespace ClientesApi.Services;

public interface ICobolService
{
    Task<CobolConsultaResult> ConsultarAsync(Cliente cliente);
    Task<CobolAtualizacaoResult> AtualizarAsync(Cliente clienteAtual, string novoTelefone, string novoEmail);
}

public class CobolService : ICobolService
{
    private readonly string _cobolExePath;
    private readonly ILogger<CobolService> _logger;

    public CobolService(IConfiguration configuration, ILogger<CobolService> logger)
    {
        _cobolExePath = configuration["Cobol:ExePath"]
            ?? throw new InvalidOperationException("Cobol:ExePath not configured.");
        _logger = logger;
    }

    // Envia dados ao COBOL para validação/processamento e retorna a resposta bruta.
    public async Task<string> ExecutarAsync(string input)
    {
        _logger.LogInformation("COBOL input: {Input}", input);

        var psi = new ProcessStartInfo
        {
            FileName               = _cobolExePath,
            RedirectStandardInput  = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
            // GnuCOBOL trabalha em Latin-1; ajuste se seu ambiente compilou em UTF-8
            StandardInputEncoding  = Encoding.Latin1,
            StandardOutputEncoding = Encoding.Latin1
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        await process.StandardInput.WriteLineAsync(input);
        process.StandardInput.Close();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error  = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(error))
            _logger.LogWarning("COBOL stderr: {Error}", error);

        _logger.LogInformation("COBOL output: {Output}", output);
        return output.Trim();
    }

    // Consulta: manda os dados já buscados do banco para o COBOL validar/formatar.
    public async Task<CobolConsultaResult> ConsultarAsync(Cliente cliente)
    {
        // ACAO|CODIGO|NOME|TELEFONE|EMAIL|NOVO_TELEFONE|NOVO_EMAIL
        string input = $"CONSULTAR|{cliente.Codigo}|{cliente.Nome}|{cliente.Telefone}|{cliente.Email}||";
        string output = await ExecutarAsync(input);

        if (output.StartsWith("OK|"))
        {
            var partes = output.Split('|');
            return new CobolConsultaResult
            {
                Sucesso = true,
                Cliente = new Cliente
                {
                    Codigo   = partes.ElementAtOrDefault(1) ?? cliente.Codigo,
                    Nome     = partes.ElementAtOrDefault(2) ?? cliente.Nome,
                    Telefone = partes.ElementAtOrDefault(3) ?? cliente.Telefone,
                    Email    = partes.ElementAtOrDefault(4) ?? cliente.Email
                }
            };
        }

        if (output == "NOTFOUND")
            return new CobolConsultaResult { Sucesso = false, NaoEncontrado = true };

        return new CobolConsultaResult { Sucesso = false, Erro = output };
    }

    // Atualização: o COBOL valida os novos dados e sinaliza se deve persistir.
    public async Task<CobolAtualizacaoResult> AtualizarAsync(Cliente clienteAtual, string novoTelefone, string novoEmail)
    {
        string input = $"ATUALIZAR|{clienteAtual.Codigo}|{clienteAtual.Nome}|{clienteAtual.Telefone}|{clienteAtual.Email}|{novoTelefone}|{novoEmail}";
        string output = await ExecutarAsync(input);

        if (output.StartsWith("ATUALIZADO|"))
        {
            var partes = output.Split('|');
            return new CobolAtualizacaoResult
            {
                Sucesso      = true,
                NovoTelefone = partes.ElementAtOrDefault(2) ?? novoTelefone,
                NovoEmail    = partes.ElementAtOrDefault(3) ?? novoEmail
            };
        }

        if (output == "NOTFOUND")
            return new CobolAtualizacaoResult { Sucesso = false, NaoEncontrado = true };

        return new CobolAtualizacaoResult { Sucesso = false, Erro = output };
    }
}

public class CobolConsultaResult
{
    public bool Sucesso { get; set; }
    public bool NaoEncontrado { get; set; }
    public Cliente? Cliente { get; set; }
    public string? Erro { get; set; }
}

public class CobolAtualizacaoResult
{
    public bool Sucesso { get; set; }
    public bool NaoEncontrado { get; set; }
    public string NovoTelefone { get; set; } = string.Empty;
    public string NovoEmail { get; set; } = string.Empty;
    public string? Erro { get; set; }
}
