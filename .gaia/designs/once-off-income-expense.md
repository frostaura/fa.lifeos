# Once-Off Future Income/Expense Feature Design

## Overview

This design enables users to create once-off (non-recurring) future income and expense items that integrate with the existing financial projection system. The approach is minimal, leveraging existing infrastructure.

## Design Decision

**Approach**: Add `Once` to the `PaymentFrequency` enum and leverage existing fields (`NextPaymentDate`) as the scheduled date.

**Rationale**:
- Minimal code changes
- Consistent with existing patterns
- Frontend already has `'Once'` in the PaymentFrequency type
- `NextPaymentDate` on IncomeSource already exists and can serve as the scheduled date
- `EndDate` on ExpenseDefinition can indicate when it occurs (or use EndConditionType.UntilDate with EndDate)

## Backend Changes

### 1. PaymentFrequency Enum Update

**File**: `src/backend/LifeOS.Domain/Enums/PaymentFrequency.cs`

```csharp
namespace LifeOS.Domain.Enums;

public enum PaymentFrequency
{
    Weekly,
    Biweekly,
    Monthly,
    Quarterly,
    Annually,
    Once  // NEW: Once-off payment (single occurrence)
}
```

### 2. SimulationEngine Changes

**File**: `src/backend/LifeOS.Application/Services/SimulationEngine.cs`

#### 2.1 ShouldApplyRecurringItem Method Update

The method needs to handle `Once` frequency - it should only apply when the simulation date matches (or has passed) the scheduled date, and only once.

```csharp
private bool ShouldApplyRecurringItem(PaymentFrequency frequency, DateOnly currentDate, DateOnly? startDate)
{
    return frequency switch
    {
        PaymentFrequency.Weekly => true,
        PaymentFrequency.Biweekly => true,
        PaymentFrequency.Monthly => true,
        PaymentFrequency.Quarterly => currentDate.Month % 3 == (startDate?.Month ?? 1) % 3,
        PaymentFrequency.Annually => currentDate.Month == (startDate?.Month ?? 1),
        PaymentFrequency.Once => startDate.HasValue && currentDate.Year == startDate.Value.Year && currentDate.Month == startDate.Value.Month,
        _ => true
    };
}
```

#### 2.2 Track Applied Once-Off Items

Add tracking sets for once-off income and expense items (similar to `appliedOneTimeEvents`):

```csharp
// Track applied one-time income/expenses
var appliedOnceOffIncome = new HashSet<Guid>();
var appliedOnceOffExpenses = new HashSet<Guid>();
```

#### 2.3 Income Application Logic

```csharp
// In the income sources loop
foreach (var income in incomeSources)
{
    // Skip once-off if already applied
    if (income.PaymentFrequency == PaymentFrequency.Once && appliedOnceOffIncome.Contains(income.Id))
        continue;

    if (!ShouldApplyRecurringItem(income.PaymentFrequency, currentDate, income.NextPaymentDate))
        continue;

    // ... existing logic ...

    // Mark once-off as applied
    if (income.PaymentFrequency == PaymentFrequency.Once)
        appliedOnceOffIncome.Add(income.Id);
}
```

#### 2.4 Expense Application Logic

```csharp
// In the expense definitions loop
foreach (var expense in expenseDefinitions)
{
    // Skip once-off if already applied
    if (expense.Frequency == PaymentFrequency.Once && appliedOnceOffExpenses.Contains(expense.Id))
        continue;

    // For once-off expenses, use EndDate as the scheduled date
    var scheduledDate = expense.Frequency == PaymentFrequency.Once ? expense.EndDate : null;
    if (!ShouldApplyRecurringItem(expense.Frequency, currentDate, scheduledDate))
        continue;

    // ... existing logic ...

    // Mark once-off as applied
    if (expense.Frequency == PaymentFrequency.Once)
        appliedOnceOffExpenses.Add(expense.Id);
}
```

### 3. ConvertToMonthly Helper Update

**File**: `src/backend/LifeOS.Application/Services/SimulationEngine.cs`

For once-off items, converting to monthly doesn't make sense. Return 0 to exclude from monthly totals:

```csharp
private decimal ConvertToMonthly(decimal amount, PaymentFrequency frequency)
{
    return frequency switch
    {
        PaymentFrequency.Weekly => amount * 52 / 12,
        PaymentFrequency.Biweekly => amount * 26 / 12,
        PaymentFrequency.Monthly => amount,
        PaymentFrequency.Quarterly => amount / 3,
        PaymentFrequency.Annually => amount / 12,
        PaymentFrequency.Once => 0, // Once-off doesn't contribute to monthly totals
        _ => amount
    };
}
```

### 4. Tax Calculation Update (Optional Consideration)

For once-off income that is pre-tax, the tax calculation uses annual conversion. Once-off income should be treated as a lump sum:

```csharp
// In CalculateNetIncome
var annualGross = frequency switch
{
    PaymentFrequency.Weekly => grossAmount * 52,
    PaymentFrequency.Biweekly => grossAmount * 26,
    PaymentFrequency.Monthly => grossAmount * 12,
    PaymentFrequency.Quarterly => grossAmount * 4,
    PaymentFrequency.Annually => grossAmount,
    PaymentFrequency.Once => grossAmount, // Once-off is just the amount
    _ => grossAmount * 12
};
```

### 5. Validation Rules

#### 5.1 Income Source Validation

When `PaymentFrequency == Once`:
- `NextPaymentDate` becomes required (this is the scheduled date)
- `AnnualIncreaseRate` should be ignored (N/A for once-off)

#### 5.2 Expense Definition Validation

When `Frequency == Once`:
- `EndDate` becomes required (this is the scheduled date)
- `EndConditionType` should be set to `UntilDate` or `None`
- `InflationAdjusted` should be `false` (N/A for once-off)

## Frontend Changes

### 1. Types Already Updated

**File**: `src/frontend/src/types/index.ts`

The frontend already has `'Once'` in the `PaymentFrequency` type:
```typescript
export type PaymentFrequency = 'Monthly' | 'Annual' | 'Weekly' | 'BiWeekly' | 'Once';
```

### 2. Frequency Options Update

**File**: `src/frontend/src/pages/Settings.tsx`

Update the `frequencyOptions` array to include once-off:

```typescript
const frequencyOptions = [
  { value: 'Monthly', label: 'Monthly' },
  { value: 'Annual', label: 'Annually' },
  { value: 'Weekly', label: 'Weekly' },
  { value: 'BiWeekly', label: 'Bi-Weekly' },
  { value: 'Once', label: 'Once-Off' }, // NEW
];
```

### 3. Conditional Scheduled Date Field

For both income and expense forms, show a "Scheduled Date" field when frequency is `Once`:

```tsx
{/* For Income Form - show NextPaymentDate as required when Once */}
{incomeForm.paymentFrequency === 'Once' && (
  <div>
    <label className="block text-sm font-medium text-text-secondary mb-1">
      <span className="inline-flex items-center gap-1.5">
        Scheduled Date *
        <InfoTooltip content="The date when this one-time income will be received" />
      </span>
    </label>
    <input
      type="date"
      value={incomeForm.nextPaymentDate || ''}
      onChange={(e) => setIncomeForm(prev => ({ ...prev, nextPaymentDate: e.target.value }))}
      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
      required
    />
  </div>
)}

{/* For Expense Form - show EndDate as required when Once */}
{expenseForm.frequency === 'Once' && (
  <div>
    <label className="block text-sm font-medium text-text-secondary mb-1">
      <span className="inline-flex items-center gap-1.5">
        Scheduled Date *
        <InfoTooltip content="The date when this one-time expense will occur" />
      </span>
    </label>
    <input
      type="date"
      value={expenseForm.endDate || ''}
      onChange={(e) => setExpenseForm(prev => ({ 
        ...prev, 
        endDate: e.target.value,
        endConditionType: 'UntilDate' // Auto-set for once-off
      }))}
      className="w-full bg-bg-tertiary border border-glass-border rounded-lg px-3 py-2 text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple [color-scheme:dark]"
      required
    />
  </div>
)}
```

### 4. Visual Differentiation

Add a badge/indicator to distinguish once-off items in the list:

```tsx
{/* Once-off badge in income list */}
{income.paymentFrequency === 'Once' && (
  <span className="px-2 py-0.5 bg-amber-500/20 text-amber-400 text-xs rounded-full ml-2">
    One-Time
  </span>
)}

{/* Different row styling for once-off items */}
<div className={cn(
  "p-4 rounded-lg border transition-colors",
  income.paymentFrequency === 'Once'
    ? "bg-amber-900/10 border-amber-500/30"
    : income.isActive
      ? "bg-bg-tertiary border-glass-border"
      : "bg-bg-tertiary/50 border-glass-border/50 opacity-60"
)}>
```

### 5. Form Validation

Disable/hide irrelevant fields when once-off is selected:

```tsx
{/* Hide annual increase for once-off income */}
{incomeForm.paymentFrequency !== 'Once' && (
  <div>
    <label>Annual Increase %</label>
    {/* ... */}
  </div>
)}

{/* Hide inflation adjustment for once-off expense */}
{expenseForm.frequency !== 'Once' && (
  <div>
    <label className="flex items-center gap-2">
      <input type="checkbox" checked={expenseForm.inflationAdjusted} {...} />
      Adjust for inflation
    </label>
  </div>
)}

{/* Hide end condition options for once-off expense (auto-set to UntilDate) */}
{expenseForm.frequency !== 'Once' && (
  <div>
    <label>End Condition</label>
    <select>{/* ... */}</select>
  </div>
)}
```

### 6. Summary Calculations

Update the summary display to show once-off items separately or exclude from monthly totals:

```tsx
// Calculate once-off totals
const upcomingOnceOffIncome = incomeSources
  ?.filter(i => i.isActive && i.paymentFrequency === 'Once')
  .reduce((sum, i) => sum + i.baseAmount, 0) || 0;

const upcomingOnceOffExpenses = expenseDefinitions
  ?.filter(e => e.isActive && e.frequency === 'Once')
  .reduce((sum, e) => sum + (e.amountValue || 0), 0) || 0;

// Show in UI
{upcomingOnceOffIncome > 0 && (
  <div className="text-sm text-text-secondary">
    Scheduled one-time income: <span className="text-green-400">{formatCurrency(upcomingOnceOffIncome)}</span>
  </div>
)}
```

## Database Considerations

### Migration Not Required

The `PaymentFrequency` enum is stored as an integer in the database. Adding `Once = 5` at the end of the enum won't break existing data. However, ensure EF Core generates the correct migration if enum order matters:

```csharp
public enum PaymentFrequency
{
    Weekly = 0,
    Biweekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Annually = 4,
    Once = 5  // NEW - explicit value ensures no conflicts
}
```

## Use Cases

### Once-Off Income Examples
- Year-end bonus (scheduled for December)
- Tax refund (scheduled when expected)
- Someone paying back a loan
- Inheritance or gift
- One-time freelance payment

### Once-Off Expense Examples
- Planned large purchase (car, appliance)
- Annual insurance premium (if not recurring)
- Wedding expense
- Vacation budget
- Home renovation deposit

## Auto-Deactivation (Future Enhancement)

For now, once-off items remain in the list after their date passes. A future enhancement could:
1. Mark them as `IsActive = false` after the scheduled date
2. Move them to a "completed" or "history" view
3. Add a "Mark as Completed" manual action

This is not essential for MVP and can be deferred.

## Summary of Changes

| Component | File | Change Type |
|-----------|------|-------------|
| Domain Enum | `PaymentFrequency.cs` | Add `Once` value |
| Simulation Engine | `SimulationEngine.cs` | Handle Once in `ShouldApplyRecurringItem`, track applied once-off items, update `ConvertToMonthly` |
| Frontend Types | `types/index.ts` | Already has `Once` ✓ |
| Frontend Forms | `Settings.tsx` | Add Once to frequency options, conditional scheduled date field, visual badges |
| Validation | Command handlers | Require scheduled date for Once frequency |

## Implementation Priority

1. **Backend enum update** - Required first
2. **SimulationEngine changes** - Core logic for projections
3. **Frontend frequency options** - Enable UI selection
4. **Frontend conditional fields** - Scheduled date input
5. **Frontend visual differentiation** - Badges and styling
6. **Validation** - Ensure scheduled date required

---

**Design Status**: Complete  
**Ready For**: @Builder implementation
