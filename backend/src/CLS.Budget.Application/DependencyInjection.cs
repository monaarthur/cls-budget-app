using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Accounts;
using CLS.Budget.Application.Accounts.Validators;
using CLS.Budget.Application.Auth;
using CLS.Budget.Application.Budgets;
using CLS.Budget.Application.CreditCards;
using CLS.Budget.Application.BudgetPaymentStatuses;
using CLS.Budget.Application.BudgetTemplates;
using CLS.Budget.Application.PaymentSources;
using CLS.Budget.Application.Payments;
using CLS.Budget.Application.Incomes;
using CLS.Budget.Application.IncomeSources;
using CLS.Budget.Application.PaySchedules;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CLS.Budget.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateAccountRequestValidator>();

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IBudgetIncomeService, IncomeService>();
        services.AddScoped<IIncomeSourceService, IncomeSourceService>();
        services.AddScoped<IBudgetPaymentStatusService, BudgetPaymentStatusService>();
        services.AddScoped<IPaymentSourceService, PaymentSourceService>();
        services.AddScoped<IPayFrequencyTypeService, PayFrequencyTypeService>();
        services.AddScoped<IPayScheduleService, PayScheduleService>();
        services.AddScoped<IBudgetTemplateService, BudgetTemplateService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<ICreditCardService, CreditCardService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
