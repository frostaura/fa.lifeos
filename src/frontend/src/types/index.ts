// API Types
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}

// Dashboard Types
export interface DashboardData {
  lifeScore: number;
  lifeScoreTrend: number;
  netWorth: NetWorthData;
  dimensions: DimensionScore[];
  streaks: Streak[];
  todaysTasks: TaskItem[];
  projections: Projection[];
}

export interface NetWorthData {
  value: number;
  currency: string;
  change: number;
  changePercent: number;
  history: NetWorthDataPoint[];
}

export interface NetWorthDataPoint {
  date: string;
  value: number;
  accounts?: Array<{
    accountId: string;
    accountName: string;
    balance: number;
  }>;
}

// Dimension Types
export type DimensionId = 
  | 'health' 
  | 'mind' 
  | 'work' 
  | 'money' 
  | 'relationships' 
  | 'play' 
  | 'growth' 
  | 'community';

export interface Dimension {
  id: DimensionId;
  name: string;
  description: string;
  weight: number;
  color: string;
}

export interface DimensionScore {
  dimensionId: DimensionId;
  score: number;
  trend: number;
  activeMilestones: number;
}

// Streak Types
export interface Streak {
  id: string;
  name: string;
  habitId: string;
  currentDays: number;
  longestDays: number;
  lastCompletedAt: string;
}

// Task Types
export interface TaskItem {
  id: string;
  title: string;
  completed: boolean;
  dueDate?: string;
  dimensionId?: DimensionId;
}

// Projection Types
export interface Projection {
  id: string;
  label: string;
  targetDate: string;
  targetValue: number;
  probability: number;
}

// Finance Types
export type AccountType = 'bank' | 'investment' | 'crypto' | 'credit' | 'loan' | 'property' | 'other';

export interface Account {
  id: string;
  name: string;
  type: AccountType;
  balance: number;
  currency: string;
  change?: number;
  institution?: string;
  lastUpdated: string;
  isLiability?: boolean;
  interestRateAnnual?: number;
  monthlyInterest?: number;
  monthlyFee?: number;
}

export interface Transaction {
  id: string;
  accountId: string;
  amount: number;
  currency: string;
  description: string;
  category?: string;
  date: string;
  type: 'income' | 'expense' | 'transfer';
}

export interface FxRate {
  pair: string;
  rate: number;
  change: number;
  timestamp: string;
}

// Simulation Types
export interface Scenario {
  id: string;
  name: string;
  description?: string;
  startDate: string;
  endDate: string;
  createdAt: string;
  events: FutureEvent[];
  isActive: boolean;
}

export type EventType = 
  | 'income_change' 
  | 'expense_change' 
  | 'one_time_income' 
  | 'one_time_expense' 
  | 'asset_purchase' 
  | 'asset_sale' 
  | 'market_adjustment';

export interface FutureEvent {
  id: string;
  scenarioId: string;
  name: string;
  type: EventType;
  date: string;
  amount: number;
  currency: string;
  isRecurring: boolean;
  recurringFrequency?: 'monthly' | 'quarterly' | 'yearly';
}

export interface SimulationResult {
  scenarioId: string;
  projections: ProjectionDataPoint[];
  milestones: MilestoneResult[];
  summary: SimulationSummary;
}

export interface ProjectionAccountBalance {
  accountId: string;
  accountName: string;
  balance: number;
  periodIncome: number;
  periodExpenses: number;
}

export interface ProjectionDataPoint {
  date: string;
  netWorth: number;
  income: number;
  expenses: number;
  savings: number;
  accounts?: ProjectionAccountBalance[];
}

export interface MilestoneResult {
  label: string;
  targetValue: number;
  achievedDate: string | null;
  probability: number;
}

export interface SimulationSummary {
  finalNetWorth: number;
  totalIncome: number;
  totalExpenses: number;
  avgMonthlyGrowth: number;
}

// Tax Profile Types
export interface TaxBracket {
  min: number;
  max: number | null;
  rate: number;
  baseTax: number;
}

export interface TaxRebates {
  primary?: number;
  secondary?: number;
  tertiary?: number;
}

export interface TaxProfile {
  id: string;
  name: string;
  taxYear: number;
  countryCode: string;
  brackets: TaxBracket[];
  uifRate?: number;
  uifCap?: number;
  vatRate?: number;
  isVatRegistered: boolean;
  taxRebates?: TaxRebates;
  isActive: boolean;
}

export interface CreateTaxProfileRequest {
  name: string;
  taxYear: number;
  countryCode: string;
  brackets: TaxBracket[];
  uifRate?: number;
  uifCap?: number;
  vatRate?: number;
  isVatRegistered?: boolean;
  taxRebates?: TaxRebates;
}

export interface UpdateTaxProfileRequest {
  name?: string;
  brackets?: TaxBracket[];
  uifRate?: number;
  uifCap?: number;
  vatRate?: number;
  isVatRegistered?: boolean;
  taxRebates?: TaxRebates;
  isActive?: boolean;
}

// Income Source Types
export type PaymentFrequency = 'Monthly' | 'Annually' | 'Weekly' | 'Biweekly' | 'Once';

export interface IncomeSource {
  id: string;
  name: string;
  baseAmount: number;
  currency: string;
  isPreTax: boolean;
  taxProfileId?: string;
  paymentFrequency: PaymentFrequency;
  nextPaymentDate?: string;
  annualIncreaseRate?: number;
  employerName?: string;
  notes?: string;
  isActive: boolean;
  targetAccountId?: string;
  targetAccountName?: string;
}

export interface CreateIncomeSourceRequest {
  name: string;
  currency: string;
  baseAmount: number;
  isPreTax?: boolean;
  taxProfileId?: string;
  paymentFrequency: PaymentFrequency;
  nextPaymentDate?: string;
  annualIncreaseRate?: number;
  employerName?: string;
  notes?: string;
  targetAccountId?: string; // Optional: where the income is deposited (defaults to first bank account)
}

// Expense Definition Types
export type AmountType = 'Fixed' | 'Percentage' | 'Formula';

// End condition type for recurring expenses
export type EndConditionType = 'None' | 'UntilAccountSettled' | 'UntilDate' | 'UntilAmount';

export interface ExpenseDefinition {
  id: string;
  name: string;
  currency: string;
  amountType: AmountType;
  amountValue?: number;
  amountFormula?: string;
  frequency: PaymentFrequency;
  startDate?: string; // Start date for scheduling (especially for once-off expenses)
  category: string;
  isTaxDeductible: boolean;
  linkedAccountId?: string;
  linkedAccountName?: string;
  inflationAdjusted: boolean;
  isActive: boolean;
  endConditionType: EndConditionType;
  endConditionAccountId?: string;
  endDate?: string;
  endAmountThreshold?: number;
}

export interface CreateExpenseDefinitionRequest {
  name: string;
  currency: string;
  amountType: AmountType;
  amountValue?: number;
  amountFormula?: string;
  frequency: PaymentFrequency;
  startDate?: string; // Start date for scheduling (especially for once-off expenses)
  category: string;
  isTaxDeductible?: boolean;
  linkedAccountId?: string; // Optional: the account this expense is debited from
  inflationAdjusted?: boolean;
  endConditionType?: EndConditionType;
  endConditionAccountId?: string;
  endDate?: string;
  endAmountThreshold?: number;
}

export interface UpdateExpenseDefinitionRequest {
  name?: string;
  amountValue?: number;
  amountFormula?: string;
  frequency?: PaymentFrequency;
  startDate?: string;
  category?: string;
  isTaxDeductible?: boolean;
  linkedAccountId?: string;
  inflationAdjusted?: boolean;
  isActive?: boolean;
  endConditionType?: EndConditionType;
  endConditionAccountId?: string;
  endDate?: string;
  endAmountThreshold?: number;
}

// Currency
export type Currency = 'ZAR' | 'USD' | 'EUR' | 'GBP' | 'BTC';

export const CURRENCIES: { value: Currency; label: string; symbol: string }[] = [
  { value: 'ZAR', label: 'South African Rand', symbol: 'R' },
  { value: 'USD', label: 'US Dollar', symbol: '$' },
  { value: 'EUR', label: 'Euro', symbol: '€' },
  { value: 'GBP', label: 'British Pound', symbol: '£' },
  { value: 'BTC', label: 'Bitcoin', symbol: '₿' },
];

// Investment Contribution Types
export interface InvestmentContribution {
  id: string;
  name: string;
  currency: string;
  amount: number;
  frequency: PaymentFrequency;
  targetAccountId?: string;
  targetAccountName?: string;
  sourceAccountId?: string;
  sourceAccountName?: string;
  category?: string;
  annualIncreaseRate?: number;
  notes?: string;
  startDate?: string; // Required for once-off contributions
  isActive: boolean;
  endConditionType: EndConditionType;
  endConditionAccountId?: string;
  endConditionAccountName?: string;
  endDate?: string;
  endAmountThreshold?: number;
  createdAt: string;
}

export interface InvestmentContributionSummary {
  totalMonthlyContributions: number;
  byCategory: Record<string, number>;
}

export interface InvestmentContributionListResponse {
  sources: InvestmentContribution[];
  summary: InvestmentContributionSummary;
}

export interface CreateInvestmentContributionRequest {
  name: string;
  currency: string;
  amount: number;
  frequency: PaymentFrequency;
  targetAccountId?: string; // Optional: where the investment goes (defaults to first investment account)
  sourceAccountId?: string; // Optional: where the money comes from (defaults to first bank account)
  category?: string;
  annualIncreaseRate?: number;
  notes?: string;
  startDate?: string; // Required for once-off contributions
  endConditionType?: EndConditionType;
  endConditionAccountId?: string;
  endDate?: string;
  endAmountThreshold?: number;
}

export interface UpdateInvestmentContributionRequest {
  name?: string;
  amount?: number;
  frequency?: PaymentFrequency;
  targetAccountId?: string;
  sourceAccountId?: string;
  category?: string;
  annualIncreaseRate?: number;
  notes?: string;
  startDate?: string;
  isActive?: boolean;
  endConditionType?: EndConditionType;
  endConditionAccountId?: string;
  endDate?: string;
  endAmountThreshold?: number;
}

// Financial Goals Types
export interface FinancialGoal {
  id: string;
  name: string;
  targetAmount: number;
  currentAmount: number;
  priority: number;
  targetDate?: string;
  category?: string;
  iconName?: string;
  currency: string;
  notes?: string;
  isActive: boolean;
  createdAt: string;
  remainingAmount: number;
  progressPercent: number;
  monthsToAcquire?: number;
}

export interface FinancialGoalSummary {
  totalTargetAmount: number;
  totalCurrentAmount: number;
  totalRemainingAmount: number;
  overallProgressPercent: number;
  monthlyInvestmentRate: number;
  estimatedTotalMonths?: number;
}

export interface FinancialGoalListResponse {
  goals: FinancialGoal[];
  summary: FinancialGoalSummary;
}

export interface CreateFinancialGoalRequest {
  name: string;
  targetAmount: number;
  currentAmount?: number;
  priority?: number;
  targetDate?: string;
  category?: string;
  iconName?: string;
  currency?: string;
  notes?: string;
}

export interface UpdateFinancialGoalRequest {
  name?: string;
  targetAmount?: number;
  currentAmount?: number;
  priority?: number;
  targetDate?: string;
  category?: string;
  iconName?: string;
  notes?: string;
  isActive?: boolean;
}

// Dimension API Response Types (matching backend DTOs)
export interface DimensionAttributes {
  code: string;
  name: string;
  description: string | null;
  icon: string | null;
  weight: number;
  defaultWeight: number;
  sortOrder: number;
  isActive: boolean;
  currentScore: number;
}

export interface DimensionItemResponse {
  id: string;
  type: 'dimension';
  attributes: DimensionAttributes;
}

export interface DimensionListMeta {
  totalWeight: number;
}

export interface DimensionListResponse {
  data: DimensionItemResponse[];
  meta: DimensionListMeta;
}

export interface MilestoneReference {
  id: string;
  title: string;
  status: string;
}

export interface TaskReference {
  id: string;
  title: string;
  taskType: string;
}

export interface DimensionRelationships {
  milestones: MilestoneReference[];
  activeTasks: TaskReference[];
}

export interface DimensionDetailData {
  id: string;
  type: 'dimension';
  attributes: DimensionAttributes;
  relationships?: DimensionRelationships;
}

export interface DimensionDetailResponse {
  data: DimensionDetailData;
}

export interface UpdateDimensionWeightRequest {
  weight: number;
  autoRebalance?: boolean;
}
