// Tooltip content for form fields across the application

export const TOOLTIP_CONTENT = {
  // Currency tooltips
  currency: {
    general: "Select the currency for this item. All amounts will be displayed and calculated in the selected currency. For foreign currencies, exchange rates will be applied when calculating totals.",
    ZAR: "South African Rand - The primary currency for South African transactions",
    USD: "United States Dollar - Common for international investments and trading",
    EUR: "Euro - The official currency of the Eurozone countries",
    GBP: "British Pound Sterling - The official currency of the United Kingdom",
  },

  // Frequency tooltips
  frequency: {
    general: "How often this payment occurs. This affects how the amount is calculated for monthly and annual projections.",
    Monthly: "Payment occurs once every month (12 times per year)",
    Annual: "Payment occurs once per year. Amount will be divided by 12 for monthly calculations.",
    Weekly: "Payment occurs every week (52 times per year). Multiplied by ~4.33 for monthly calculations.",
    BiWeekly: "Payment occurs every two weeks (26 times per year). Multiplied by ~2.17 for monthly calculations.",
    Once: "A one-time payment that won't recur. Useful for single purchases or bonuses.",
  },

  // Expense category tooltips
  expenseCategory: {
    general: "Categorize your expense to better track spending patterns and generate insights.",
    Housing: "Rent, mortgage, property taxes, home insurance, maintenance, and repairs",
    Transport: "Car payments, fuel, public transit, ride-sharing, vehicle insurance and maintenance",
    Food: "Groceries, dining out, takeaway, coffee shops, and food delivery",
    Utilities: "Electricity, water, gas, internet, phone, and streaming services",
    Insurance: "Life, health, disability, and other personal insurance premiums",
    Healthcare: "Medical appointments, prescriptions, dental, vision, and wellness",
    Entertainment: "Movies, concerts, hobbies, sports, and recreational activities",
    Education: "Tuition, courses, books, certifications, and professional development",
    Savings: "Planned savings contributions (use Investments section for investment contributions)",
    Debt: "Credit card payments, personal loans, and other debt repayments",
    Other: "Miscellaneous expenses that don't fit other categories",
  },

  // Tax profile tooltips
  taxProfile: {
    selector: "Link this income to a tax profile to automatically calculate PAYE tax and UIF deductions. Tax calculations will use the brackets and rates from the selected profile.",
    none: "No tax will be calculated for this income. Use this for post-tax income or income from tax-free sources.",
    country: "The country whose tax rules this profile follows. This helps select appropriate default tax brackets.",
    taxYear: "The tax year these brackets apply to. Tax rules often change annually.",
    brackets: "Tax brackets define progressive tax rates. Each bracket specifies the income range and the tax rate applied to that portion.",
    uifRate: "Unemployment Insurance Fund rate. In South Africa, this is typically 1% of income, shared between employer and employee.",
    uifCap: "Maximum annual UIF contribution. Once reached, no further UIF deductions are made for the year.",
    vatRate: "Value Added Tax rate for VAT-registered entities. Currently 15% in South Africa.",
    rebates: "Tax rebates reduce your total tax liability. Primary is for under 65, Secondary is for 65-74, Tertiary is for 75+.",
  },

  // Investment category tooltips
  investmentCategory: {
    general: "Categorize your investment contribution to track allocation across different goals.",
    Retirement: "Long-term retirement savings (e.g., pension funds, retirement annuities, 401k)",
    "Emergency Fund": "Liquid savings for unexpected expenses (typically 3-6 months of expenses)",
    "Short-Term Savings": "Savings for goals within the next 1-3 years",
    "Long-Term Savings": "Savings for goals 3+ years away (excluding retirement)",
    "Debt Repayment": "Extra payments toward debt beyond minimum required payments",
    Investment: "General investment accounts (stocks, ETFs, unit trusts, etc.)",
    "Goal-based": "Savings earmarked for specific financial goals",
    Other: "Investment contributions that don't fit other categories",
  },

  // Amount type tooltips
  amountType: {
    general: "How the expense amount is determined.",
    Fixed: "A fixed amount that stays the same each period. Best for predictable expenses like rent or subscriptions.",
    Percentage: "Amount calculated as a percentage of another value (e.g., income). Useful for proportional expenses.",
    Formula: "Amount calculated using a custom formula. Advanced option for complex calculations.",
  },

  // Income specific tooltips
  income: {
    baseAmount: "The gross amount before any deductions. If linked to a tax profile, taxes will be calculated from this amount.",
    isPreTax: "If checked, the amount entered is before tax (gross). Taxes will be calculated and deducted. If unchecked, the amount is what you actually receive (net).",
    annualIncrease: "Expected yearly increase percentage. Used in financial projections and simulations to model salary growth.",
    employerName: "Optional. The source or employer for this income. Helps identify income in reports.",
  },

  // Goal specific tooltips
  goal: {
    priority: "Priority affects how monthly investment contributions are allocated across goals. Higher priority goals are funded first.",
    targetDate: "Optional target date for achieving this goal. Used to calculate required monthly savings.",
    timeToAcquire: "Estimated time to reach this goal based on your current monthly investment rate and the remaining amount needed.",
    targetAmount: "The total amount you want to save or accumulate for this financial goal.",
  },
  
  // Generic category tooltip (alias for expenseCategory.general)
  category: "Select a category to organize and track your expenses by type.",

  // General tooltips
  homeCurrency: "Your primary currency for displaying totals and net worth. Foreign currency accounts will be converted at current exchange rates.",
  dimensionWeights: "How much each life dimension contributes to your overall life score. Weights must total 100%.",
  inflationAdjusted: "If enabled, the expense amount will increase with inflation in financial projections.",
  isTaxDeductible: "If enabled, this expense can reduce taxable income in tax calculations.",
  targetAccount: "Optionally link this contribution to a tracked account. Helps visualize where your money is going.",
  sourceAccount: "Select the account from which this contribution will be debited. This is used in financial simulations and projections to accurately track cash flow.",
} as const;
