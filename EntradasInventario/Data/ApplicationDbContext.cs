using EntradasInventario.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EntradasInventario.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Productos> Productos { get; set; }
    public DbSet<Entrada> Entradas { get; set; }
    public DbSet<EntradaDetalle> EntradaDetalles { get; set; }
}
