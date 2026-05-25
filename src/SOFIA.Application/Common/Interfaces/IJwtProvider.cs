using SOFIA.Domain.Entities;

namespace SOFIA.Application.Common.Interfaces;

public interface IJwtProvider
{
    string Generate(Cuenta cuenta);
}
