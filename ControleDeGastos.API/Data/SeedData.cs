using ControleDeGastos.API.Models;

namespace ControleDeGastos.API.Data;

public static class SeedData
{
    public static void Popular(AppDbContext contexto)
    {
        if (contexto.Gastos.Any())
        {
            return;
        }

        var hoje = DateTime.Today;

        contexto.Gastos.AddRange(
            new Gasto
            {
                Descricao = "Salário",
                Valor = 5500,
                Data = hoje.AddDays(-10),
                Categoria = "Salário",
                Tipo = TipoGasto.Receita,
                FormaPagamento = FormaPagamento.TransferenciaBancaria,
                Tags = new List<string> { "fixo" }
            },
            new Gasto
            {
                Descricao = "Supermercado",
                Valor = 480.50m,
                Data = hoje.AddDays(-8),
                Categoria = "Alimentação",
                Tipo = TipoGasto.Despesa,
                FormaPagamento = FormaPagamento.CartaoDebito,
                Tags = new List<string> { "essencial" }
            },
            new Gasto
            {
                Descricao = "Aluguel",
                Valor = 1800,
                Data = hoje.AddDays(-7),
                Categoria = "Moradia",
                Tipo = TipoGasto.Despesa,
                FormaPagamento = FormaPagamento.Boleto,
                Tags = new List<string> { "fixo", "essencial" }
            },
            new Gasto
            {
                Descricao = "Assinatura streaming",
                Valor = 39.90m,
                Data = hoje.AddDays(-5),
                Categoria = "Lazer",
                Tipo = TipoGasto.Despesa,
                FormaPagamento = FormaPagamento.CartaoCredito,
                Tags = new List<string> { "assinatura" }
            },
            new Gasto
            {
                Descricao = "Freelance",
                Valor = 900,
                Data = hoje.AddDays(-3),
                Categoria = "Renda extra",
                Tipo = TipoGasto.Receita,
                FormaPagamento = FormaPagamento.Pix,
                Tags = new List<string> { "extra" }
            },
            new Gasto
            {
                Descricao = "Uber",
                Valor = 32.70m,
                Data = hoje.AddDays(-1),
                Categoria = "Transporte",
                Tipo = TipoGasto.Despesa,
                FormaPagamento = FormaPagamento.Pix,
                Tags = new List<string>()
            }
        );

        contexto.SaveChanges();
    }
}
