namespace ControleDeGastos.API.Models;

public class Gasto
{
    public int Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime Data { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public TipoGasto Tipo { get; set; }
    public FormaPagamento FormaPagamento { get; set; }
    public List<string> Tags { get; set; } = new();
}
