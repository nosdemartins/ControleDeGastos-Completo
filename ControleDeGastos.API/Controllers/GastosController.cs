using ControleDeGastos.API.Data;
using ControleDeGastos.API.DTOs;
using ControleDeGastos.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GastosController : ControllerBase
{
    private readonly AppDbContext _contexto;

    public GastosController(AppDbContext contexto)
    {
        _contexto = contexto;
    }
    [HttpGet("google")]
public IActionResult Google()
{
    return Redirect("https://www.google.com");
}

    // GET api/gastos?tipo=Despesa&categoria=Alimentação&dataInicio=2026-01-01&dataFim=2026-12-31
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Gasto>>> Listar(
        [FromQuery] TipoGasto? tipo,
        [FromQuery] string? categoria,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim)
    {
        var query = _contexto.Gastos.AsQueryable();

        if (tipo is not null)
        {
            query = query.Where(g => g.Tipo == tipo);
        }

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            query = query.Where(g => g.Categoria.ToLower() == categoria.ToLower());
        }

        if (dataInicio is not null)
        {
            query = query.Where(g => g.Data >= dataInicio.Value);
        }

        if (dataFim is not null)
        {
            query = query.Where(g => g.Data <= dataFim.Value);
        }

        var gastos = await query.OrderByDescending(g => g.Data).ToListAsync();
        return Ok(gastos);
    }

    // GET api/gastos/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Gasto>> ObterPorId(int id)
    {
        var gasto = await _contexto.Gastos.FindAsync(id);

        if (gasto is null)
        {
            return NotFound(new { mensagem = $"Gasto com id {id} não encontrado." });
        }

        return Ok(gasto);
    }

    // GET api/gastos/resumo
    [HttpGet("resumo")]
    public async Task<ActionResult<ResumoDto>> ObterResumo()
    {
        var gastos = await _contexto.Gastos.ToListAsync();

        var totalReceitas = gastos.Where(g => g.Tipo == TipoGasto.Receita).Sum(g => g.Valor);
        var totalDespesas = gastos.Where(g => g.Tipo == TipoGasto.Despesa).Sum(g => g.Valor);

        var resumo = new ResumoDto
        {
            TotalReceitas = totalReceitas,
            TotalDespesas = totalDespesas,
            Saldo = totalReceitas - totalDespesas,
            DespesasPorCategoria = gastos
                .Where(g => g.Tipo == TipoGasto.Despesa)
                .GroupBy(g => g.Categoria)
                .Select(grupo => new ResumoCategoriaDto { Categoria = grupo.Key, Total = grupo.Sum(g => g.Valor) })
                .OrderByDescending(r => r.Total)
                .ToList(),
            ReceitasPorCategoria = gastos
                .Where(g => g.Tipo == TipoGasto.Receita)
                .GroupBy(g => g.Categoria)
                .Select(grupo => new ResumoCategoriaDto { Categoria = grupo.Key, Total = grupo.Sum(g => g.Valor) })
                .OrderByDescending(r => r.Total)
                .ToList()
        };

        return Ok(resumo);
    }

    // POST api/gastos
    [HttpPost]
    public async Task<ActionResult<Gasto>> Criar([FromBody] GastoRequestDto dto)
    {
        var gasto = new Gasto
        {
            Descricao = dto.Descricao,
            Valor = dto.Valor,
            Data = dto.Data,
            Categoria = dto.Categoria,
            Tipo = dto.Tipo,
            FormaPagamento = dto.FormaPagamento,
            Tags = dto.Tags ?? new List<string>()
        };

        _contexto.Gastos.Add(gasto);
        await _contexto.SaveChangesAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = gasto.Id }, gasto);
    }

    // PUT api/gastos/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] GastoRequestDto dto)
    {
        var gasto = await _contexto.Gastos.FindAsync(id);

        if (gasto is null)
        {
            return NotFound(new { mensagem = $"Gasto com id {id} não encontrado." });
        }

        gasto.Descricao = dto.Descricao;
        gasto.Valor = dto.Valor;
        gasto.Data = dto.Data;
        gasto.Categoria = dto.Categoria;
        gasto.Tipo = dto.Tipo;
        gasto.FormaPagamento = dto.FormaPagamento;
        gasto.Tags = dto.Tags ?? new List<string>();

        await _contexto.SaveChangesAsync();

        return Ok(gasto);
    }

    // DELETE api/gastos/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id)
    {
        var gasto = await _contexto.Gastos.FindAsync(id);

        if (gasto is null)
        {
            return NotFound(new { mensagem = $"Gasto com id {id} não encontrado." });
        }

        _contexto.Gastos.Remove(gasto);
        await _contexto.SaveChangesAsync();

        return NoContent();
    }
}
