using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSimulatorAPI.Models
{
    public class clsEscenario1
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEscenario1 { get; set; }
        public required string usuario { get; set; }
        public int PuntajeCableDañado { get; set; }
        public int PuntajeSetIncorporado { get; set; }
        public int PuntajeFuenteDesconectada { get; set; }
        public int PuntajeBombaSucia { get; set; }
        public int PuntajeCableDesconectado { get; set; }
        public int PuntajePurga { get; set; }
        public int PuntajeDocumentoPaciente { get; set; }
        public int PuntajeMedicamentoCorrecto { get; set; }
        public int PuntajeConfigurarLineas { get; set; }
        public int PuntajeAreaCorrecta { get; set; }
        public int PuntajeSticker { get; set; }
        public TimeOnly TiempoRestante { get; set; }
        public required string MedicamentoSeleccionado { get; set; }
        public required string PatologiaSeleccionada { get; set; }
        public int CalificacionTotal { get; set; }
    }
}
