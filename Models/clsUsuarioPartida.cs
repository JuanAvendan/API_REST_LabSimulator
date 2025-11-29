using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSimulatorAPI.Models
{
    public class clsUsuarioPartida
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idUsuarioPartida { get; set; }
        public required string Usuario { get; set; }
        public int idPartida { get; set; }
    }
}
