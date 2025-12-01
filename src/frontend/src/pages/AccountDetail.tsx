import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { NetWorthChart } from '@components/organisms/NetWorthChart';
import { TransactionRow } from '@components/molecules/TransactionRow';
import { ArrowLeft, Plus, Edit, Trash2 } from 'lucide-react';
import { cn } from '@utils/cn';
import { formatCurrency } from '@components/molecules/CurrencySelector';
import type { Account, Transaction, NetWorthDataPoint } from '@/types';
import { AddTransactionModal } from './placeholders/AddTransactionModal';

// Mock data
const mockAccount: Account = {
  id: '1',
  name: 'Main Checking',
  type: 'bank',
  balance: 125000,
  currency: 'ZAR',
  change: 2.5,
  institution: 'Standard Bank',
  lastUpdated: new Date().toISOString(),
};

const mockBalanceHistory: NetWorthDataPoint[] = [
  { date: '2024-06', value: 100000 },
  { date: '2024-07', value: 105000 },
  { date: '2024-08', value: 115000 },
  { date: '2024-09', value: 110000 },
  { date: '2024-10', value: 120000 },
  { date: '2024-11', value: 125000 },
];

const mockTransactions: Transaction[] = [
  { id: 't1', accountId: '1', amount: 45000, currency: 'ZAR', description: 'Salary', category: 'Income', date: '2024-11-25', type: 'income' },
  { id: 't2', accountId: '1', amount: 2500, currency: 'ZAR', description: 'Grocery shopping', category: 'Food', date: '2024-11-24', type: 'expense' },
  { id: 't3', accountId: '1', amount: 1200, currency: 'ZAR', description: 'Electricity', category: 'Utilities', date: '2024-11-22', type: 'expense' },
  { id: 't4', accountId: '1', amount: 5000, currency: 'ZAR', description: 'Freelance payment', category: 'Income', date: '2024-11-20', type: 'income' },
  { id: 't5', accountId: '1', amount: 350, currency: 'ZAR', description: 'Netflix', category: 'Entertainment', date: '2024-11-15', type: 'expense' },
];

export function AccountDetail() {
  const { accountId } = useParams();
  const navigate = useNavigate();
  const [isAddTransactionOpen, setIsAddTransactionOpen] = useState(false);

  // In real app, fetch account by ID using RTK Query
  const account = mockAccount;
  const transactions = mockTransactions;
  const balanceHistory = mockBalanceHistory;

  const typeIcons: Record<string, string> = {
    bank: '🏦',
    investment: '📈',
    crypto: '₿',
    credit: '💳',
    loan: '📋',
    property: '🏠',
    other: '💰',
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <button
            onClick={() => navigate('/finances')}
            className="p-2 rounded-lg hover:bg-background-hover transition-colors"
          >
            <ArrowLeft className="w-5 h-5 text-text-secondary" />
          </button>
          <div className="flex items-center gap-3">
            <span className="text-3xl">{typeIcons[account.type]}</span>
            <div>
              <h1 className="text-2xl font-bold text-text-primary">{account.name}</h1>
              <p className="text-text-secondary capitalize">{account.type} • {account.institution}</p>
            </div>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <Button variant="ghost" icon={<Edit className="w-4 h-4" />}>
            Edit
          </Button>
          <Button variant="danger" icon={<Trash2 className="w-4 h-4" />}>
            Delete
          </Button>
        </div>
      </div>

      {/* Balance Card */}
      <GlassCard variant="elevated" glow="accent" className="p-6">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-text-secondary mb-1">Current Balance</p>
            <p className={cn(
              'text-4xl font-bold',
              account.balance < 0 ? 'text-semantic-error' : 'text-text-primary'
            )}>
              {formatCurrency(account.balance, account.currency)}
            </p>
            {account.change && (
              <p className={cn(
                'text-sm mt-1',
                account.change > 0 ? 'text-semantic-success' : 'text-semantic-error'
              )}>
                {account.change > 0 ? '+' : ''}{account.change}% this month
              </p>
            )}
          </div>
          <Button onClick={() => setIsAddTransactionOpen(true)} icon={<Plus className="w-4 h-4" />}>
            Add Transaction
          </Button>
        </div>
      </GlassCard>

      {/* Balance History Chart */}
      <GlassCard variant="default" className="p-6">
        <h2 className="text-lg font-semibold text-text-primary mb-4">Balance History</h2>
        <NetWorthChart data={balanceHistory} currency={account.currency} height={250} />
      </GlassCard>

      {/* Transactions */}
      <GlassCard variant="default" className="p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-text-primary">Recent Transactions</h2>
          <span className="text-text-tertiary text-sm">{transactions.length} transactions</span>
        </div>
        <div className="space-y-1">
          {transactions.map((tx) => (
            <TransactionRow key={tx.id} transaction={tx} />
          ))}
        </div>
      </GlassCard>

      {/* Modal */}
      <AddTransactionModal 
        isOpen={isAddTransactionOpen} 
        onClose={() => setIsAddTransactionOpen(false)}
        defaultAccountId={accountId}
      />
    </div>
  );
}
