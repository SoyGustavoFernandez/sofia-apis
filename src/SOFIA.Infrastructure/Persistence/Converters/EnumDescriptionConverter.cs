using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SOFIA.Domain.Common;

namespace SOFIA.Infrastructure.Persistence.Converters;

public class EnumDescriptionConverter<TEnum> : ValueConverter<TEnum, string> where TEnum : Enum
{
    public EnumDescriptionConverter()
        : base(
            v => v.GetDescription(),
            v => EnumExtensions.GetValueFromDescription<TEnum>(v))
    {
    }
}
