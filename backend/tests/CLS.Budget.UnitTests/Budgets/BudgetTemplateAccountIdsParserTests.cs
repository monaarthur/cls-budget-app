using CLS.Budget.Application.Budgets;
using FluentAssertions;

namespace CLS.Budget.UnitTests.Budgets;

public class BudgetTemplateAccountIdsParserTests
{
    [Fact]
    public void Parse_ReturnsIds_FromJsonArray()
    {
        var result = BudgetTemplateAccountIdsParser.Parse("[1, 2, 37]");

        result.Should().Equal(1, 2, 37);
    }

    [Fact]
    public void Parse_ReturnsIds_FromCommaSeparated()
    {
        var result = BudgetTemplateAccountIdsParser.Parse("1,5,10");

        result.Should().Equal(1, 5, 10);
    }

    [Fact]
    public void Parse_ReturnsEmpty_WhenNullOrWhitespace()
    {
        BudgetTemplateAccountIdsParser.Parse(null).Should().BeEmpty();
        BudgetTemplateAccountIdsParser.Parse("   ").Should().BeEmpty();
    }
}
