import { ArrowUpRight, ArrowDownRight, Minus, Pencil, Trash2 } from 'lucide-react';
import { cn } from '@utils/cn';
import type { Account } from '@/types';
import { formatCurrency } from './CurrencySelector';

interface AccountRowProps {
  account: Account;
  onClick?: () => void;
  onEdit?: () => void;
  onDelete?: () => void;
  className?: string;
}

const typeIcons: Record<string, string> = {
  bank: '🏦',
  investment: '📈',
  crypto: '₿',
  credit: '💳',
  loan: '📋',
  property: '🏠',
  other: '💰',
};

export function AccountRow({ account, onClick, onEdit, onDelete, className }: AccountRowProps) {
  const TrendIcon = account.change && account.change > 0 
    ? ArrowUpRight 
    : account.change && account.change < 0 
    ? ArrowDownRight 
    : Minus;

  const trendColor = account.change && account.change > 0
    ? 'text-semantic-success'
    : account.change && account.change < 0
    ? 'text-semantic-error'
    : 'text-text-tertiary';

  return (
    <div
      className={cn(
        'flex items-center justify-between p-4 rounded-lg',
        'bg-background-hover/50 hover:bg-background-hover transition-colors',
        className
      )}
    >
      <div 
        className={cn("flex items-center gap-4 flex-1", onClick && "cursor-pointer")}
        onClick={onClick}
        role={onClick ? 'button' : undefined}
        tabIndex={onClick ? 0 : undefined}
      >
        <div className="text-2xl">{typeIcons[account.type] || '💰'}</div>
        <div>
          <p className="font-medium text-text-primary">{account.name}</p>
          <div className="flex items-center gap-2">
            <p className="text-sm text-text-tertiary capitalize">
              {account.type}
              {account.institution && (
                <span className="text-text-muted"> · {account.institution}</span>
              )}
            </p>
            {account.isLiability && account.interestRateAnnual && (
              <span className="text-xs px-1.5 py-0.5 rounded bg-red-500/20 text-red-400">
                {account.interestRateAnnual.toFixed(2)}% p.a.
              </span>
            )}
          </div>
        </div>
      </div>
      <div className="flex items-center gap-4">
        <div className="text-right">
          <p className={cn(
            'font-semibold',
            account.balance < 0 ? 'text-semantic-error' : 'text-text-primary'
          )}>
            {formatCurrency(account.balance, account.currency)}
          </p>
          {(account.monthlyInterest && account.monthlyInterest > 0) || (account.monthlyFee && account.monthlyFee > 0) ? (
            <p className="text-xs text-red-400">
              {account.monthlyInterest && account.monthlyInterest > 0 && (
                <span>~{formatCurrency(account.monthlyInterest, account.currency)}/mo interest</span>
              )}
              {account.monthlyInterest && account.monthlyInterest > 0 && account.monthlyFee && account.monthlyFee > 0 && ' + '}
              {account.monthlyFee && account.monthlyFee > 0 && (
                <span>{formatCurrency(account.monthlyFee, account.currency)}/mo fee</span>
              )}
            </p>
          ) : (
            account.change !== undefined && account.change !== 0 && (
              <div className={cn('flex items-center justify-end gap-1 text-sm', trendColor)}>
                <TrendIcon className="w-3 h-3" />
                <span>{Math.abs(account.change)}%</span>
              </div>
            )
          )}
        </div>
        {(onEdit || onDelete) && (
          <div className="flex items-center gap-1">
            {onEdit && (
              <button
                onClick={(e) => { e.stopPropagation(); onEdit(); }}
                className="p-2 text-text-secondary hover:text-text-primary hover:bg-background-primary rounded transition-colors"
                title="Edit account"
              >
                <Pencil className="w-4 h-4" />
              </button>
            )}
            {onDelete && (
              <button
                onClick={(e) => { e.stopPropagation(); onDelete(); }}
                className="p-2 text-red-400 hover:bg-red-400/10 rounded transition-colors"
                title="Delete account"
              >
                <Trash2 className="w-4 h-4" />
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
