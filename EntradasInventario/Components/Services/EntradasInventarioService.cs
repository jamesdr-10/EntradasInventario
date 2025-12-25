using System.Linq.Expressions;
using EntradasInventario.Data;
using EntradasInventario.Models;
using Microsoft.EntityFrameworkCore;

namespace EntradasInventario.Services;

public class EntradasInventarioService(IDbContextFactory<ApplicationDbContext> DbFactory)
{
    private async Task<bool> Existe(int entradaId)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        return await contexto.Entradas.AnyAsync(e => e.EntradaId == entradaId);
    }

    private async Task<bool> Insertar(Entrada entrada)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        contexto.Entradas.Add(entrada);
        await AfectarEntradaInventario(entrada.EntradaDetalles.ToArray(), TipoOperacion.Suma);
        return await contexto.SaveChangesAsync() > 0;
    }

    private async Task AfectarEntradaInventario(EntradaDetalle[] detalle, TipoOperacion tipoOperacion)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        foreach (var item in detalle)
        {
            var producto = await contexto.Productos.SingleAsync(p => p.ProductoId == item.ProductoId);
            if (tipoOperacion == TipoOperacion.Suma)
            {
                producto.Existencia += item.Cantidad;
            }
            else
            {
                producto.Existencia -= item.Cantidad;
            }
        }

        await contexto.SaveChangesAsync();
    }

    private async Task<bool> Modificar(Entrada entrada)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();

        var anterior = await contexto.Entradas
            .Include(e => e.EntradaDetalles)
            .FirstOrDefaultAsync(e => e.EntradaId == entrada.EntradaId);

        if (anterior == null)
            return false;
        
        await AfectarEntradaInventario(anterior.EntradaDetalles.ToArray(), TipoOperacion.Resta);
        await AfectarEntradaInventario(entrada.EntradaDetalles.ToArray(), TipoOperacion.Suma);

        contexto.EntradaDetalles.RemoveRange(anterior.EntradaDetalles);
        anterior.EntradaDetalles = entrada.EntradaDetalles;
        anterior.Concepto = entrada.Concepto;
        anterior.Fecha = entrada.Fecha;
        anterior.Total = entrada.Total;

        return await contexto.SaveChangesAsync() > 0;
    }

    public async Task<bool> Guardar(Entrada entrada)
    {
        if (!await Existe(entrada.EntradaId))
        {
            return await Insertar(entrada);
        }
        else
        {
            return await Modificar(entrada);
        }
    }

    public async Task<bool> Eliminar(int entradaId)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();

        var entrada = await contexto.Entradas
            .Include(e => e.EntradaDetalles)
            .FirstOrDefaultAsync(e => e.EntradaId == entradaId);

        if (entrada == null)
            return false;

        await AfectarEntradaInventario(entrada.EntradaDetalles.ToArray(), TipoOperacion.Resta);

        contexto.EntradaDetalles.RemoveRange(entrada.EntradaDetalles);
        contexto.Entradas.Remove(entrada);

        return await contexto.SaveChangesAsync() > 0;
    }

    public async Task<Entrada?> Buscar(int entradaId)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        return await contexto.Entradas
            .Include(e => e.EntradaDetalles)
            .ThenInclude(p => p.Producto)
            .FirstOrDefaultAsync(e => e.EntradaId == entradaId);
    }

    public async Task<List<Entrada>> Listar(Expression<Func<Entrada, bool>> criterio)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        return await contexto.Entradas.AsNoTracking().Where(criterio).ToListAsync();
    }
}

public enum TipoOperacion
{
    Suma = 1,
    Resta = 2
}
