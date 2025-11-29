namespace LabSimulatorAPI.Models
{
    public class clsUsuariosPartidas
    {
        public string Usuario { get; set; }
        public string NombreSala { get; set; }
        public string NombreDocente { get; set; }
        public int CantidadJugadores { get; set; }
        public DateOnly FechaFinal { get; set; }
    }
}
