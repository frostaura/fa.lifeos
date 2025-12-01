import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { BaseQueryFn, FetchArgs, FetchBaseQueryError } from '@reduxjs/toolkit/query/react';
import type { RootState } from '../index';
import { logout } from '../slices/authSlice';

const baseQuery = fetchBaseQuery({
  // Use relative URLs so Vite proxy handles routing in dev, and nginx handles in prod
  baseUrl: '',
  prepareHeaders: (headers, { getState }) => {
    const token = (getState() as RootState).auth.token;
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }
    return headers;
  },
  credentials: 'include',
});

// Wrapper that handles 401 responses by logging out
const baseQueryWithAutoLogout: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> = async (
  args,
  api,
  extraOptions
) => {
  const result = await baseQuery(args, api, extraOptions);
  
  if (result.error && result.error.status === 401) {
    // Clear localStorage
    localStorage.removeItem('accessToken');
    localStorage.removeItem('user');
    
    // Dispatch logout action to clear Redux state
    api.dispatch(logout());
    
    // Redirect to login page
    window.location.href = '/#/login';
  }
  
  return result;
};

export const apiSlice = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithAutoLogout,
  tagTypes: [
    'Dashboard',
    'Dimensions',
    'Milestones',
    'Tasks',
    'Habits',
    'Metrics',
    'Accounts',
    'Transactions',
    'Scenarios',
    'Projections',
    'Health',
    'Settings',
    'TaxProfiles',
    'IncomeSources',
    'ExpenseDefinitions',
    'InvestmentContributions',
    'FinancialGoals',
  ],
  endpoints: () => ({}),
});
