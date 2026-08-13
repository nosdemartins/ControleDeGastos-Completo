using System.ComponentModel.DataAnnotations;
using ControleDeGastos.API.Models;

namespace ControleDeGastos.API.DTOs;

public class GastoRequestDto
{
    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [MaxLength(150)]
    public string Descricao { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Valor { get; set; }

    public DateTime Data { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "A categoria é obrigatória.")]
    [MaxLength(60)]
    public string Categoria { get; set; } = string.Empty;

    public TipoGasto Tipo { get; set; }

    public FormaPagamento FormaPagamento { get; set; }

    public List<string> Tags { get; set; } = new();
}

public class ResumoCategoriaDto
{
    public string Categoria { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

public class ResumoDto
{
    public decimal TotalReceitas { get; set; }
    public decimal TotalDespesas { get; set; }
    public decimal Saldo { get; set; }
    public List<ResumoCategoriaDto> DespesasPorCategoria { get; set; } = new();
    public List<ResumoCategoriaDto> ReceitasPorCategoria { get; set; } = new();
}
