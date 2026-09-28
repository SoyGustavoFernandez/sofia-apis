using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.SeriesFiscales.Commands.DeleteSerieFiscal;

public record DeleteSerieFiscalCommand(Guid Id) : ICommand;
