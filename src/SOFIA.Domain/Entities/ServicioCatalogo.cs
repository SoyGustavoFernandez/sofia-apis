using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

// A service the pharmacy charges for, such as giving an injection or taking blood pressure
public sealed class ServicioCatalogo : BaseEntity
{
    public const int NombreMaxLength = 150;
    public const decimal PrecioMaximo = 99999.99m;

    private ServicioCatalogo() { }

    public string Nombre { get; private set; } = string.Empty;
    public decimal Precio { get; private set; }
    public bool AplicaMedicamento { get; private set; }
    public bool RegistraResultado { get; private set; }

    public static Result<ServicioCatalogo> Create(string nombre, decimal precio, bool aplicaMedicamento = false, bool registraResultado = false)
    {
        var servicio = new ServicioCatalogo();
        var result = servicio.Update(nombre, precio, aplicaMedicamento, registraResultado);
        return result.IsSuccess ? Result.Success(servicio) : Result.Failure<ServicioCatalogo>(result.Error);
    }

    public Result Update(string nombre, decimal precio, bool aplicaMedicamento, bool registraResultado)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > NombreMaxLength)
        {
            return Result.Failure(Error.Validation("ServicioCatalogo.Nombre", $"Name is required and must not exceed {NombreMaxLength} characters."));
        }

        if (precio <= 0 || precio > PrecioMaximo)
        {
            return Result.Failure(Error.Validation("ServicioCatalogo.Precio", "Price must be greater than zero."));
        }

        Nombre = nombre.Trim();
        Precio = precio;
        AplicaMedicamento = aplicaMedicamento;
        RegistraResultado = registraResultado;
        return Result.Success();
    }
}
