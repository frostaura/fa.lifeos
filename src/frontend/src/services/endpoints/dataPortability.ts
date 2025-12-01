import { apiSlice } from '@store/api/apiSlice';

export interface ExportSchema {
  version: string;
  generator: string;
  exportedAt: string;
}

export interface ExportMeta {
  totalEntities: number;
  entityCounts: {
    dimensions: number;
    metricDefinitions: number;
    scoreDefinitions: number;
    taxProfiles: number;
    longevityModels: number;
    accounts: number;
    milestones: number;
    tasks: number;
    streaks: number;
    metricRecords: number;
    scoreRecords: number;
    incomeSources: number;
    expenseDefinitions: number;
    investmentContributions: number;
    financialGoals: number;
    fxRates: number;
    transactions: number;
    simulationScenarios: number;
    simulationEvents: number;
    accountProjections: number;
    netWorthProjections: number;
    longevitySnapshots: number;
  };
}

export interface LifeOSExport {
  schema: ExportSchema;
  data: Record<string, unknown>;
  meta?: ExportMeta;
}

export interface ImportRequest {
  mode: 'replace' | 'merge';
  dryRun?: boolean;
  data: LifeOSExport;
}

export interface ImportEntityResult {
  imported: number;
  skipped: number;
  errors: number;
  errorDetails?: string[];
}

export interface ImportResult {
  status: string;
  mode: string;
  importedAt: string;
  schemaVersion: string;
  results: Record<string, ImportEntityResult>;
  totalImported: number;
  totalSkipped: number;
  totalErrors: number;
  durationMs: number;
  isDryRun: boolean;
}

export const dataPortabilityApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    // Export all user data
    exportData: builder.query<LifeOSExport, void>({
      query: () => '/api/v1/data/export',
    }),

    // Import data (JSON body)
    importData: builder.mutation<{ data: ImportResult }, ImportRequest>({
      query: (body) => ({
        url: '/api/v1/data/import',
        method: 'POST',
        body,
      }),
      invalidatesTags: [
        'Dashboard',
        'Accounts',
        'Transactions',
        'Dimensions',
        'Milestones',
        'Metrics',
        'Settings',
        'Scenarios',
        'Health',
      ],
    }),

    // Import data (file upload)
    importDataFile: builder.mutation<{ data: ImportResult }, { file: File; mode: string; dryRun: boolean }>({
      query: ({ file, mode, dryRun }) => {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('mode', mode);
        formData.append('dryRun', String(dryRun));
        return {
          url: '/api/v1/data/import/upload',
          method: 'POST',
          body: formData,
        };
      },
      invalidatesTags: [
        'Dashboard',
        'Accounts',
        'Transactions',
        'Dimensions',
        'Milestones',
        'Metrics',
        'Settings',
        'Scenarios',
        'Health',
      ],
    }),
  }),
});

export const {
  useLazyExportDataQuery,
  useImportDataMutation,
  useImportDataFileMutation,
} = dataPortabilityApi;
