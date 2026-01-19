namespace Middagsklok.Features.Shared;

public interface IClock
{
    DateOnly Today { get; }
}

public class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
}
