// Re-export all API endpoints
export { authApi, useLogoutMutation } from './auth';
export { dashboardApi, useGetDashboardQuery, useGetNetWorthQuery, useGetDimensionScoresQuery, useGetStreaksQuery, useGetTodaysTasksQuery, useGetProjectionsQuery, useCompleteTaskMutation } from './dashboard';
export { 
  financesApi, 
  useGetAccountsQuery, 
  useGetAccountQuery, 
  useGetAccountHistoryQuery, 
  useCreateAccountMutation, 
  useUpdateAccountMutation, 
  useDeleteAccountMutation, 
  useGetTransactionsQuery, 
  useGetAccountTransactionsQuery, 
  useCreateTransactionMutation, 
  useDeleteTransactionMutation, 
  useGetFxRatesQuery, 
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
} from './finances';
export { simulationApi, useGetScenariosQuery, useGetScenarioQuery, useCreateScenarioMutation, useUpdateScenarioMutation, useDeleteScenarioMutation, useSetActiveScenarioMutation, useGetScenarioEventsQuery, useAddEventMutation, useUpdateEventMutation, useDeleteEventMutation, useRunSimulationMutation, useGetScenarioProjectionsQuery } from './simulation';
export { settingsApi, useGetProfileQuery, useUpdateProfileMutation, useUpdateDimensionWeightsMutation, useGetApiKeysQuery, useCreateApiKeyMutation, useRevokeApiKeyMutation } from './settings';
export type { ProfileSettings, ApiKeyInfo, CreateApiKeyResponse, UpdateProfileRequest, CreateApiKeyRequest, DimensionWeight } from './settings';
// Metrics
export {
  metricsApi,
  useGetMetricDefinitionsQuery,
  useCreateMetricDefinitionMutation,
  useUpdateMetricDefinitionMutation,
  useDeleteMetricDefinitionMutation,
  useGetMetricRecordsQuery,
  useUpdateMetricRecordMutation,
  useDeleteMetricRecordMutation,
  useGetMetricHistoryQuery,
} from './metrics';
export type {
  MetricDefinition,
  MetricRecord,
  MetricRecordsResponse,
  CreateMetricDefinitionRequest,
  UpdateMetricDefinitionRequest,
  UpdateMetricRecordRequest,
  MetricHistoryParams,
  MetricHistoryPoint,
  MetricHistoryData,
  MetricHistoryResponse,
} from './metrics';
// Dimensions
export {
  dimensionsApi,
  useGetDimensionsQuery,
  useGetDimensionQuery,
  useUpdateDimensionWeightMutation,
} from './dimensions';
// Milestones
export {
  milestonesApi,
  useGetMilestonesQuery,
  useGetMilestoneQuery,
  useCreateMilestoneMutation,
  useUpdateMilestoneMutation,
  useDeleteMilestoneMutation,
} from './milestones';
export type {
  MilestoneAttributes,
  MilestoneItem,
  MilestoneListResponse,
  MilestoneDetailResponse,
  CreateMilestoneRequest,
  UpdateMilestoneRequest,
} from './milestones';
// Data Portability
export {
  dataPortabilityApi,
  useLazyExportDataQuery,
  useImportDataMutation,
  useImportDataFileMutation,
} from './dataPortability';
export type {
  LifeOSExport,
  ExportSchema,
  ExportMeta,
  ImportRequest,
  ImportResult,
  ImportEntityResult,
} from './dataPortability';
