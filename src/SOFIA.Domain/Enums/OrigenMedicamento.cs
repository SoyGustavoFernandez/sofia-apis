namespace SOFIA.Domain.Enums;

// Where the medicine applied by a service came from; only the first one is charged and deducts stock
public enum OrigenMedicamento
{
    CompradoAhora = 0,
    CompradoAntes = 1,
    TraidoPorPaciente = 2
}
