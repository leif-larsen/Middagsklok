---
applyTo: "**/*.Tests/**/*.cs,**/*Tests.cs,**/*Test.cs"
---

# Testing Guidelines

## Framework & Tools

- **xUnit** for test framework
- **FakeItEasy** for mocking
- **FluentAssertions** for readable assertions (optional)

## Test Naming

```
MethodName_Scenario_ExpectedResult
```

**Examples:**
- `GetCustomer_WithValidId_ReturnsCustomer`
- `GetCustomer_WithNullId_ThrowsArgumentNullException`
- `Calculate_WhenAmountIsNegative_ReturnsZero`

## Test Structure (AAA)

```csharp
[Fact]
public void MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var sut = new CustomerService(fakeDependency);
    
    // Act
    var result = sut.GetCustomer(customerId);
    
    // Assert
    Assert.NotNull(result);
}
```

## Mocking

```csharp
// FakeItEasy
var fakeRepo = A.Fake<ICustomerRepository>();
A.CallTo(() => fakeRepo.Find(customerId)).Returns(expectedCustomer);
```

## What to Test

✅ **Do test:**
- Business logic and calculations
- Validation rules
- Edge cases and error handling
- Public API contracts

❌ **Don't test:**
- Framework code (EF Core, ASP.NET)
- Private methods directly
- Simple DTOs/models without logic
- Third-party libraries

## Best Practices

- One assertion concept per test (multiple related asserts OK)
- Tests should be independent — no shared state
- Use descriptive variable names (`expectedCustomer`, `invalidId`)
- Prefer real objects over mocks when simple
- Keep tests fast — mock external dependencies