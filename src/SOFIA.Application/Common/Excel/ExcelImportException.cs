using SOFIA.Domain.Common;

namespace SOFIA.Application.Common.Excel;

// Thrown by IExcelReaderService when an upload is rejected; the API maps it to a 400 carrying Error.Code
public sealed class ExcelImportException(Error error) : Exception(error.Message)
{
    public const string ArchivoInvalido = "CargaMasiva.Archivo.Invalido";
    public const string ArchivoExcedeDescompresion = "CargaMasiva.Archivo.ExcedeDescompresion";
    public const string FilasExcedidas = "CargaMasiva.Filas.Excedidas";

    public Error Error { get; } = error;
}
