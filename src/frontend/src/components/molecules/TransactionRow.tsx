import { cn } from '@utils/cn';
import type { Transaction } from '@/types';
import { formatCurrency } from './CurrencySelector';

interface TransactionRowProps {
  transaction: Transaction;
  onClick?: () => void;
  className?: string;
}

const typeColors = {
  income: 'text-semantic-success',
  expense: 'text-semantic-error',
  transfer: 'text-text-secondary',
};

const typeIcons = {
  income: '↓',
  expense: '↑',
  transfer: '↔',
};

export function TransactionRow({ transaction, onClick, className }: TransactionRowProps) {
  const formattedDate = new Date(transaction.date).toLocaleDateString('en-ZA', {
    day: 'numeric',
    month: 'short',
  });

  return (
    <div
      onClick={onClick}
      className={cn(
        'flex items-center justify-between p-3 rounded-lg',
        'hover:bg-background-hover/50 transition-colors',
        onClick && 'cursor-pointer',
        className
      )}
    >
      <div className="flex items-center gap-3">
        <div className={cn(
          'w-8 h-8 rounded-full flex items-center justify-center',
          'bg-glass-medium text-lg',
          typeColors[transaction.type]
        )}>
          {typeIcons[transaction.type]}
        </div>
        <div>
          <p className="font-medium text-text-primary">{transaction.description}</p>
          <p className="text-xs text-text-tertiary">
            {formattedDate}
            {transaction.category && ` • ${transaction.category}`}
          </p>
        </div>
      </div>
      <span className={cn('font-semibold', typeColors[transaction.type])}>
        {transaction.type === 'expense' ? '-' : transaction.type === 'income' ? '+' : ''}
        {formatCurrency(Math.abs(transaction.amount), transaction.currency)}
      </span>
    </div>
  );
}
