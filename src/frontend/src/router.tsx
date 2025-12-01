import { createHashRouter, Navigate } from 'react-router-dom';
import { AppLayout } from '@components/templates/AppLayout';
import { Login } from '@pages/Login';
import { Dashboard } from '@pages/Dashboard';
import { Dimensions } from '@pages/Dimensions';
import { DimensionDetail } from '@pages/DimensionDetail';
import { Finances } from '@pages/Finances';
import { AccountDetail } from '@pages/AccountDetail';
import { Health } from '@pages/Health';
import { Simulation } from '@pages/Simulation';
import { SimulationDetail } from '@pages/SimulationDetail';
import { SimulationBuilder } from '@pages/SimulationBuilder';
import { Metrics } from '@pages/Metrics';
import {
  Settings,
  ProfileSettings,
  ApiKeySettings,
  DimensionSettings,
  TaxProfileSettings,
  IncomeExpenseSettings,
  InvestmentSettings,
  GoalsSettings,
  DataPortabilitySettings,
} from '@pages/Settings';
import { NotFound } from '@pages/NotFound';
import { AuthGuard } from '@components/AuthGuard';

export const router = createHashRouter([
  {
    path: '/login',
    element: <Login />,
  },
  {
    path: '/',
    element: <AuthGuard><AppLayout /></AuthGuard>,
    children: [
      { index: true, element: <Dashboard /> },
      { path: 'dimensions', element: <Dimensions /> },
      { path: 'dimensions/:dimensionId', element: <DimensionDetail /> },
      { path: 'finances', element: <Finances /> },
      { path: 'finances/accounts/:accountId', element: <AccountDetail /> },
      { path: 'simulation', element: <Simulation /> },
      { path: 'simulation/new', element: <SimulationBuilder /> },
      { path: 'simulation/:scenarioId', element: <SimulationDetail /> },
      { path: 'health', element: <Health /> },
      { path: 'metrics', element: <Metrics /> },
      {
        path: 'settings',
        element: <Settings />,
        children: [
          { index: true, element: <Navigate to="profile" replace /> },
          { path: 'profile', element: <ProfileSettings /> },
          { path: 'api-keys', element: <ApiKeySettings /> },
          { path: 'dimensions', element: <DimensionSettings /> },
          { path: 'tax-profiles', element: <TaxProfileSettings /> },
          { path: 'income-expenses', element: <IncomeExpenseSettings /> },
          { path: 'investments', element: <InvestmentSettings /> },
          { path: 'goals', element: <GoalsSettings /> },
          { path: 'data', element: <DataPortabilitySettings /> },
        ],
      },
    ],
  },
  { path: '*', element: <NotFound /> },
]);
