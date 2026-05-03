using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public class Sucursal : BaseEntity<string>
{
    public string Nombre { get; set; } = string.Empty;
    public string Direccion_Fisica { get; set; } = string.Empty;
    public string Numero_Licencia { get; set; } = string.Empty;

    // Relación con el Gerente (Empleado)
    public string? Gerente_ID { get; set; }
    public virtual Empleado? Gerente { get; set; }

    // Colección de empleados base en esta sucursal
    public virtual ICollection<Empleado> Empleados { get; set; } = [];
}
