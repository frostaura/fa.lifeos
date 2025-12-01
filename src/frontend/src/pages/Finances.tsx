import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Spinner } from '@components/atoms/Spinner';
import { NetWorthChart } from '@components/organisms/NetWorthChart';
import { FinancialGoalsWidget } from '@components/organisms/FinancialGoalsWidget';
import { NetWorthGoalTracker } from '@components/organisms/NetWorthGoalTracker';
import { LoanPayoffCalculator } from '@components/organisms/LoanPayoffCalculator';
import { AccountRow } from '@components/molecules/AccountRow';
import { CurrencySelector, formatCurrency } from '@components/molecules/CurrencySelector';
import { Plus, ArrowUpRight, ArrowDownRight, Calculator, TrendingUp } from 'lucide-react';
import { cn } from '@utils/cn';
import type { Account, NetWorthDataPoint, FxRate, Scenario } from '@/types';
import { AddAccountModal } from './placeholders/AddAccountModal';
import { AddTransactionModal } from './placeholders/AddTransactionModal';

interface AccountApiResponse {
  data: Array<{
    id: string;
    type: string;
    attributes: {
      name: string;
      accountType: string;
      currency: string;
      currentBalance: number;
      isLiability: boolean;
      isActive: boolean;
      institution?: string;
      interestRateAnnual?: number;
      monthlyInterest?: number;
      monthlyFee?: number;
    };
  }>;
  meta?: {
    netWorth: number;
    totalAssets: number;
    totalLiabilities: number;
    totalMonthlyInterest?: number;
  };
}

interface ScenarioApiResponse {
  data: Array<{
    id: string;
    type: string;
    attributes: {
      name: string;
      description?: string;
      isBaseline: boolean;
    };
  }>;
}

export function Finances() {
  const navigate = useNavigate();
  const [isAddAccountOpen, setIsAddAccountOpen] = useState(false);
  const [isAddTransactionOpen, setIsAddTransactionOpen] = useState(false);
  const [editingAccount, setEditingAccount] = useState<Account | null>(null);
  const [loading, setLoading] = useState(true);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [netWorth, setNetWorth] = useState(0);
  const [scenarios, setScenarios] = useState<Scenario[]>([]);
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  const [chartPeriod, setChartPeriod] = useState<'1M' | '3M' | '6M' | '1Y' | 'ALL'>('1Y');
  const [fxRates] = useState<FxRate[]>([
    { pair: 'USD/ZAR', rate: 18.52, change: 0.15, timestamp: new Date().toISOString() },
    { pair: 'EUR/ZAR', rate: 19.85, change: -0.22, timestamp: new Date().toISOString() },
    { pair: 'BTC/ZAR', rate: 1245000, change: 2.8, timestamp: new Date().toISOString() },
  ]);

  const [netWorthHistory, setNetWorthHistory] = useState<NetWorthDataPoint[]>([]);

  const handleDeleteAccount = async (accountId: string) => {
    if (!confirm('Are you sure you want to delete this account? This action cannot be undone.')) {
      return;
    }
    
    const token = localStorage.getItem('accessToken');
    const headers: HeadersInit = token ? { 'Authorization': `Bearer ${token}` } : {};
    
    try {
      const res = await fetch(`/api/accounts/${accountId}`, {
        method: 'DELETE',
        headers,
      });
      
      if (res.ok) {
        // Remove from local state
        setAccounts(prev => prev.filter(a => a.id !== accountId));
        // Trigger refresh to update net worth
        setRefreshTrigger(prev => prev + 1);
      } else {
        alert('Failed to delete account');
      }
    } catch (err) {
      console.error('Error deleting account:', err);
      alert('Failed to delete account');
    }
  };

  const handleEditAccount = (account: Account) => {
    setEditingAccount(account);
    setIsAddAccountOpen(true);
  };

  useEffect(() => {
    const fetchData = async () => {
      const token = localStorage.getItem('accessToken');
      const headers: HeadersInit = token ? { 'Authorization': `Bearer ${token}` } : {};

      try {
        // Fetch accounts
        const accountsRes = await fetch('/api/accounts', { headers });
        if (accountsRes.ok) {
          const accountsData: AccountApiResponse = await accountsRes.json();
          const mappedAccounts: Account[] = accountsData.data.map(a => ({
            id: a.id,
            name: a.attributes.name,
            type: a.attributes.accountType as Account['type'],
            balance: a.attributes.isLiability ? -a.attributes.currentBalance : a.attributes.currentBalance,
            currency: a.attributes.currency,
            change: 0,
            lastUpdated: new Date().toISOString(),
            isLiability: a.attributes.isLiability,
            institution: a.attributes.institution,
            interestRateAnnual: a.attributes.interestRateAnnual,
            monthlyInterest: a.attributes.monthlyInterest,
            monthlyFee: a.attributes.monthlyFee,
          }));
          setAccounts(mappedAccounts);
          
          // Calculate net worth
          const assets = mappedAccounts.filter(a => a.balance > 0).reduce((sum, a) => sum + a.balance, 0);
          const liabilities = mappedAccounts.filter(a => a.balance < 0).reduce((sum, a) => sum + Math.abs(a.balance), 0);
          setNetWorth(assets - liabilities);
        }

        // Fetch scenarios
        const scenariosRes = await fetch('/api/simulations/scenarios', { headers });
        if (scenariosRes.ok) {
          const scenariosData: ScenarioApiResponse = await scenariosRes.json();
          const mappedScenarios: Scenario[] = scenariosData.data.map(s => ({
            id: s.id,
            name: s.attributes.name,
            description: s.attributes.description,
            startDate: new Date().toISOString(),
            endDate: new Date().toISOString(),
            createdAt: new Date().toISOString(),
            events: [],
            isActive: s.attributes.isBaseline,
          }));
          setScenarios(mappedScenarios);
        }

        // Fetch net worth history from API
        const historyRes = await fetch(`/api/dashboard/net-worth/history?period=${chartPeriod}`, { headers });
        if (historyRes.ok) {
          const historyData = await historyRes.json();
          if (historyData.data?.history?.length > 0) {
            setNetWorthHistory(historyData.data.history.map((h: { date: string; value: number }) => ({
              date: h.date,
              value: h.value,
            })));
          } else {
            // Fallback to generated data if no history exists yet
            setNetWorthHistory(generateNetWorthHistory());
          }
        } else {
          // Fallback to generated data if API fails
          setNetWorthHistory(generateNetWorthHistory());
        }
      } catch (err) {
        console.error('Failed to fetch finances data:', err);
        // Fallback to generated data on error
        setNetWorthHistory(generateNetWorthHistory());
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [refreshTrigger, chartPeriod]);

  const handleAddAccount = async (data: { name: string; type: string; balance: number; currency: string; institution?: string; isLiability?: boolean; interestRateAnnual?: number }) => {
    const token = localStorage.getItem('accessToken');
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      ...(token ? { 'Authorization': `Bearer ${token}` } : {})
    };

    try {
      const res = await fetch('/api/accounts', {
        method: 'POST',
        headers,
        body: JSON.stringify({
          name: data.name,
          accountType: data.type,
          currency: data.currency,
          initialBalance: data.balance,
          institution: data.institution || null,
          isLiability: data.isLiability || false,
          interestRateAnnual: data.interestRateAnnual || null,
        }),
      });

      if (res.ok) {
        setRefreshTrigger(prev => prev + 1);
      }
    } catch (error) {
      console.error('Failed to add account:', error);
    }
  };

  const handleUpdateAccount = async (id: string, data: { name: string; type: string; balance: number; currency: string; institution?: string; isLiability?: boolean; interestRateAnnual?: number; monthlyFee?: number }) => {
    const token = localStorage.getItem('accessToken');
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      ...(token ? { 'Authorization': `Bearer ${token}` } : {})
    };

    try {
      const res = await fetch(`/api/accounts/${id}`, {
        method: 'PATCH',
        headers,
        body: JSON.stringify({
          name: data.name,
          accountType: data.type,
          currency: data.currency,
          currentBalance: data.balance,
          institution: data.institution || null,
          isLiability: data.isLiability || false,
          interestRateAnnual: data.interestRateAnnual || null,
          monthlyFee: data.monthlyFee || null,
        }),
      });

      if (res.ok) {
        setEditingAccount(null);
        setRefreshTrigger(prev => prev + 1);
      } else {
        alert('Failed to update account');
      }
    } catch (error) {
      console.error('Failed to update account:', error);
      alert('Failed to update account');
    }
  };

  const handleCloseModal = () => {
    setIsAddAccountOpen(false);
    setEditingAccount(null);
  };

  // Generate fallback mock history based on current net worth (used when API has no data)
  const generateNetWorthHistory = (): NetWorthDataPoint[] => {
    const periodMonths: Record<string, number> = {
      '1M': 1,
      '3M': 3,
      '6M': 6,
      '1Y': 12,
      'ALL': 24,
    };
    const months = periodMonths[chartPeriod] || 12;
    const history: NetWorthDataPoint[] = [];
    
    for (let i = months; i >= 0; i--) {
      const date = new Date();
      date.setMonth(date.getMonth() - i);
      const variance = (Math.random() - 0.3) * 0.08;
      history.push({
        date: date.toISOString().slice(0, 7),
        value: Math.round(netWorth * (1 - (i * 0.015) + variance)),
      });
    }
    return history;
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Spinner size="lg" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-text-primary">Finances</h1>
          <p className="text-text-secondary mt-1">Track your wealth</p>
        </div>
        <div className="flex items-center gap-3">
          <CurrencySelector size="md" />
          <Button onClick={() => setIsAddTransactionOpen(true)} variant="secondary" icon={<Plus className="w-4 h-4" />}>
            Transaction
          </Button>
          <Button onClick={() => setIsAddAccountOpen(true)} icon={<Plus className="w-4 h-4" />}>
            Account
          </Button>
        </div>
      </div>

      {/* Net Worth Banner */}
      <GlassCard variant="elevated" glow="accent" className="p-8">
        <div className="text-center">
          <p className="text-text-secondary mb-2">Total Net Worth</p>
          <h2 className="text-5xl font-bold text-text-primary mb-2">
            {formatCurrency(netWorth, 'ZAR')}
          </h2>
          <div className={cn(
            "flex items-center justify-center gap-1",
            netWorth >= 0 ? "text-semantic-success" : "text-semantic-error"
          )}>
            {netWorth >= 0 ? <ArrowUpRight className="w-4 h-4" /> : <ArrowDownRight className="w-4 h-4" />}
            <span>Real-time from {accounts.length} accounts</span>
          </div>
        </div>
      </GlassCard>

      {/* Net Worth Chart */}
      <GlassCard variant="default" className="p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-text-primary">Net Worth History</h2>
          <div className="flex gap-2">
            {(['1M', '3M', '6M', '1Y', 'ALL'] as const).map((period) => (
              <button
                key={period}
                onClick={() => setChartPeriod(period)}
                className={cn(
                  'px-3 py-1 text-sm rounded-lg transition-colors',
                  chartPeriod === period 
                    ? 'bg-accent-purple text-white' 
                    : 'text-text-secondary hover:bg-background-hover'
                )}
              >
                {period}
              </button>
            ))}
          </div>
        </div>
        <NetWorthChart data={netWorthHistory} height={280} />
      </GlassCard>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Accounts */}
        <div className="lg:col-span-2">
          <GlassCard variant="default" className="p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-text-primary">Accounts</h2>
              <span className="text-text-tertiary text-sm">{accounts.length} accounts</span>
            </div>
            <div className="space-y-3">
              {accounts.length === 0 ? (
                <p className="text-text-tertiary text-center py-8">No accounts yet. Add your first account!</p>
              ) : (
                accounts.map((account) => (
                  <AccountRow
                    key={account.id}
                    account={account}
                    onClick={() => navigate(`/finances/accounts/${account.id}`)}
                    onEdit={() => handleEditAccount(account)}
                    onDelete={() => handleDeleteAccount(account.id)}
                  />
                ))
              )}
            </div>
          </GlassCard>
        </div>

        {/* Sidebar: FX & Simulations */}
        <div className="space-y-6">
          {/* FX Rates */}
          <GlassCard variant="default" className="p-6">
            <div className="flex items-center gap-2 mb-4">
              <TrendingUp className="w-5 h-5 text-accent-cyan" />
              <h2 className="text-lg font-semibold text-text-primary">Exchange Rates</h2>
            </div>
            <div className="space-y-4">
              {fxRates.map((rate) => (
                <div key={rate.pair} className="flex items-center justify-between">
                  <span className="text-text-secondary">{rate.pair}</span>
                  <div className="text-right">
                    <span className="font-medium text-text-primary">
                      {rate.rate.toLocaleString()}
                    </span>
                    <span className={cn(
                      'text-sm ml-2',
                      rate.change > 0 ? 'text-semantic-success' : 'text-semantic-error'
                    )}>
                      {rate.change > 0 ? '+' : ''}{rate.change}%
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </GlassCard>

          {/* Simulations */}
          <GlassCard variant="default" className="p-6">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <Calculator className="w-5 h-5 text-accent-purple" />
                <h2 className="text-lg font-semibold text-text-primary">Simulations</h2>
              </div>
              <button 
                onClick={() => navigate('/simulation/new')}
                className="text-accent-purple hover:text-accent-purple/80 transition-colors"
              >
                <Plus className="w-5 h-5" />
              </button>
            </div>
            <div className="space-y-3">
              {scenarios.length === 0 ? (
                <p className="text-text-tertiary text-center py-4">No simulations yet</p>
              ) : (
                scenarios.map((scenario) => (
                  <div
                    key={scenario.id}
                    onClick={() => navigate(`/simulation/${scenario.id}`)}
                    className="p-3 rounded-lg bg-background-hover/50 hover:bg-background-hover transition-colors cursor-pointer"
                  >
                    <div className="flex items-center justify-between">
                      <span className="font-medium text-text-primary">{scenario.name}</span>
                      {scenario.isActive && (
                        <span className="text-xs px-2 py-0.5 rounded-full bg-semantic-success/20 text-semantic-success">
                          Baseline
                        </span>
                      )}
                    </div>
                    {scenario.description && (
                      <p className="text-sm text-text-tertiary mt-1">{scenario.description}</p>
                    )}
                  </div>
                ))
              )}
            </div>
          </GlassCard>
        </div>
      </div>

      {/* Financial Goals & Calculators Section */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <FinancialGoalsWidget />
        <NetWorthGoalTracker />
        <LoanPayoffCalculator />
      </div>

      {/* Modals */}
      <AddAccountModal 
        isOpen={isAddAccountOpen} 
        onClose={handleCloseModal} 
        onSubmit={handleAddAccount}
        editingAccount={editingAccount}
        onUpdate={handleUpdateAccount}
      />
      <AddTransactionModal isOpen={isAddTransactionOpen} onClose={() => setIsAddTransactionOpen(false)} />
    </div>
  );
}
