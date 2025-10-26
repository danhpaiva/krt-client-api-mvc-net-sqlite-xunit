using KRT.Cliente.Api.Data;
using KRT.Cliente.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace KRT.Cliente.Api.Controllers;

[Route("v1/api/[controller]")]
[ApiController]
public class ContasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;

    public ContasController(AppDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    private async Task InvalidateCaches(Conta conta)
    {
        await _cache.RemoveAsync($"conta:{conta.CPF}");

        await _cache.RemoveAsync("ResumoStatus");
        await _cache.RemoveAsync("ContasAtivas");
        await _cache.RemoveAsync("ContasInativas");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Conta>>> GetConta()
    {
        return await _context.TB_Contas.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Conta>> GetConta(Guid id)
    {
        var conta = await _context.TB_Contas.FindAsync(id);

        if (conta == null)
        {
            return NotFound();
        }

        return conta;
    }

    [HttpGet("EndPointSistemaTerceiros/{id}")]
    public async Task<ActionResult<ContaDTO>> GetContasSistemasTerceiros(Guid id)
    {
        var conta = await _context.TB_Contas.FindAsync(id);

        if (conta == null)
        {
            return NotFound();
        }

        var contaDTO = new ContaDTO
        {
            Id = conta.Id,
            Nome = conta.NomeTitular,
            CPF = conta.CPF,
            StatusConta = conta.Status ? "Ativa" : "Inativa"
        };

        return contaDTO;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutConta(Guid id, Conta conta)
    {
        if (id != conta.Id)
        {
            return BadRequest();
        }

        _context.Entry(conta).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            await InvalidateCaches(conta);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ContaExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<Conta>> PostConta(Conta conta)
    {
        _context.TB_Contas.Add(conta);
        await _context.SaveChangesAsync();

        await InvalidateCaches(conta);

        return CreatedAtAction("GetConta", new { id = conta.Id }, conta);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConta(Guid id)
    {
        var conta = await _context.TB_Contas.FindAsync(id);
        if (conta == null)
        {
            return NotFound();
        }

        _context.TB_Contas.Remove(conta);
        await _context.SaveChangesAsync();

        await InvalidateCaches(conta);

        return NoContent();
    }

    private bool ContaExists(Guid id)
    {
        return _context.TB_Contas.Any(e => e.Id == id);
    }

    [HttpGet("Ativos")]
    public async Task<ActionResult<IEnumerable<ContaDTO>>> GetContasAtivas()
    {
        var cacheKey = "ContasAtivas";
        var cached = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cached))
            return Ok(JsonSerializer.Deserialize<List<ContaDTO>>(cached));

        var contasAtivas = await _context.TB_Contas
            .Where(c => c.Status)
            .Select(c => new ContaDTO
            {
                Id = c.Id,
                Nome = c.NomeTitular,
                CPF = c.CPF,
                StatusConta = "Ativa"
            })
            .ToListAsync();

        if (!contasAtivas.Any())
            return NotFound("Nenhuma conta ativa encontrada.");

        var expiration = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1) - DateTime.UtcNow;
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(contasAtivas), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });

        return Ok(contasAtivas);
    }


    [HttpGet("Inativos")]
    public async Task<ActionResult<IEnumerable<ContaDTO>>> GetContasInativas()
    {
        var cacheKey = "ContasInativas";
        var cached = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cached))
            return Ok(JsonSerializer.Deserialize<List<ContaDTO>>(cached));

        var contasInativas = await _context.TB_Contas
            .Where(c => !c.Status)
            .Select(c => new ContaDTO
            {
                Id = c.Id,
                Nome = c.NomeTitular,
                CPF = c.CPF,
                StatusConta = "Inativa"
            })
            .ToListAsync();

        if (!contasInativas.Any())
            return NotFound("Nenhuma conta inativa encontrada.");

        var expiration = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1) - DateTime.UtcNow;
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(contasInativas), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });

        return Ok(contasInativas);
    }


    [HttpGet("TotaisPorAno")]
    public async Task<ActionResult<List<TotalAnoDTO>>> GetTotaisPorAno([FromQuery] int[] anos)
    {
        if (anos == null || anos.Length == 0)
            return BadRequest("Informe pelo menos um ano. Exemplo: /v1/api/Contas/TotaisPorAno?anos=2024&anos=2025");

        var cacheKey = "TotaisPorAno:" + string.Join(",", anos.OrderBy(x => x));
        var cached = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cached))
            return Ok(JsonSerializer.Deserialize<List<TotalAnoDTO>>(cached));

        var totais = await _context.TB_Contas
            .Where(c => anos.Contains(c.CriadoEm.Year))
            .GroupBy(c => c.CriadoEm.Year)
            .Select(g => new TotalAnoDTO
            {
                Ano = g.Key,
                TotalClientes = g.Count()
            })
            .OrderBy(x => x.Ano)
            .ToListAsync();

        if (!totais.Any())
            return NotFound("Nenhum cliente encontrado para os anos informados.");

        var expiration = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1) - DateTime.UtcNow;
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(totais), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });

        return Ok(totais);
    }


    [HttpPatch("{id}/ativar")]
    public async Task<IActionResult> AtivarConta(Guid id)
    {
        var conta = await _context.TB_Contas.FindAsync(id);
        if (conta == null)
            return NotFound();

        if (conta.Status)
            return BadRequest("A conta já está ativa.");

        conta.Status = true;
        conta.AtualizadoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await InvalidateCaches(conta);

        return Ok(new { mensagem = "Conta ativada com sucesso.", conta.Id, conta.Status });
    }

    [HttpPatch("{id}/inativar")]
    public async Task<IActionResult> InativarConta(Guid id)
    {
        var conta = await _context.TB_Contas.FindAsync(id);
        if (conta == null)
            return NotFound();

        if (!conta.Status)
            return BadRequest("A conta já está inativa.");

        conta.Status = false;
        conta.AtualizadoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await InvalidateCaches(conta);

        return Ok(new { mensagem = "Conta inativada com sucesso.", conta.Id, conta.Status });
    }

    [HttpGet("cpf/{cpf}")]
    public async Task<ActionResult<ContaDTO>> GetContaPorCPF(string cpf)
    {
        var cacheKey = $"conta:{cpf}";
        var cachedConta = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedConta))
        {
            var dtoFromCache = JsonSerializer.Deserialize<ContaDTO>(cachedConta);
            return Ok(dtoFromCache);
        }

        var conta = await _context.TB_Contas
            .FirstOrDefaultAsync(c => c.CPF == cpf);

        if (conta == null)
            return NotFound("Conta não encontrada.");

        var dto = new ContaDTO
        {
            Id = conta.Id,
            Nome = conta.NomeTitular,
            CPF = conta.CPF,
            StatusConta = conta.Status ? "Ativa" : "Inativa"
        };

        // Definir expiração até o final do dia
        var expiration = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1) - DateTime.UtcNow;
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), cacheOptions);

        return Ok(dto);
    }

    [HttpGet("ResumoStatus")]
    public async Task<ActionResult<ResumoStatusDTO>> GetResumoStatus()
    {
        var cacheKey = "ResumoStatus";
        var cached = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cached))
        {
            return Ok(JsonSerializer.Deserialize<ResumoStatusDTO>(cached));
        }

        var totalAtivas = await _context.TB_Contas.CountAsync(c => c.Status);
        var totalInativas = await _context.TB_Contas.CountAsync(c => !c.Status);

        var resumo = new ResumoStatusDTO
        {
            TotalAtivas = totalAtivas,
            TotalInativas = totalInativas,
            TotalGeral = totalAtivas + totalInativas
        };

        var expiration = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1) - DateTime.UtcNow;
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(resumo), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });

        return Ok(resumo);
    }


    [HttpGet("PorPeriodo")]
    public async Task<ActionResult<IEnumerable<ContaDTO>>> GetContasPorPeriodo(DateTime inicio, DateTime fim)
    {
        var contas = await _context.TB_Contas
            .Where(c => c.CriadoEm >= inicio && c.CriadoEm <= fim)
            .Select(c => new ContaDTO
            {
                Id = c.Id,
                Nome = c.NomeTitular,
                CPF = c.CPF,
                StatusConta = c.Status ? "Ativa" : "Inativa"
            })
            .ToListAsync();

        if (!contas.Any())
            return NotFound("Nenhuma conta encontrada no período informado.");

        return Ok(contas);
    }

    [HttpPatch("{id}/softdelete")]
    public async Task<IActionResult> SoftDelete(Guid id)
    {
        var conta = await _context.TB_Contas.FindAsync(id);
        if (conta == null)
            return NotFound();

        conta.DeletadoEm = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await InvalidateCaches(conta);

        return Ok(new { mensagem = "Conta marcada como deletada.", conta.Id });
    }

    [HttpGet("Deletadas")]
    public async Task<ActionResult<IEnumerable<ContaDTO>>> GetContasDeletadas()
    {
        var contas = await _context.TB_Contas
            .Where(c => c.DeletadoEm != null)
            .Select(c => new ContaDTO
            {
                Id = c.Id,
                Nome = c.NomeTitular,
                CPF = c.CPF,
                StatusConta = c.Status ? "Ativa" : "Inativa"
            })
            .ToListAsync();

        if (!contas.Any())
            return NotFound("Nenhuma conta deletada encontrada.");

        return Ok(contas);
    }

    [HttpPatch("{id}/restaurar")]
    public async Task<IActionResult> RestaurarConta(Guid id)
    {
        var conta = await _context.TB_Contas.FindAsync(id);
        if (conta == null)
            return NotFound();

        if (conta.DeletadoEm == null)
            return BadRequest("A conta não está marcada como deletada.");

        conta.DeletadoEm = null;
        conta.AtualizadoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await InvalidateCaches(conta);

        return Ok(new { mensagem = "Conta restaurada com sucesso.", conta.Id });
    }
}