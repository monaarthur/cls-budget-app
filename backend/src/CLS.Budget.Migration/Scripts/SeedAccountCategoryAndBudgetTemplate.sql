-- Reference seed: AccountCategory and BudgetTemplate
-- Run only if you are not applying EF migrations that include HasData seeding.
-- PostgreSQL (table names match EF Core defaults)

INSERT INTO "AccountCategories" ("AccountCategoryId", "Name", "Description")
VALUES
    (1, 'Credit Card', 'Revolving credit accounts'),
    (2, 'Loan', 'Installment loans'),
    (3, 'Mortgage', 'Home mortgage accounts'),
    (4, 'Utility', 'Utility and service bills'),
    (5, 'Subscription', 'Recurring subscriptions'),
    (6, 'Savings', 'Savings accounts'),
    (7, 'Checking', 'Checking accounts')
ON CONFLICT ("AccountCategoryId") DO NOTHING;

INSERT INTO "BudgetTemplates" ("BudgetTemplateId", "Name", "Description", "AccountIds")
VALUES
    (1, 'Monthly Household Budget', 'Default template for monthly budget planning', NULL)
ON CONFLICT ("BudgetTemplateId") DO NOTHING;

SELECT setval(pg_get_serial_sequence('"AccountCategories"', 'AccountCategoryId'),
              (SELECT COALESCE(MAX("AccountCategoryId"), 1) FROM "AccountCategories"));
SELECT setval(pg_get_serial_sequence('"BudgetTemplates"', 'BudgetTemplateId'),
              (SELECT COALESCE(MAX("BudgetTemplateId"), 1) FROM "BudgetTemplates"));
