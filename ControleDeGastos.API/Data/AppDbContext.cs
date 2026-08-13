using ControleDeGastos.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Gasto> Gastos => Set<Gasto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Gasto>()
            .Property(g => g.Tags)
            .HasConversion(
                v => string.Join(';', v),
                v => v.Length == 0
                    ? new List<string>()
                    : v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
            .Metadata.SetValueComparer(
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                    (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
                    v => v.Aggregate(0, (hash, tag) => HashCode.Combine(hash, tag.GetHashCode())),
                    v => v.ToList()));

        base.OnModelCreating(modelBuilder);
    }
}
