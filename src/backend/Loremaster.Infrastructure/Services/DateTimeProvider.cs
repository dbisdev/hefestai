using Loremaster.Application.Common.Interfaces;

namespace Loremaster.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
