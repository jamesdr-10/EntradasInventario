using System.Linq.Expressions;
using EntradasInventario.Data;
using EntradasInventario.Models;
using Microsoft.EntityFrameworkCore;

namespace EntradasInventario.Services;

public class ProductosService(IDbContextFactory<ApplicationDbContext> DbFactory)
{
    private async Task<bool> Existe(int productoId)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        return await contexto.Productos.AnyAsync(p => p.ProductoId == productoId);
    }

    private async Task<bool> Insertar(Productos producto)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        contexto.Productos.Add(producto);
        return await contexto.SaveChangesAsync() > 0;
    }

    private async Task<bool> Modificar(Productos producto)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        contexto.Productos.Update(producto);
        return await contexto.SaveChangesAsync() > 0;
    }

    public async Task<bool> Guardar(Productos producto)
    {
        if (!await Existe(producto.ProductoId))
        {
            return await Insertar(producto);
        } else
        {
            return await Modificar(producto);
        }
    }

    public async Task<bool> Eliminar(int productoId)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();

        var producto = await contexto.Productos
            .Include(p => p.EntradaDetalles)
            .FirstOrDefaultAsync(p => p.ProductoId == productoId);

        if (producto == null)
        {
            return false;
        }

        if (producto.EntradaDetalles.Any())
        {
            return false;
        }

        contexto.Productos.Remove(producto);
        await contexto.SaveChangesAsync();

        return true;
    }

    public async Task<Productos?> Buscar(int productoId)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        return await contexto.Productos.FirstOrDefaultAsync(p => p.ProductoId == productoId);
    }

    public async Task<List<Productos>> Listar(Expression<Func<Productos, bool>> criterio)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        return await contexto.Productos.AsNoTracking().Where(criterio).ToListAsync();
    }
}
