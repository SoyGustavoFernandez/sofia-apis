using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

// A notice that already went out: about one company's expiry date, or (without company) the owner's summary of one day
public sealed class AvisoEnviado : BaseEntity
{
    private static readonly TimeSpan Peru = TimeSpan.FromHours(-5);

    public const int DiasPrimerAviso = 7;
    public const int DiasUltimoAviso = 1;
    public const int DiasResumen = 0;

    private AvisoEnviado() { }

    public Guid? EmpresaId { get; private set; }
    public DateOnly Fecha { get; private set; }
    public int Dias { get; private set; }

    public static DateOnly DiaEnPeru(DateTimeOffset instante) => DateOnly.FromDateTime(instante.ToOffset(Peru).DateTime);

    // Calendar days between today and the expiry day, both as seen in Peru
    public static int DiasRestantes(DateTimeOffset vencimiento, DateTimeOffset ahora) =>
        DiaEnPeru(vencimiento).DayNumber - DiaEnPeru(ahora).DayNumber;

    // The notice that corresponds today; a missed day is caught up, and the last one replaces a missed first one
    public static int? HitoPara(int diasRestantes) => diasRestantes switch
    {
        < 0 or > DiasPrimerAviso => null,
        <= DiasUltimoAviso => DiasUltimoAviso,
        _ => DiasPrimerAviso
    };

    public static AvisoEnviado DeEmpresa(Guid empresaId, DateTimeOffset vencimiento, int hito) =>
        new() { EmpresaId = empresaId, TenantId = empresaId, Fecha = DiaEnPeru(vencimiento), Dias = hito };

    public static AvisoEnviado ResumenDelDia(DateTimeOffset ahora) => new() { Fecha = DiaEnPeru(ahora), Dias = DiasResumen };
}
