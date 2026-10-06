using CommerceApi.Models;
using CommerceApi.Services;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CommerceApi.Controllers;

[ApiController]
[Route("api/commerce")]
public class CommerceController(CommerceService service) : ControllerBase
{
    private static readonly string[] Required =
    {
        "pc_processdate",
        "pc_nomcomred",
        "pc_numdoc",
        "pc_address",
        "pc_city"
    };

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("El archivo está vacío.");
        }

        if (!Path.GetExtension(file.FileName)
            .Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Solo se permiten archivos CSV.");
        }

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());

            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null
            };

            using var csv = new CsvReader(reader, configuration);

            if (!await csv.ReadAsync() || !csv.ReadHeader())
            {
                return BadRequest("El CSV no contiene cabecera.");
            }

            var headers = csv.HeaderRecord ?? [];

            var missingColumns = Required
                .Where(column =>
                    !headers.Contains(column, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            if (missingColumns.Length > 0)
            {
                return BadRequest(new
                {
                    message = "Formato CSV incorrecto.",
                    missingColumns,
                    expectedColumns = Required
                });
            }

            var inserted = 0;
            var rowNumber = 1;

            while (await csv.ReadAsync())
            {
                rowNumber++;

                var dateText = csv.GetField("pc_processdate");

                if (!DateTime.TryParse(
                        dateText,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var processDate)
                    && !DateTime.TryParse(dateText, out processDate))
                {
                    return BadRequest(
                        $"Fecha inválida en la fila {rowNumber}: {dateText}");
                }

                var row = new CommerceCsvRow
                {
                    PcProcessDate = processDate,
                    PcNomcomred = csv.GetField("pc_nomcomred"),
                    PcNumdoc = csv.GetField("pc_numdoc"),
                    PcAddress = csv.GetField("pc_address"),
                    PcCity = csv.GetField("pc_city")
                };

                await service.InsertAsync(row);
                inserted++;
            }

            if (inserted == 0)
            {
                return BadRequest("El CSV no contiene registros.");
            }

            return Ok(new
            {
                message = "Archivo cargado correctamente.",
                inserted
            });
        }
        catch (HeaderValidationException)
        {
            return BadRequest(
                "La cabecera del CSV no tiene el formato esperado.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "No se pudo procesar el archivo.",
                detail = ex.Message
            });
        }
    }

    [HttpPost("process/{date}")]
    public async Task<IActionResult> Process(DateTime date)
    {
        var quarantined = await service.ProcessAsync(date);

        return Ok(new
        {
            processDate = date.Date,
            quarantined
        });
    }

    [HttpGet("quarantine")]
    public async Task<IActionResult> Quarantine()
    {
        var records = await service.QuarantineAsync();

        return Ok(records);
    }
}