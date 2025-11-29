using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSimulatorAPI.Models
{
    public class clsPartida
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPartida { get; set; }
        public required string NombreSala { get; set; }
        public required string NombreDocente { get; set; }
        public int CantidadJugadores { get; set; }
        public DateOnly FechaFinal { get; set; }
    }
}
