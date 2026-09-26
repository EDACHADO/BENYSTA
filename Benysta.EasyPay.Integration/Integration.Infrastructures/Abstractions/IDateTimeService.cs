namespace Integration.Infrastructures.Abstractions;

public interface IDateTimeService
{
    DateTime NowUTC { get; }
}