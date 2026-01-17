namespace Middagsklok.Domain;

public record PlanningRules(
    int WeekdayMaxTotalMinutes = 45,
    int WeekendMaxTotalMinutes = 60,
    int MinFishDinnersPerWeek = 2);
