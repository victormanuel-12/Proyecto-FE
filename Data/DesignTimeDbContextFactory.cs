using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SPIn.Common.Seguridad;

namespace SPIn.Data;

/// <summary>Lo usa "dotnet ef migrations add" para crear el contexto sin levantar la aplicación.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=datos/spin.db")
            .Options;
        return new ApplicationDbContext(options, new TenantSistema());
    }
}
