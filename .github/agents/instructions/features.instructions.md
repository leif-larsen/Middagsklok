---
applyTo: "**/Features/**/*.cs"
---

# Feature Pattern

## Core Principles

1. **Single Responsibility** — one feature, one purpose
2. **One Public Method** — always named `Execute`
3. **Features NEVER call other Features** — use services/domain for composition
4. **Public interface, internal implementation**

## Structure

```csharp
// Interface - exactly one Execute method
public interface IGetCustomerFeature
{
    Task<Customer?> Execute(CustomerId id);
}

// Implementation - internal class with primary constructor
internal class GetCustomerFeature(IDbContextFactory<AppDbContext> dbFactory) 
    : IGetCustomerFeature
{
    public async Task<Customer?> Execute(CustomerId id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id);
    }
}
```

## Naming

| Type | Convention | Example |
|------|------------|---------|
| Query | `IGet{Entity}Feature` | `IGetCustomerFeature` |
| List | `IGet{Entities}Feature` | `IGetCustomersFeature` |
| Command | `I{Action}{Entity}Feature` | `ICreateOrderFeature` |
| Validation | `IValidate{Entity}Feature` | `IValidateOrderFeature` |

## Read Features

- Inject `IDbContextFactory<TContext>`
- Use `AsNoTracking()` for read-only queries
- Use `AsSplitQuery()` when including multiple collections

## Write Features

- Load entity → apply domain logic → save changes
- Use domain methods for business rules
- Return created/updated entity or result type

## Registration

```csharp
services.AddScoped<IGetCustomerFeature, GetCustomerFeature>();
```

## ❌ Anti-Patterns

```csharp
// BAD: Feature calling another feature
internal class CreateOrderFeature(IGetCustomerFeature getCustomer) { }

// BAD: Multiple public methods
public interface IBadFeature
{
    Task<Customer> GetById(int id);
    Task<List<Customer>> GetAll();  // Split into separate features!
}

// BAD: Named something other than Execute
public Task<Customer> GetCustomer(int id);  // Should be Execute()
```