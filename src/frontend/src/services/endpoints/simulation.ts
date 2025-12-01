import { apiSlice } from '@store/api/apiSlice';
import type { 
  Scenario, 
  FutureEvent,
  ProjectionDataPoint,
  MilestoneResult,
} from '@/types';

// API Response types matching backend
interface ScenarioApiItem {
  id: string;
  type: 'scenario';
  attributes: {
    name: string;
    description: string;
    startDate: string;
    endDate: string;
    isBaseline: boolean;
    lastRunAt: string | null;
  };
}

interface ScenariosResponse {
  data: ScenarioApiItem[];
}

interface ScenarioResponse {
  data: ScenarioApiItem;
}

interface RunSimulationResponse {
  data: {
    scenarioId: string;
    status: string;
    periodsCalculated: number;
    executionTimeMs: number;
    startDate: string;
    endDate: string;
    keyMilestones: Array<{
      description: string;
      date: string;
      value: number;
      yearsAway: number;
    }>;
  };
}

interface ProjectionsResponse {
  data: {
    scenarioId: string;
    monthlyProjections: Array<{
      period: string;
      netWorth: number;
      totalAssets: number;
      totalLiabilities: number;
      breakdownByType: Record<string, number>;
      accounts: Array<{
        accountId: string;
        accountName: string;
        balance: number;
        periodIncome: number;
        periodExpenses: number;
      }>;
    }>;
    milestones: Array<{
      description: string;
      date: string;
      value: number;
      yearsAway: number;
    }>;
    summary: {
      startNetWorth: number;
      endNetWorth: number;
      totalGrowth: number;
      annualizedReturn: number;
      totalMonths: number;
    };
  };
}

interface EventApiItem {
  id: string;
  type: 'event';
  attributes: {
    scenarioId: string;
    name: string;
    eventType: string;
    date: string;
    amount: number;
    currency: string;
    isRecurring: boolean;
    recurringFrequency?: string;
  };
}

interface EventsResponse {
  data: EventApiItem[];
}

interface EventResponse {
  data: EventApiItem;
}

// Transform functions
const transformScenario = (item: ScenarioApiItem): Scenario => ({
  id: item.id,
  name: item.attributes.name,
  description: item.attributes.description,
  startDate: item.attributes.startDate,
  endDate: item.attributes.endDate,
  createdAt: item.attributes.lastRunAt || new Date().toISOString(),
  events: [],
  isActive: item.attributes.isBaseline,
});

const transformEvent = (item: EventApiItem): FutureEvent => ({
  id: item.id,
  scenarioId: item.attributes.scenarioId,
  name: item.attributes.name,
  type: item.attributes.eventType as FutureEvent['type'],
  date: item.attributes.date,
  amount: item.attributes.amount,
  currency: item.attributes.currency,
  isRecurring: item.attributes.isRecurring,
  recurringFrequency: item.attributes.recurringFrequency as FutureEvent['recurringFrequency'],
});

export const simulationApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    // Scenarios
    getScenarios: builder.query<Scenario[], void>({
      query: () => '/api/simulations/scenarios',
      transformResponse: (response: ScenariosResponse) => 
        response.data.map(transformScenario),
      providesTags: ['Scenarios'],
    }),
    
    getScenario: builder.query<Scenario, string>({
      query: (id) => `/api/simulations/scenarios/${id}`,
      transformResponse: (response: ScenarioResponse) => 
        transformScenario(response.data),
      providesTags: (_result, _error, id) => [{ type: 'Scenarios', id }],
    }),
    
    createScenario: builder.mutation<Scenario, Omit<Scenario, 'id' | 'createdAt' | 'events'>>({
      query: (body) => ({
        url: '/api/simulations/scenarios',
        method: 'POST',
        body: {
          data: {
            type: 'scenario',
            attributes: {
              name: body.name,
              description: body.description,
              startDate: body.startDate,
              endDate: body.endDate,
              isBaseline: body.isActive,
            },
          },
        },
      }),
      transformResponse: (response: ScenarioResponse) => 
        transformScenario(response.data),
      invalidatesTags: ['Scenarios'],
    }),
    
    updateScenario: builder.mutation<Scenario, Partial<Scenario> & { id: string }>({
      query: ({ id, ...body }) => ({
        url: `/api/simulations/scenarios/${id}`,
        method: 'PATCH',
        body: {
          data: {
            type: 'scenario',
            attributes: {
              ...(body.name && { name: body.name }),
              ...(body.description && { description: body.description }),
              ...(body.startDate && { startDate: body.startDate }),
              ...(body.endDate && { endDate: body.endDate }),
              ...(body.isActive !== undefined && { isBaseline: body.isActive }),
            },
          },
        },
      }),
      transformResponse: (response: ScenarioResponse) => 
        transformScenario(response.data),
      invalidatesTags: (_result, _error, { id }) => [{ type: 'Scenarios', id }, 'Scenarios'],
    }),
    
    deleteScenario: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/simulations/scenarios/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Scenarios'],
    }),
    
    setActiveScenario: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/simulations/scenarios/${id}`,
        method: 'PATCH',
        body: {
          data: {
            type: 'scenario',
            attributes: {
              isBaseline: true,
            },
          },
        },
      }),
      invalidatesTags: ['Scenarios', 'Dashboard'],
    }),
    
    // Future Events
    getScenarioEvents: builder.query<FutureEvent[], string>({
      query: (scenarioId) => `/api/simulations/events?scenarioId=${scenarioId}`,
      transformResponse: (response: EventsResponse) => 
        response.data.map(transformEvent),
      providesTags: (_result, _error, scenarioId) => [{ type: 'Scenarios', id: scenarioId }],
    }),
    
    addEvent: builder.mutation<FutureEvent, { scenarioId: string; event: Omit<FutureEvent, 'id' | 'scenarioId'> }>({
      query: ({ scenarioId, event }) => ({
        url: '/api/simulations/events',
        method: 'POST',
        body: {
          data: {
            type: 'event',
            attributes: {
              scenarioId,
              name: event.name,
              eventType: event.type,
              date: event.date,
              amount: event.amount,
              currency: event.currency,
              isRecurring: event.isRecurring,
              recurringFrequency: event.recurringFrequency,
            },
          },
        },
      }),
      transformResponse: (response: EventResponse) => 
        transformEvent(response.data),
      invalidatesTags: (_result, _error, { scenarioId }) => [{ type: 'Scenarios', id: scenarioId }],
    }),
    
    updateEvent: builder.mutation<FutureEvent, { eventId: string; event: Partial<FutureEvent> }>({
      query: ({ eventId, event }) => ({
        url: `/api/simulations/events/${eventId}`,
        method: 'PATCH',
        body: {
          data: {
            type: 'event',
            attributes: {
              ...(event.name && { name: event.name }),
              ...(event.type && { eventType: event.type }),
              ...(event.date && { date: event.date }),
              ...(event.amount !== undefined && { amount: event.amount }),
              ...(event.currency && { currency: event.currency }),
              ...(event.isRecurring !== undefined && { isRecurring: event.isRecurring }),
              ...(event.recurringFrequency && { recurringFrequency: event.recurringFrequency }),
            },
          },
        },
      }),
      transformResponse: (response: EventResponse) => 
        transformEvent(response.data),
      invalidatesTags: ['Scenarios'],
    }),
    
    deleteEvent: builder.mutation<void, { scenarioId: string; eventId: string }>({
      query: ({ eventId }) => ({
        url: `/api/simulations/events/${eventId}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, { scenarioId }) => [{ type: 'Scenarios', id: scenarioId }],
    }),
    
    // Simulation Execution
    runSimulation: builder.mutation<RunSimulationResponse['data'], string>({
      query: (scenarioId) => ({
        url: `/api/simulations/scenarios/${scenarioId}/run`,
        method: 'POST',
      }),
      transformResponse: (response: RunSimulationResponse) => response.data,
      invalidatesTags: ['Projections'],
    }),
    
    getScenarioProjections: builder.query<{
      projections: ProjectionDataPoint[];
      milestones: MilestoneResult[];
      summary: {
        startNetWorth: number;
        endNetWorth: number;
        totalGrowth: number;
        annualizedReturn: number;
        totalMonths: number;
      };
    }, { scenarioId: string; currency?: string }>({
      query: ({ scenarioId }) => 
        `/api/simulations/scenarios/${scenarioId}/projections`,
      transformResponse: (response: ProjectionsResponse) => ({
        projections: response.data.monthlyProjections.map((p) => ({
          date: p.period,
          netWorth: p.netWorth,
          income: p.accounts.reduce((sum, a) => sum + a.periodIncome, 0),
          expenses: p.accounts.reduce((sum, a) => sum + a.periodExpenses, 0),
          savings: p.accounts.reduce((sum, a) => sum + a.periodIncome - a.periodExpenses, 0),
        })),
        milestones: response.data.milestones.map((m) => ({
          label: m.description,
          targetValue: m.value,
          achievedDate: m.date,
          probability: 1 - (m.yearsAway / 20), // Simple probability based on years away
        })),
        summary: response.data.summary,
      }),
      providesTags: ['Projections'],
    }),
  }),
});

export const {
  // Scenarios
  useGetScenariosQuery,
  useGetScenarioQuery,
  useCreateScenarioMutation,
  useUpdateScenarioMutation,
  useDeleteScenarioMutation,
  useSetActiveScenarioMutation,
  // Events
  useGetScenarioEventsQuery,
  useAddEventMutation,
  useUpdateEventMutation,
  useDeleteEventMutation,
  // Simulation
  useRunSimulationMutation,
  useGetScenarioProjectionsQuery,
} = simulationApi;
