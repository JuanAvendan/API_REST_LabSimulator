using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LabSimulatorAPI.Models
{
    public class clsUsuario
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdUsuario { get; set; }
        [Key]
        public required string Usuario { get; set; }
        public required string Rol { get; set; }
    }
}
