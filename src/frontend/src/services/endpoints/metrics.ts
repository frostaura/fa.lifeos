import { apiSlice } from '@store/api/apiSlice';

// Types for Metric Definitions
export interface MetricDefinition {
  id: string;
  code: string;
  name: string;
  description?: string;
  unit: string;
  valueType: 'integer' | 'decimal' | 'boolean' | 'percentage';
  targetValue?: number;
  targetDirection: 'AtOrAbove' | 'AtOrBelow';
  isActive: boolean;
  dimensionId?: string;
  dimensionCode?: string;
  latestValue?: number;
  latestRecordedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateMetricDefinitionRequest {
  code: string;
  name: string;
  description?: string;
  unit: string;
  valueType: 'integer' | 'decimal' | 'boolean' | 'percentage';
  targetValue?: number;
  targetDirection?: 'AtOrAbove' | 'AtOrBelow';
  dimensionId?: string;
}

export interface UpdateMetricDefinitionRequest {
  name?: string;
  description?: string;
  unit?: string;
  valueType?: 'integer' | 'decimal' | 'boolean' | 'percentage';
  targetValue?: number;
  targetDirection?: 'AtOrAbove' | 'AtOrBelow';
  isActive?: boolean;
  dimensionId?: string;
}

// Types for Metric Records
export interface MetricRecord {
  id: string;
  definitionCode: string;
  value: number | string;
  recordedAt: string;
  source?: string;
  notes?: string;
  metadata?: Record<string, unknown>;
  createdAt: string;
}

export interface MetricRecordsResponse {
  data: MetricRecord[];
  meta: {
    page: number;
    pageSize: number;
    total: number;
    totalPages: number;
  };
}

export interface UpdateMetricRecordRequest {
  valueNumber?: number;
  valueBoolean?: boolean;
  valueString?: string;
  notes?: string;
  metadata?: Record<string, unknown>;
}

// Types for Metric History
export interface MetricHistoryParams {
  codes: string[];
  from?: string;
  to?: string;
  granularity?: 'raw' | 'hourly' | 'daily' | 'weekly' | 'monthly';
  limit?: number;
}

export interface MetricHistoryPoint {
  timestamp: string;
  value: number;
  source?: string;
  aggregation?: string;
}

export interface MetricHistoryData {
  points: MetricHistoryPoint[];
  targetValue?: number;
}

export interface MetricHistoryResponse {
  data: Record<string, MetricHistoryData>;
  meta: {
    from: string;
    to: string;
    granularity: string;
    metricsReturned: string[];
  };
}

// API Response types
interface MetricDefinitionApiResponse {
  data: Array<{
    id: string;
    type: string;
    attributes: {
      code: string;
      name: string;
      description?: string;
      dimensionId?: string;
      dimensionCode?: string;
      unit: string;
      valueType: string;
      targetValue?: number;
      isActive: boolean;
      latestValue?: number;
      latestRecordedAt?: string;
      createdAt: string;
      updatedAt: string;
    };
  }>;
}

interface MetricRecordsApiResponse {
  data: Array<{
    id: string;
    type: string;
    attributes: {
      metricCode: string;
      valueNumber: number | null;
      valueBoolean: boolean | null;
      valueString: string | null;
      recordedAt: string;
      source?: string;
      notes?: string;
      metadata?: Record<string, unknown>;
    };
  }>;
  meta: {
    page: number;
    perPage: number;
    total: number;
    totalPages: number;
  };
}

export const metricsApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    // Metric Definitions
    getMetricDefinitions: builder.query<MetricDefinition[], void>({
      query: () => '/api/metrics/definitions',
      transformResponse: (response: MetricDefinitionApiResponse) => {
        if (!response?.data) return [];
        return response.data.map((item) => ({
          id: item.id,
          code: item.attributes.code,
          name: item.attributes.name,
          description: item.attributes.description,
          dimensionId: item.attributes.dimensionId,
          dimensionCode: item.attributes.dimensionCode,
          unit: item.attributes.unit,
          valueType: item.attributes.valueType as MetricDefinition['valueType'],
          targetValue: item.attributes.targetValue,
          targetDirection: (item.attributes.targetDirection || 'AtOrAbove') as MetricDefinition['targetDirection'],
          isActive: item.attributes.isActive,
          latestValue: item.attributes.latestValue,
          latestRecordedAt: item.attributes.latestRecordedAt,
          createdAt: item.attributes.createdAt,
          updatedAt: item.attributes.updatedAt,
        }));
      },
      providesTags: ['Metrics'],
    }),

    createMetricDefinition: builder.mutation<MetricDefinition, CreateMetricDefinitionRequest>({
      query: (body) => ({
        url: '/api/metrics/definitions',
        method: 'POST',
        body,
      }),
      invalidatesTags: ['Metrics'],
    }),

    updateMetricDefinition: builder.mutation<void, { code: string } & UpdateMetricDefinitionRequest>({
      query: ({ code, ...body }) => ({
        url: `/api/metrics/definitions/${code}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: ['Metrics'],
    }),

    deleteMetricDefinition: builder.mutation<void, string>({
      query: (code) => ({
        url: `/api/metrics/definitions/${code}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Metrics'],
    }),

    // Metric Records
    getMetricRecords: builder.query<MetricRecordsResponse, { code: string; page?: number; pageSize?: number }>({
      query: ({ code, page = 1, pageSize = 20 }) =>
        `/api/metrics/records?code=${code}&page=${page}&pageSize=${pageSize}`,
      transformResponse: (response: MetricRecordsApiResponse) => {
        return {
          data: response.data.map((item) => ({
            id: item.id,
            definitionCode: item.attributes.metricCode,
            value: item.attributes.valueNumber ?? item.attributes.valueString ?? (item.attributes.valueBoolean ? 1 : 0),
            recordedAt: item.attributes.recordedAt,
            source: item.attributes.source,
            notes: item.attributes.notes,
            metadata: item.attributes.metadata,
            createdAt: item.attributes.recordedAt,
          })),
          meta: {
            page: response.meta.page,
            pageSize: response.meta.perPage,
            total: response.meta.total,
            totalPages: response.meta.totalPages,
          },
        };
      },
      providesTags: (_result, _error, { code }) => [{ type: 'Metrics', id: code }],
    }),

    updateMetricRecord: builder.mutation<void, { id: string } & UpdateMetricRecordRequest>({
      query: ({ id, ...body }) => ({
        url: `/api/metrics/records/${id}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: ['Metrics'],
    }),

    deleteMetricRecord: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/metrics/records/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Metrics'],
    }),

    // Metric History
    getMetricHistory: builder.query<MetricHistoryResponse, MetricHistoryParams>({
      query: ({ codes, from, to, granularity = 'daily', limit = 100 }) => {
        const params = new URLSearchParams({
          codes: codes.join(','),
          granularity,
          limit: limit.toString(),
        });
        if (from) params.append('from', from);
        if (to) params.append('to', to);
        return `/api/metrics/history?${params.toString()}`;
      },
      providesTags: (result) =>
        result
          ? [
              ...Object.keys(result.data).map((code) => ({ type: 'Metrics' as const, id: `history-${code}` })),
              { type: 'Metrics' as const, id: 'HISTORY' },
            ]
          : [{ type: 'Metrics' as const, id: 'HISTORY' }],
    }),
  }),
});

export const {
  useGetMetricDefinitionsQuery,
  useCreateMetricDefinitionMutation,
  useUpdateMetricDefinitionMutation,
  useDeleteMetricDefinitionMutation,
  useGetMetricRecordsQuery,
  useUpdateMetricRecordMutation,
  useDeleteMetricRecordMutation,
  useGetMetricHistoryQuery,
} = metricsApi;
