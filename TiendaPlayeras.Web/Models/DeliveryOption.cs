namespace TiendaPlayeras.Web.Models
{
    public class DeliveryOption
    {
        public string Id { get; set; } = string.Empty;     // identificador interno
        public string Point { get; set; } = string.Empty;  // nombre del lugar
        public string Schedule { get; set; } = string.Empty; // horario
    }
}
