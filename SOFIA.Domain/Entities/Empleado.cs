using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public class Empleado : BaseEntity<string>
{
    public string Sucursal_Base_ID { get; set; } = string.Empty;
    public virtual Sucursal? Sucursal_Base { get; set; }

    public string Nombre_Completo { get; set; } = string.Empty;
    public string Rol_Sistema { get; set; } = string.Empty;
    public string? Licencia_Prof { get; set; }

    // BLOB en SQL Server -> byte[]
    public byte[]? Huella_Biometrica { get; set; }

    // Si es gerente de una sucursal
    public virtual Sucursal? Sucursal_Gerenciada { get; set; }
}
