using Integration.Infrastructures.Abstractions;

namespace Integration.Infrastructures.Concrete
{
    public class DateTimeService : IDateTimeService
    {
        public DateTime NowUTC => DateTime.UtcNow;
    }
}
