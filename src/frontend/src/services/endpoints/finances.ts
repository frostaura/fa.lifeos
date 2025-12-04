import { apiSlice } from '@store/api/apiSlice';
import type { 
  Account, 
  AccountType,
  Transaction,
  FxRate,
  NetWorthDataPoint,
  TaxProfile,
  CreateTaxProfileRequest,
  UpdateTaxProfileRequest,
  IncomeSource,
  ExpenseDefinition,
  CreateIncomeSourceRequest,
  CreateExpenseDefinitionRequest,
  InvestmentContribution,
  InvestmentContributionListResponse,
  CreateInvestmentContributionRequest,
  UpdateInvestmentContributionRequest,
  FinancialGoal,
  FinancialGoalListResponse,
  CreateFinancialGoalRequest,
  UpdateFinancialGoalRequest,
} from '@/types';

// API Response type for tax profiles
interface TaxProfileResponse {
  data: Array<{
    id: string;
    type: string;
    attributes: {
      name: string;
      taxYear: number;
      countryCode: string;
      brackets: TaxProfile['brackets'];
      uifRate?: number;
      uifCap?: number;
      vatRate?: number;
      isVatRegistered: boolean;
      taxRebates?: TaxProfile['taxRebates'];
      isActive: boolean;
    };
  }>;
}

// API Response types for income sources
interface IncomeSourceResponse {
  data: Array<{
    id: string;
    type: string;
    attributes: {
      name: string;
      currency: string;
      baseAmount: number;
      isPreTax: boolean;
      taxProfileId?: string;
      paymentFrequency: string;
      nextPaymentDate?: string;
      annualIncreaseRate?: number;
      employerName?: string;
      notes?: string;
      isActive: boolean;
      targetAccountId?: string;
      targetAccountName?: string;
    };
  }>;
  meta?: {
    totalMonthlyGross: number;
    totalMonthlyNet: number;
    totalMonthlyTax: number;
    totalMonthlyUif: number;
  };
}

// API Response types for expense definitions
interface ExpenseDefinitionItem {
  id: string;
  type: string;
  attributes: {
    name: string;
    currency: string;
    amountType: string;
    amountValue?: number;
    amountFormula?: string;
    frequency: string;
    startDate?: string;
    category: string;
    isTaxDeductible: boolean;
    linkedAccountId?: string;
    linkedAccountName?: string;
    inflationAdjusted: boolean;
    isActive: boolean;
    endConditionType: string;
    endConditionAccountId?: string;
    endDate?: string;
    endAmountThreshold?: number;
  };
}

interface ExpenseDefinitionResponse {
  data: ExpenseDefinitionItem[];
  meta?: {
    totalMonthly: number;
    byCategory: Record<string, number>;
  };
}

// API Response type for accounts
interface AccountsListResponse {
  data: Account[];
  meta?: {
    page: number;
    perPage: number;
    total: number;
    totalPages: number;
    totalAssets: number;
    totalLiabilities: number;
    netWorth: number;
    totalMonthlyInterest: number;
    totalMonthlyFees: number;
    homeCurrency: string;
  };
}

export const financesApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    // Accounts
    getAccounts: builder.query<AccountsListResponse, void>({
      query: () => '/api/accounts',
      transformResponse: (response: { data?: Array<{ id: string; type: string; attributes: Record<string, unknown> }>; meta?: Record<string, unknown> }) => {
        // Transform API response to Account[] format with meta
        const accounts: Account[] = response?.data?.map(item => ({
          id: item.id,
          name: (item.attributes?.name as string) || '',
          type: ((item.attributes?.accountType as string) || 'bank') as AccountType,
          balance: (item.attributes?.currentBalance as number) || 0,
          currency: (item.attributes?.currency as string) || 'ZAR',
          lastUpdated: (item.attributes?.balanceUpdatedAt as string) || new Date().toISOString(),
          isLiability: (item.attributes?.isLiability as boolean) || false,
          institution: item.attributes?.institution as string | undefined,
          interestRateAnnual: item.attributes?.interestRateAnnual as number | undefined,
          monthlyInterest: item.attributes?.monthlyInterest as number | undefined,
          monthlyFee: item.attributes?.monthlyFee as number | undefined,
        })) || [];
        
        return {
          data: accounts,
          meta: response?.meta ? {
            page: (response.meta.page as number) || 1,
            perPage: (response.meta.perPage as number) || 20,
            total: (response.meta.total as number) || 0,
            totalPages: (response.meta.totalPages as number) || 0,
            totalAssets: (response.meta.totalAssets as number) || 0,
            totalLiabilities: (response.meta.totalLiabilities as number) || 0,
            netWorth: (response.meta.netWorth as number) || 0,
            totalMonthlyInterest: (response.meta.totalMonthlyInterest as number) || 0,
            totalMonthlyFees: (response.meta.totalMonthlyFees as number) || 0,
            homeCurrency: (response.meta.homeCurrency as string) || 'ZAR',
          } : undefined,
        };
      },
      providesTags: ['Accounts'],
    }),
    
    getAccount: builder.query<Account, string>({
      query: (id) => `/api/accounts/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Accounts', id }],
    }),
    
    getAccountHistory: builder.query<NetWorthDataPoint[], { id: string; period?: string }>({
      query: ({ id, period = '1Y' }) => `/api/accounts/${id}/history?period=${period}`,
      providesTags: (_result, _error, { id }) => [{ type: 'Accounts', id }],
    }),
    
    createAccount: builder.mutation<Account, Omit<Account, 'id' | 'lastUpdated'>>({
      query: (body) => ({
        url: '/api/accounts',
        method: 'POST',
        body,
      }),
      invalidatesTags: ['Accounts', 'Dashboard', 'InvestmentContributions', 'ExpenseDefinitions', 'Scenarios'],
    }),
    
    updateAccount: builder.mutation<Account, Partial<Account> & { id: string }>({
      query: ({ id, ...body }) => ({
        url: `/api/accounts/${id}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: ['Accounts', 'Dashboard', 'InvestmentContributions', 'ExpenseDefinitions', 'Scenarios'],
    }),
    
    deleteAccount: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/accounts/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Accounts', 'Dashboard', 'InvestmentContributions', 'ExpenseDefinitions', 'Scenarios'],
    }),
    
    // Transactions
    getTransactions: builder.query<Transaction[], { accountId?: string; limit?: number }>({
      query: ({ accountId, limit = 50 }) => ({
        url: '/api/transactions',
        params: { accountId, limit },
      }),
      providesTags: ['Transactions'],
    }),
    
    getAccountTransactions: builder.query<Transaction[], { accountId: string; page?: number; limit?: number }>({
      query: ({ accountId, page = 1, limit = 20 }) => 
        `/api/accounts/${accountId}/transactions?page=${page}&limit=${limit}`,
      providesTags: (_result, _error, { accountId }) => [
        { type: 'Transactions', id: accountId },
        'Transactions',
      ],
    }),
    
    createTransaction: builder.mutation<Transaction, Omit<Transaction, 'id'>>({
      query: (body) => ({
        url: '/api/transactions',
        method: 'POST',
        body,
      }),
      invalidatesTags: ['Transactions', 'Accounts', 'Dashboard'],
    }),
    
    deleteTransaction: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/transactions/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Transactions', 'Accounts', 'Dashboard'],
    }),
    
    // FX Rates
    getFxRates: builder.query<FxRate[], void>({
      query: () => '/api/fx-rates',
      providesTags: ['Dashboard'],
    }),
    
    // Net Worth History
    getNetWorthHistory: builder.query<NetWorthDataPoint[], { currency?: string; period?: string }>({
      query: ({ currency = 'ZAR', period = '1Y' }) => 
        `/api/finances/net-worth/history?currency=${currency}&period=${period}`,
      providesTags: ['Dashboard'],
    }),

    // Tax Profiles
    getTaxProfiles: builder.query<TaxProfile[], void>({
      query: () => '/api/tax-profiles',
      transformResponse: (response: TaxProfileResponse) => {
        return response.data.map(item => ({
          id: item.id,
          name: item.attributes.name,
          taxYear: item.attributes.taxYear,
          countryCode: item.attributes.countryCode,
          brackets: item.attributes.brackets || [],
          uifRate: item.attributes.uifRate,
          uifCap: item.attributes.uifCap,
          vatRate: item.attributes.vatRate,
          isVatRegistered: item.attributes.isVatRegistered,
          taxRebates: item.attributes.taxRebates,
          isActive: item.attributes.isActive,
        }));
      },
      providesTags: ['TaxProfiles'],
    }),

    createTaxProfile: builder.mutation<TaxProfile, CreateTaxProfileRequest>({
      query: (body) => ({
        url: '/api/tax-profiles',
        method: 'POST',
        body,
      }),
      invalidatesTags: ['TaxProfiles'],
    }),

    updateTaxProfile: builder.mutation<void, { id: string } & UpdateTaxProfileRequest>({
      query: ({ id, ...body }) => ({
        url: `/api/tax-profiles/${id}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: ['TaxProfiles'],
    }),

    deleteTaxProfile: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/tax-profiles/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['TaxProfiles'],
    }),

    // Income Sources
    getIncomeSources: builder.query<IncomeSource[], void>({
      query: () => '/api/income-sources',
      transformResponse: (response: IncomeSourceResponse) => {
        if (!response?.data) return [];
        return response.data.map(item => ({
          id: item.id,
          name: item.attributes.name,
          baseAmount: item.attributes.baseAmount,
          currency: item.attributes.currency,
          isPreTax: item.attributes.isPreTax,
          taxProfileId: item.attributes.taxProfileId,
          paymentFrequency: item.attributes.paymentFrequency as IncomeSource['paymentFrequency'],
          nextPaymentDate: item.attributes.nextPaymentDate,
          annualIncreaseRate: item.attributes.annualIncreaseRate,
          employerName: item.attributes.employerName,
          notes: item.attributes.notes,
          isActive: item.attributes.isActive,
          targetAccountId: item.attributes.targetAccountId,
          targetAccountName: item.attributes.targetAccountName,
        }));
      },
      providesTags: ['IncomeSources'],
    }),

    getIncomeSourcesWithSummary: builder.query<{
      sources: IncomeSource[];
      summary: {
        totalMonthlyGross: number;
        totalMonthlyNet: number;
        totalMonthlyTax: number;
        totalMonthlyUif: number;
      };
    }, void>({
      query: () => '/api/income-sources',
      transformResponse: (response: IncomeSourceResponse) => {
        if (!response?.data) return { 
          sources: [], 
          summary: { totalMonthlyGross: 0, totalMonthlyNet: 0, totalMonthlyTax: 0, totalMonthlyUif: 0 } 
        };
        return {
          sources: response.data.map(item => ({
            id: item.id,
            name: item.attributes.name,
            baseAmount: item.attributes.baseAmount,
            currency: item.attributes.currency,
            isPreTax: item.attributes.isPreTax,
            taxProfileId: item.attributes.taxProfileId,
            paymentFrequency: item.attributes.paymentFrequency as IncomeSource['paymentFrequency'],
            nextPaymentDate: item.attributes.nextPaymentDate,
            annualIncreaseRate: item.attributes.annualIncreaseRate,
            employerName: item.attributes.employerName,
            notes: item.attributes.notes,
            isActive: item.attributes.isActive,
            targetAccountId: item.attributes.targetAccountId,
            targetAccountName: item.attributes.targetAccountName,
          })),
          summary: {
            totalMonthlyGross: response.meta?.totalMonthlyGross || 0,
            totalMonthlyNet: response.meta?.totalMonthlyNet || 0,
            totalMonthlyTax: response.meta?.totalMonthlyTax || 0,
            totalMonthlyUif: response.meta?.totalMonthlyUif || 0,
          }
        };
      },
      providesTags: ['IncomeSources'],
    }),

    createIncomeSource: builder.mutation<IncomeSource, CreateIncomeSourceRequest>({
      query: (body) => ({
        url: '/api/income-sources',
        method: 'POST',
        body,
      }),
      invalidatesTags: ['IncomeSources', 'Dashboard', 'Scenarios'],
    }),

    updateIncomeSource: builder.mutation<void, Partial<IncomeSource> & { id: string; clearTaxProfile?: boolean }>({
      query: ({ id, ...body }) => ({
        url: `/api/income-sources/${id}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: ['IncomeSources', 'Dashboard', 'Scenarios'],
    }),

    deleteIncomeSource: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/income-sources/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['IncomeSources', 'Dashboard', 'Scenarios'],
    }),

    // Expense Definitions
    getExpenseDefinitions: builder.query<ExpenseDefinition[], void>({
      query: () => '/api/expense-definitions',
      transformResponse: (response: ExpenseDefinitionResponse) => {
        if (!response?.data) return [];
        return response.data.map(item => ({
          id: item.id,
          name: item.attributes.name,
          currency: item.attributes.currency,
          amountType: item.attributes.amountType as ExpenseDefinition['amountType'],
          amountValue: item.attributes.amountValue,
          amountFormula: item.attributes.amountFormula,
          frequency: item.attributes.frequency as ExpenseDefinition['frequency'],
          startDate: item.attributes.startDate,
          category: item.attributes.category,
          isTaxDeductible: item.attributes.isTaxDeductible,
          linkedAccountId: item.attributes.linkedAccountId,
          linkedAccountName: item.attributes.linkedAccountName,
          inflationAdjusted: item.attributes.inflationAdjusted,
          isActive: item.attributes.isActive,
          endConditionType: (item.attributes.endConditionType || 'none') as ExpenseDefinition['endConditionType'],
          endConditionAccountId: item.attributes.endConditionAccountId,
          endDate: item.attributes.endDate,
          endAmountThreshold: item.attributes.endAmountThreshold,
        }));
      },
      providesTags: ['ExpenseDefinitions'],
    }),

    createExpenseDefinition: builder.mutation<ExpenseDefinition, CreateExpenseDefinitionRequest>({
      query: (body) => ({
        url: '/api/expense-definitions',
        method: 'POST',
        body,
      }),
      transformResponse: (response: { data: ExpenseDefinitionItem }) => {
        const item = response.data;
        return {
          id: item.id,
          name: item.attributes.name,
          currency: item.attributes.currency,
          amountType: item.attributes.amountType as ExpenseDefinition['amountType'],
          amountValue: item.attributes.amountValue,
          amountFormula: item.attributes.amountFormula,
          frequency: item.attributes.frequency as ExpenseDefinition['frequency'],
          startDate: item.attributes.startDate,
          category: item.attributes.category,
          isTaxDeductible: item.attributes.isTaxDeductible,
          linkedAccountId: item.attributes.linkedAccountId,
          linkedAccountName: item.attributes.linkedAccountName,
          inflationAdjusted: item.attributes.inflationAdjusted,
          isActive: item.attributes.isActive,
          endConditionType: (item.attributes.endConditionType || 'none') as ExpenseDefinition['endConditionType'],
          endConditionAccountId: item.attributes.endConditionAccountId,
          endDate: item.attributes.endDate,
          endAmountThreshold: item.attributes.endAmountThreshold,
        };
      },
      invalidatesTags: ['ExpenseDefinitions', 'Dashboard', 'Scenarios'],
    }),

    updateExpenseDefinition: builder.mutation<void, Partial<ExpenseDefinition> & { id: string }>({
      query: ({ id, ...body }) => ({
        url: `/api/expense-definitions/${id}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: ['ExpenseDefinitions', 'Dashboard', 'Scenarios'],
    }),

    deleteExpenseDefinition: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/expense-definitions/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['ExpenseDefinitions', 'Dashboard', 'Scenarios'],
    }),

    // Investment Contributions
    getInvestmentContributions: builder.query<InvestmentContributionListResponse, void>({
      query: () => '/api/investment-contributions',
      providesTags: ['InvestmentContributions'],
    }),

    createInvestmentContribution: builder.mutation<InvestmentContribution, CreateInvestmentContributionRequest>({
      query: (body) => ({
        url: '/api/investment-contributions',
        method: 'POST',
        body,
      }),
      invalidatesTags: ['InvestmentContributions', 'Dashboard', 'Scenarios'],
    }),

    updateInvestmentContribution: builder.mutation<InvestmentContribution, { id: string } & UpdateInvestmentContributionRequest>({
      query: ({ id, ...body }) => ({
        url: `/api/investment-contributions/${id}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: ['InvestmentContributions', 'Dashboard', 'Scenarios'],
    }),

    deleteInvestmentContribution: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/investment-contributions/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['InvestmentContributions', 'Dashboard', 'Scenarios'],
    }),

    // Financial Goals
    getFinancialGoals: builder.query<FinancialGoalListResponse, void>({
      query: () => '/api/financial-goals',
      providesTags: ['FinancialGoals'],
    }),

    createFinancialGoal: builder.mutation<FinancialGoal, CreateFinancialGoalRequest>({
      query: (body) => ({
        url: '/api/financial-goals',
        method: 'POST',
        body,
      }),
      invalidatesTags: ['FinancialGoals', 'Dashboard'],
    }),

    updateFinancialGoal: builder.mutation<FinancialGoal, { id: string } & UpdateFinancialGoalRequest>({
      query: ({ id, ...body }) => ({
        url: `/api/financial-goals/${id}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: ['FinancialGoals', 'Dashboard'],
    }),

    deleteFinancialGoal: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/financial-goals/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['FinancialGoals', 'Dashboard'],
    }),
  }),
});

export const {
  // Accounts
  useGetAccountsQuery,
  useGetAccountQuery,
  useGetAccountHistoryQuery,
  useCreateAccountMutation,
  useUpdateAccountMutation,
  useDeleteAccountMutation,
  // Transactions
  useGetTransactionsQuery,
  useGetAccountTransactionsQuery,
  useCreateTransactionMutation,
  useDeleteTransactionMutation,
  // FX
  useGetFxRatesQuery,
  // Net Worth
  useGetNetWorthHistoryQuery,
  // Tax Profiles
  useGetTaxProfilesQuery,
  useCreateTaxProfileMutation,
  useUpdateTaxProfileMutation,
  useDeleteTaxProfileMutation,
  // Income Sources
  useGetIncomeSourcesQuery,
  useGetIncomeSourcesWithSummaryQuery,
  useCreateIncomeSourceMutation,
  useUpdateIncomeSourceMutation,
  useDeleteIncomeSourceMutation,
  // Expense Definitions
  useGetExpenseDefinitionsQuery,
  useCreateExpenseDefinitionMutation,
  useUpdateExpenseDefinitionMutation,
  useDeleteExpenseDefinitionMutation,
  // Investment Contributions
  useGetInvestmentContributionsQuery,
  useCreateInvestmentContributionMutation,
  useUpdateInvestmentContributionMutation,
  useDeleteInvestmentContributionMutation,
  // Financial Goals
  useGetFinancialGoalsQuery,
  useCreateFinancialGoalMutation,
  useUpdateFinancialGoalMutation,
  useDeleteFinancialGoalMutation,
} = financesApi;
