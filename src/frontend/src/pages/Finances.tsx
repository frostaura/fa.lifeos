import { useState, useEffect } from 'react';
import { useNavigate, NavLink, Outlet, useLocation } from 'react-router-dom';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Spinner } from '@components/atoms/Spinner';
import { NetWorthChart } from '@components/organisms/NetWorthChart';
import { FinancialGoalsWidget } from '@components/organisms/FinancialGoalsWidget';
import { NetWorthGoalTracker } from '@components/organisms/NetWorthGoalTracker';
import { LoanPayoffCalculator } from '@components/organisms/LoanPayoffCalculator';
import { AccountRow } from '@components/molecules/AccountRow';
import { CurrencySelector, formatCurrency } from '@components/molecules/CurrencySelector';
import { PeriodSelector } from '@components/molecules/PeriodSelector';
import { Plus, ArrowUpRight, ArrowDownRight, Calculator, TrendingUp, Info, Wallet, DollarSign, Target, Zap } from 'lucide-react';
import { cn } from '@utils/cn';
import { confirmToast } from '@utils/confirmToast';
import type { Account, NetWorthDataPoint, FxRate, Scenario } from '@/types';
import { AddAccountModal } from './placeholders/AddAccountModal';
import { AddTransactionModal } from './placeholders/AddTransactionModal';
import { useCreateAccountMutation, useUpdateAccountMutation, useDeleteAccountMutation } from '@services/endpoints/finances';
import { useChartPeriod, getMonthsForPeriod, type ChartPeriod } from '@/hooks/useChartPeriod';
import toast from 'react-hot-toast';

// Tab navigation items for Finances page
const financesNav = [
  { icon: Wallet, label: 'Overview', path: '/finances' },
  { icon: Calculator, label: 'Tax Profiles', path: '/finances/tax-profiles' },
  { icon: DollarSign, label: 'Income/Expenses', path: '/finances/income-expenses' },
  { icon: TrendingUp, label: 'Investments', path: '/finances/investments' },
  { icon: Target, label: 'Goals', path: '/finances/goals' },
  { icon: Zap, label: 'Simulation', path: '/finances/simulation' },
];

// Layout wrapper for Finances with tab navigation
export function FinancesLayout() {
  const location = useLocation();
  const isOverview = location.pathname === '/finances' || location.pathname === '/finances/';

  return (
    <div className="space-y-4 overflow-x-hidden">
      {/* Header with Title */}
      <div>
        <h1 className="text-base md:text-lg font-bold text-text-primary">Finances</h1>
        <p className="text-text-secondary mt-0.5 text-xs">Manage your financial life</p>
      </div>

      {/* Tab Navigation */}
      <GlassCard variant="default" className="p-2">
        <nav className="flex flex-wrap gap-1">
          {financesNav.map((item) => (
            <NavLink
              key={item.path}
              to={item.path}
              end={item.path === '/finances'}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-1.5 px-3 py-2 rounded-lg transition-colors text-xs md:text-sm',
                  isActive
                    ? 'bg-accent-purple/20 text-accent-purple'
                    : 'text-text-secondary hover:text-text-primary hover:bg-background-hover'
                )
              }
            >
              <item.icon className="w-4 h-4" />
              <span className="font-medium whitespace-nowrap">{item.label}</span>
            </NavLink>
          ))}
        </nav>
      </GlassCard>

      {/* Content Area */}
      <div>
        {isOverview ? <FinancesOverview /> : <Outlet />}
      </div>
    </div>
  );
}

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

// Original Finances content renamed to FinancesOverview for the tab-based layout
export function FinancesOverview() {
  const navigate = useNavigate();
  const [isAddAccountOpen, setIsAddAccountOpen] = useState(false);
  const [isAddTransactionOpen, setIsAddTransactionOpen] = useState(false);
  const [editingAccount, setEditingAccount] = useState<Account | null>(null);
  const [loading, setLoading] = useState(true);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [netWorth, setNetWorth] = useState(0);
  const [scenarios, setScenarios] = useState<Scenario[]>([]);
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  const [chartPeriod, setChartPeriod] = useChartPeriod(); // Use shared hook
  const [showProjectionInfo, setShowProjectionInfo] = useState(false);
  const [fxRates] = useState<FxRate[]>([
    { pair: 'USD/ZAR', rate: 18.52, change: 0.15, timestamp: new Date().toISOString() },
    { pair: 'EUR/ZAR', rate: 19.85, change: -0.22, timestamp: new Date().toISOString() },
    { pair: 'BTC/ZAR', rate: 1245000, change: 2.8, timestamp: new Date().toISOString() },
  ]);

  const [netWorthHistory, setNetWorthHistory] = useState<NetWorthDataPoint[]>([]);

  // RTK Query mutations
  const [createAccount] = useCreateAccountMutation();
  const [updateAccount] = useUpdateAccountMutation();
  const [deleteAccount] = useDeleteAccountMutation();

  const handleDeleteAccount = async (accountId: string) => {
    const confirmed = await confirmToast({
      message: 'Are you sure you want to delete this account? This action cannot be undone.',
    });
    if (!confirmed) {
      return;
    }
    
    try {
      await deleteAccount(accountId).unwrap();
      // Remove from local state
      setAccounts(prev => prev.filter(a => a.id !== accountId));
      // Trigger refresh to update net worth
      setRefreshTrigger(prev => prev + 1);
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
        // Fetch accounts first
        const accountsRes = await fetch('/api/accounts', { headers });
        let calculatedNetWorth = 0;
        let mappedAccounts: Account[] = [];
        
        if (accountsRes.ok) {
          const accountsData: AccountApiResponse = await accountsRes.json();
          mappedAccounts = accountsData.data.map(a => ({
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
          
          // Calculate net worth immediately (not relying on state)
          const assets = mappedAccounts.filter(a => a.balance > 0).reduce((sum, a) => sum + a.balance, 0);
          const liabilities = mappedAccounts.filter(a => a.balance < 0).reduce((sum, a) => sum + Math.abs(a.balance), 0);
          calculatedNetWorth = assets - liabilities;
          setNetWorth(calculatedNetWorth);
        }

        // Fetch scenarios
        let baselineScenarioId: string | null = null;
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
          
          // Find baseline scenario
          const baseline = scenariosData.data.find(s => s.attributes.isBaseline);
          if (baseline) {
            baselineScenarioId = baseline.id;
          }
        }

        // Try to get projections from baseline scenario simulation
        if (baselineScenarioId) {
          try {
            // First run the simulation to get fresh projections
            await fetch(`/api/simulations/scenarios/${baselineScenarioId}/run`, {
              method: 'POST',
              headers: { ...headers, 'Content-Type': 'application/json' },
              body: JSON.stringify({ recalculateFromStart: true }),
            });
            
            // Then fetch projections
            const projectionsRes = await fetch(`/api/simulations/scenarios/${baselineScenarioId}/projections`, { headers });
            if (projectionsRes.ok) {
              const projectionsData = await projectionsRes.json();
              if (projectionsData.data?.monthlyProjections?.length > 0) {
                // Map the projections to the expected format including accounts
                const simulationProjections: NetWorthDataPoint[] = projectionsData.data.monthlyProjections.map((p: { 
                  period: string; 
                  netWorth: number;
                  accounts?: Array<{
                    accountId: string;
                    accountName: string;
                    balance: number;
                  }>;
                }) => ({
                  date: p.period,
                  value: Math.round(p.netWorth),
                  accounts: p.accounts?.map(a => ({
                    accountId: a.accountId,
                    accountName: a.accountName,
                    balance: a.balance,
                  })),
                }));
                
                // Filter based on chart period
                const monthsToShow = getMonthsForPeriod(chartPeriod);
                
                setNetWorthHistory(simulationProjections.slice(0, monthsToShow + 1));
                return;
              }
            }
          } catch (simErr) {
            console.warn('Failed to get simulation projections, falling back to simple calculation:', simErr);
          }
        }

        // Fallback: Generate simple projections based on accounts only
        // This is a simplified version without income/expenses/investments
        setNetWorthHistory(generateProjections(calculatedNetWorth, mappedAccounts, chartPeriod));
      } catch (err) {
        console.error('Failed to fetch finances data:', err);
        // On error, show empty state
        setNetWorthHistory([]);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [refreshTrigger, chartPeriod]);

  const handleAddAccount = async (data: { name: string; type: string; balance: number; currency: string; institution?: string; isLiability?: boolean; interestRateAnnual?: number }) => {
    try {
      await createAccount({
        name: data.name,
        type: data.type as Account['type'],
        balance: data.balance,
        currency: data.currency,
        institution: data.institution,
        isLiability: data.isLiability || false,
        interestRateAnnual: data.interestRateAnnual,
      }).unwrap();
      setRefreshTrigger(prev => prev + 1);
    } catch (error) {
      console.error('Failed to add account:', error);
      alert('Failed to add account');
    }
  };

  const handleUpdateAccount = async (id: string, data: { name: string; type: string; balance: number; currency: string; institution?: string; isLiability?: boolean; interestRateAnnual?: number; monthlyFee?: number }) => {
    try {
      toast.loading('Recalculating projections...', { 
        id: 'projections-calculating',
        duration: 60000
      });
      
      await updateAccount({
        id,
        name: data.name,
        type: data.type as Account['type'],
        currentBalance: data.balance,
        currency: data.currency,
        institution: data.institution,
        isLiability: data.isLiability || false,
        interestRateAnnual: data.interestRateAnnual,
        monthlyFee: data.monthlyFee,
      }).unwrap();
      setEditingAccount(null);
      setRefreshTrigger(prev => prev + 1);
    } catch (error) {
      console.error('Failed to update account:', error);
      toast.dismiss('projections-calculating');
      toast.error('Failed to update account');
    }
  };

  const handleCloseModal = () => {
    setIsAddAccountOpen(false);
    setEditingAccount(null);
  };

  // Generate future projections based on current net worth and accounts
  // This shows the current state plus projected future values based on investments, loans, etc.
  const generateProjections = (
    _currentNetWorth: number, 
    accountsList: Account[], 
    period: ChartPeriod
  ): NetWorthDataPoint[] => {
    const months = getMonthsForPeriod(period);
    const projections: NetWorthDataPoint[] = [];
    
    // Track each account's balance separately for proper compound interest
    const accountBalances = accountsList.map(account => ({
      id: account.id,
      name: account.name,
      balance: account.balance,
      interestRateAnnual: account.interestRateAnnual || 0,
      monthlyFee: account.monthlyFee || 0,
      isLiability: account.isLiability || account.balance < 0,
    }));
    
    // Start with today's date and project forward
    for (let i = 0; i <= months; i++) {
      const date = new Date();
      date.setMonth(date.getMonth() + i);
      
      // Calculate current net worth from all account balances
      const currentTotal = accountBalances.reduce((sum, acc) => sum + acc.balance, 0);
      
      projections.push({
        date: date.toISOString().slice(0, 7), // YYYY-MM format
        value: Math.round(currentTotal),
        accounts: accountBalances.map(acc => ({
          accountId: acc.id,
          accountName: acc.name,
          balance: Math.round(acc.balance),
        })),
      });
      
      // Apply monthly compound interest and fees for next month
      accountBalances.forEach(acc => {
        const monthlyRate = acc.interestRateAnnual / 100 / 12;
        
        if (acc.balance > 0) {
          // Asset: interest grows the balance
          acc.balance += acc.balance * monthlyRate;
        } else if (acc.balance < 0) {
          // Liability: interest makes debt grow (more negative)
          // Note: balance is already negative, so subtracting interest makes it more negative
          acc.balance -= Math.abs(acc.balance) * monthlyRate;
        }
        
        // Deduct monthly fees (reduces net worth)
        if (acc.monthlyFee > 0) {
          if (acc.balance > 0) {
            acc.balance -= acc.monthlyFee;
          } else {
            acc.balance -= acc.monthlyFee; // Fee adds to debt
          }
        }
      });
    }
    
    return projections;
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Spinner size="lg" />
      </div>
    );
  }

  return (
    <div className="space-y-4 overflow-x-hidden">
      {/* Action Buttons */}
      <div className="flex flex-wrap items-center gap-2">
        <CurrencySelector size="sm" />
        <Button onClick={() => setIsAddTransactionOpen(true)} variant="secondary" icon={<Plus className="w-3 h-3" />} className="text-xs px-2 py-1">
          <span className="hidden sm:inline">Transaction</span>
          <span className="sm:hidden">+</span>
        </Button>
        <Button onClick={() => setIsAddAccountOpen(true)} icon={<Plus className="w-3 h-3" />} className="text-xs px-2 py-1">
          <span className="hidden sm:inline">Account</span>
          <span className="sm:hidden">+</span>
        </Button>
      </div>

      {/* Net Worth Banner */}
      <GlassCard variant="elevated" glow="accent" className="p-3 md:p-4">
        <div className="text-center">
          <p className="text-text-secondary mb-1 text-xs">Total Net Worth</p>
          <h2 className="text-xl sm:text-2xl md:text-3xl font-bold text-text-primary mb-1">
            {formatCurrency(netWorth, 'ZAR')}
          </h2>
          <div className={cn(
            "flex items-center justify-center gap-1 text-xs",
            netWorth >= 0 ? "text-semantic-success" : "text-semantic-error"
          )}>
            {netWorth >= 0 ? <ArrowUpRight className="w-3 h-3" /> : <ArrowDownRight className="w-3 h-3" />}
            <span>Real-time from {accounts.length} accounts</span>
          </div>
        </div>
      </GlassCard>

      {/* Net Worth Chart */}
      <GlassCard variant="default" className="p-3 md:p-4">
        <div className="flex flex-col gap-2 mb-3">
          <div className="flex items-center gap-2">
            <h2 className="text-sm md:text-base font-semibold text-text-primary">Net Worth Projection</h2>
            <div className="relative">
              <button
                onClick={() => setShowProjectionInfo(!showProjectionInfo)}
                className="text-text-tertiary hover:text-text-secondary transition-colors"
                aria-label="Projection calculation info"
              >
                <Info className="w-4 h-4" />
              </button>
              {showProjectionInfo && (
                <div className="absolute left-0 top-full mt-2 z-50 w-72 p-3 rounded-lg bg-background-tertiary border border-glass-border shadow-lg text-xs text-text-secondary">
                  <h4 className="font-semibold text-text-primary mb-2">How Projections Work</h4>
                  <ul className="space-y-1.5">
                    <li><span className="text-accent-cyan">•</span> <strong>Income:</strong> All income sources are added to target accounts, with annual increases applied</li>
                    <li><span className="text-semantic-error">•</span> <strong>Expenses:</strong> Recurring and one-off expenses deducted from source accounts, with inflation adjustment</li>
                    <li><span className="text-accent-purple">•</span> <strong>Investments:</strong> Contributions transfer from source to target accounts, with compound growth</li>
                    <li><span className="text-semantic-success">•</span> <strong>Interest:</strong> Account interest rates apply monthly (daily/monthly/quarterly/annual compounding)</li>
                    <li><span className="text-semantic-warning">•</span> <strong>Growth:</strong> Investment accounts grow at their configured annual rate</li>
                    <li><span className="text-text-tertiary">•</span> <strong>Taxes:</strong> Income tax calculated using your tax profile brackets</li>
                  </ul>
                  <p className="mt-2 text-text-tertiary text-[10px]">Default inflation: 5% annually</p>
                  <button
                    onClick={() => setShowProjectionInfo(false)}
                    className="mt-2 text-accent-purple hover:text-accent-purple/80 text-xs"
                  >
                    Close
                  </button>
                </div>
              )}
            </div>
          </div>
          <PeriodSelector value={chartPeriod} onChange={setChartPeriod} />
        </div>
        <NetWorthChart data={netWorthHistory} height={240} />
      </GlassCard>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        {/* Accounts */}
        <div className="lg:col-span-2">
          <GlassCard variant="default" className="p-3 md:p-4">
            <div className="flex items-center justify-between mb-3">
              <h2 className="text-sm md:text-base font-semibold text-text-primary">Accounts</h2>
              <span className="text-text-tertiary text-xs">{accounts.length} accounts</span>
            </div>
            <div className="space-y-2">
              {accounts.length === 0 ? (
                <p className="text-text-tertiary text-center py-6 text-xs">No accounts yet. Add your first account!</p>
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
        <div className="space-y-4">
          {/* FX Rates */}
          <GlassCard variant="default" className="p-3 md:p-4">
            <div className="flex items-center gap-2 mb-3">
              <TrendingUp className="w-4 h-4 text-accent-cyan" />
              <h2 className="text-sm md:text-base font-semibold text-text-primary">Exchange Rates</h2>
            </div>
            <div className="space-y-2">
              {fxRates.map((rate) => (
                <div key={rate.pair} className="flex items-center justify-between">
                  <span className="text-text-secondary text-xs">{rate.pair}</span>
                  <div className="text-right">
                    <span className="font-medium text-text-primary text-xs">
                      {rate.rate.toLocaleString()}
                    </span>
                    <span className={cn(
                      'text-xs ml-1',
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
          <GlassCard variant="default" className="p-3 md:p-4">
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2">
                <Calculator className="w-4 h-4 text-accent-purple" />
                <h2 className="text-sm md:text-base font-semibold text-text-primary">Simulations</h2>
              </div>
              <button 
                onClick={() => navigate('/finances/simulation/new')}
                className="text-accent-purple hover:text-accent-purple/80 transition-colors"
              >
                <Plus className="w-4 h-4" />
              </button>
            </div>
            <div className="space-y-2">
              {scenarios.length === 0 ? (
                <p className="text-text-tertiary text-center py-3 text-xs">No simulations yet</p>
              ) : (
                scenarios.map((scenario) => (
                  <div
                    key={scenario.id}
                    onClick={() => navigate(`/finances/simulation/${scenario.id}`)}
                    className="p-2 rounded-lg bg-background-hover/50 hover:bg-background-hover transition-colors cursor-pointer"
                  >
                    <div className="flex items-center justify-between">
                      <span className="font-medium text-text-primary text-xs">{scenario.name}</span>
                      {scenario.isActive && (
                        <span className="text-xs px-1.5 py-0.5 rounded-full bg-semantic-success/20 text-semantic-success">
                          Baseline
                        </span>
                      )}
                    </div>
                    {scenario.description && (
                      <p className="text-xs text-text-tertiary mt-1">{scenario.description}</p>
                    )}
                  </div>
                ))
              )}
            </div>
          </GlassCard>
        </div>
      </div>

      {/* Financial Goals & Calculators Section */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
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
