using LabSimulatorAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LabSimulatorAPI.Context
{
    public class LabSimulatorContext(DbContextOptions<LabSimulatorContext> options) : DbContext(options)
    {
        public DbSet<clsUsuario> tbl_Usuario { get; set; }
        public DbSet<clsPartida> tbl_Partida { get; set; }
        public DbSet<clsEscenario1> tbl_Escenario1BombaInfusion { get; set; }
        public DbSet<clsUsuarioPartida> tbl_UsuarioPartida { get; set; }
    }
}
