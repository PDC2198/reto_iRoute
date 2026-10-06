using CommerceApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CommerceApi.Services;

public class CommerceService(IConfiguration config)
{
    private string Cs => config.GetConnectionString("DefaultConnection")!;

    public async Task InsertAsync(CommerceCsvRow row)
    {
        await using var connection = new SqlConnection(Cs);

        await using var command = new SqlCommand(
            "dbo.sp_create_commerce",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue(
            "@pc_processdate",
            row.PcProcessDate.Date);

        command.Parameters.AddWithValue(
            "@pc_nomcomred",
            (object?)row.PcNomcomred ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@pc_numdoc",
            (object?)row.PcNumdoc ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@pc_address",
            (object?)row.PcAddress ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@pc_city",
            (object?)row.PcCity ?? DBNull.Value);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> ProcessAsync(DateTime processDate)
    {
        await using var connection = new SqlConnection(Cs);

        await using var command = new SqlCommand(
            "dbo.sp_process_commerce",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue(
            "@pc_processdate",
            processDate.Date);

        await connection.OpenAsync();

        return Convert.ToInt32(
            await command.ExecuteScalarAsync());
    }

    public async Task<List<QuarantineRow>> QuarantineAsync()
    {
        var list = new List<QuarantineRow>();

        await using var connection = new SqlConnection(Cs);

        await using var command = new SqlCommand(
            @"SELECT
                id,
                pc_processdate,
                pc_nomcomred,
                pc_numdoc,
                pc_address,
                pc_city,
                motivo
              FROM dbo.commerce_quarantine
              ORDER BY id DESC",
            connection);

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new QuarantineRow(
                reader.GetInt32(0),
                reader.IsDBNull(1)
                    ? null
                    : reader.GetDateTime(1),
                reader.IsDBNull(2)
                    ? null
                    : reader.GetString(2),
                reader.IsDBNull(3)
                    ? null
                    : reader.GetString(3),
                reader.IsDBNull(4)
                    ? null
                    : reader.GetString(4),
                reader.IsDBNull(5)
                    ? null
                    : reader.GetString(5),
                reader.GetString(6)
            ));
        }

        return list;
    }
}