namespace Middagsklok.Contracts.Dishes;

public record DishListItem(
    string Id,
    string Name,
    int ActiveMinutes,
    int TotalMinutes);

public record DishListResponse(IReadOnlyList<DishListItem> Items);
