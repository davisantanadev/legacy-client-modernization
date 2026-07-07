using MySql.Data.MySqlClient;
using ClientesApi.Models;

namespace ClientesApi.Services;

public interface IClienteRepository
{
    Task<Cliente?> BuscarPorCodigoAsync(string codigo);
    Task<bool> AtualizarContatoAsync(string codigo, string telefone, string email);
}

public class ClienteRepository : IClienteRepository
{
    private readonly string _connectionString;

    public ClienteRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("Connection string 'MySql' not found.");
    }

    public async Task<Cliente?> BuscarPorCodigoAsync(string codigo)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            "SELECT codigo, nome, telefone, email FROM clientes WHERE codigo = @codigo", conn);
        cmd.Parameters.AddWithValue("@codigo", codigo.PadLeft(5, '0'));

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new Cliente
        {
            Codigo   = reader.GetString(reader.GetOrdinal("codigo")),
            Nome     = reader.GetString(reader.GetOrdinal("nome")),
            Telefone = reader.GetString(reader.GetOrdinal("telefone")),
            Email    = reader.GetString(reader.GetOrdinal("email"))
        };
    }

    public async Task<bool> AtualizarContatoAsync(string codigo, string telefone, string email)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            "UPDATE clientes SET telefone = @telefone, email = @email WHERE codigo = @codigo", conn);
        cmd.Parameters.AddWithValue("@telefone", telefone);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@codigo", codigo.PadLeft(5, '0'));

        int rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }
}
