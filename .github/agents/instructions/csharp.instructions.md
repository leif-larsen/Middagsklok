---
applyTo: "**/*.cs"
---

# C# Coding Standards

## Language Features

- Use latest stable **.NET** and **C#** features
- Modern constructs: file-scoped namespaces, records, pattern matching, primary constructors, collection expressions
- Use `Span<T>` for performance-critical code

## Design Principles

- Follow **SOLID principles**
- Prefer explicit, dependency-injected designs
- Keep code self-documenting — minimize comments, refactor for clarity

## Naming Conventions

| Element | Style | Example |
|---------|-------|---------|
| Classes/Interfaces | PascalCase | `CustomerService`, `ICustomerRepository` |
| Methods/Properties | PascalCase | `Execute`, `CustomerId` |
| Parameters/Locals | camelCase | `customerId`, `result` |
| Private fields | camelCase | `customer`, `logger` |
| Constants | PascalCase | `MaxRetryCount` |
| Async methods | **No** "Async" suffix | `GetCustomer()` not `GetCustomerAsync()` |

## Nullability

- Enable **nullable reference types** in all projects
- Use `string?` for nullable, `string` for non-nullable
- Use null-forgiving (`!`) sparingly — prefer null checks

## Exception Handling

- Validate inputs at public API boundaries — fail fast
- Throw meaningful exceptions (`ArgumentNullException`, `InvalidOperationException`)
- Catch at application boundaries when you can handle/recover
- **Never** swallow exceptions or use them for control flow

## Logging

- Use **structured logging** with message templates:
  ```csharp
  _logger.LogInformation("Customer {CustomerId} retrieved", customerId);
  ```
- **Never log** passwords, tokens, PII, or secrets

## CancellationToken Guidelines

**Use when:**
- External API calls (unpredictable latency)
- User-cancellable operations (search, file uploads)
- Long-running background work

**Skip when:**
- Simple CRUD/database queries
- Fast operations (<1s)
- Operations that should complete atomically

Keep it simple — don't add CancellationToken unless it provides clear value.