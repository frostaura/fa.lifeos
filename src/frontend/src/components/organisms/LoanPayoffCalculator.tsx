import { useState, useMemo } from 'react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Input } from '@components/atoms/Input';
import { Select } from '@components/atoms/Select';
import { Spinner } from '@components/atoms/Spinner';
import { Calculator, Calendar, Banknote, TrendingDown } from 'lucide-react';
import { formatCurrency } from '@components/molecules/CurrencySelector';
import { useGetAccountsQuery } from '@/services';
import type { Account } from '@/types';

interface PayoffResult {
  payoffDate: Date;
  totalMonths: number;
  totalInterest: number;
  interestSaved: number;
  monthsSaved: number;
  monthlyPayment: number;
}

function calculatePayoff(
  balance: number,
  annualRate: number,
  extraPayment: number = 0
): PayoffResult | null {
  if (balance <= 0 || annualRate <= 0) return null;

  const monthlyRate = annualRate / 12;  // Rate is already in decimal form (0.2175 = 21.75%)
  
  // Standard loan calculation - assume a minimum payment that covers interest + some principal
  // This is a simplified model: we'll calculate based on a 10-year term baseline
  const standardTerm = 120; // 10 years
  const standardPayment = (balance * monthlyRate * Math.pow(1 + monthlyRate, standardTerm)) / 
                          (Math.pow(1 + monthlyRate, standardTerm) - 1);
  
  const monthlyPayment = standardPayment + extraPayment;
  
  // Calculate months to payoff with current payment
  let remainingBalance = balance;
  let months = 0;
  let totalInterest = 0;
  const maxMonths = 600; // 50 years safety limit
  
  while (remainingBalance > 0 && months < maxMonths) {
    const interestCharge = remainingBalance * monthlyRate;
    totalInterest += interestCharge;
    const principalPayment = monthlyPayment - interestCharge;
    
    if (principalPayment <= 0) {
      // Payment doesn't cover interest - can't pay off
      return null;
    }
    
    remainingBalance -= principalPayment;
    months++;
    
    if (remainingBalance < 0) remainingBalance = 0;
  }
  
  // Calculate what it would have been without extra payment
  let baseRemainingBalance = balance;
  let baseMonths = 0;
  let baseTotalInterest = 0;
  
  while (baseRemainingBalance > 0 && baseMonths < maxMonths) {
    const interestCharge = baseRemainingBalance * monthlyRate;
    baseTotalInterest += interestCharge;
    const principalPayment = standardPayment - interestCharge;
    
    if (principalPayment <= 0) break;
    
    baseRemainingBalance -= principalPayment;
    baseMonths++;
    
    if (baseRemainingBalance < 0) baseRemainingBalance = 0;
  }
  
  const payoffDate = new Date();
  payoffDate.setMonth(payoffDate.getMonth() + months);
  
  return {
    payoffDate,
    totalMonths: months,
    totalInterest,
    interestSaved: baseTotalInterest - totalInterest,
    monthsSaved: baseMonths - months,
    monthlyPayment,
  };
}

export function LoanPayoffCalculator() {
  const { data: accountsData, isLoading, error } = useGetAccountsQuery();
  const [selectedAccountId, setSelectedAccountId] = useState<string>('');
  const [extraPayment, setExtraPayment] = useState('0');
  const [showResults, setShowResults] = useState(false);

  // Filter for liability accounts
  const loanAccounts = useMemo(() => {
    if (!accountsData?.data) return [];
    return accountsData.data.filter((account: Account) => account.isLiability);
  }, [accountsData]);

  const selectedAccount = loanAccounts.find(a => a.id === selectedAccountId);

  const payoffResult = useMemo(() => {
    if (!selectedAccount) return null;
    
    const balance = Math.abs(selectedAccount.balance);
    const rate = selectedAccount.interestRateAnnual || 0;
    const extra = parseFloat(extraPayment) || 0;
    
    return calculatePayoff(balance, rate, extra);
  }, [selectedAccount, extraPayment]);

  const handleCalculate = () => {
    setShowResults(true);
  };

  if (isLoading) {
    return (
      <GlassCard variant="default" className="p-6">
        <div className="flex items-center justify-center h-40">
          <Spinner size="lg" />
        </div>
      </GlassCard>
    );
  }

  if (error) {
    return (
      <GlassCard variant="default" className="p-6">
        <p className="text-semantic-error">Failed to load accounts</p>
      </GlassCard>
    );
  }

  const accountOptions = loanAccounts.map(account => ({
    value: account.id,
    label: `${account.name} (${formatCurrency(Math.abs(account.balance), account.currency)})`,
  }));

  return (
    <GlassCard variant="default" className="p-6">
      <div className="flex items-center gap-2 mb-4">
        <Calculator className="w-5 h-5 text-accent-purple" />
        <h2 className="text-lg font-semibold text-text-primary">Loan Payoff Calculator</h2>
      </div>

      {loanAccounts.length === 0 ? (
        <p className="text-text-tertiary text-center py-8">
          No loan accounts found. Add a liability account to use this calculator.
        </p>
      ) : (
        <>
          <div className="space-y-4 mb-4">
            <Select
              label="Select Loan Account"
              options={accountOptions}
              value={selectedAccountId}
              onChange={(e) => {
                setSelectedAccountId(e.target.value);
                setShowResults(false);
              }}
              placeholder="Choose a loan..."
            />

            {selectedAccount && (
              <>
                <div className="grid grid-cols-2 gap-4 p-3 rounded-lg bg-background-hover/50">
                  <div>
                    <p className="text-xs text-text-tertiary">Current Balance</p>
                    <p className="text-lg font-semibold text-text-primary">
                      {formatCurrency(Math.abs(selectedAccount.balance), selectedAccount.currency)}
                    </p>
                  </div>
                  <div>
                    <p className="text-xs text-text-tertiary">Interest Rate</p>
                    <p className="text-lg font-semibold text-text-primary">
                      {(selectedAccount.interestRateAnnual || 0).toFixed(2)}%
                    </p>
                  </div>
                </div>

                <Input
                  label="Extra Monthly Payment (Optional)"
                  type="number"
                  step="100"
                  min="0"
                  placeholder="0"
                  value={extraPayment}
                  onChange={(e) => {
                    setExtraPayment(e.target.value);
                    setShowResults(false);
                  }}
                />

                <Button onClick={handleCalculate} className="w-full">
                  Calculate Payoff
                </Button>
              </>
            )}
          </div>

          {/* Results */}
          {showResults && payoffResult && (
            <div className="space-y-3 pt-4 border-t border-glass-border">
              <h3 className="text-sm font-medium text-text-secondary">Payoff Analysis</h3>
              
              <div className="grid grid-cols-2 gap-3">
                <div className="p-3 rounded-lg bg-background-hover/50">
                  <div className="flex items-center gap-2 mb-1">
                    <Calendar className="w-4 h-4 text-accent-cyan" />
                    <p className="text-xs text-text-tertiary">Payoff Date</p>
                  </div>
                  <p className="text-lg font-semibold text-text-primary">
                    {payoffResult.payoffDate.toLocaleDateString('en-ZA', { 
                      month: 'short', 
                      year: 'numeric' 
                    })}
                  </p>
                  <p className="text-xs text-text-tertiary">
                    {payoffResult.totalMonths} months
                  </p>
                </div>

                <div className="p-3 rounded-lg bg-background-hover/50">
                  <div className="flex items-center gap-2 mb-1">
                    <Banknote className="w-4 h-4 text-accent-purple" />
                    <p className="text-xs text-text-tertiary">Monthly Payment</p>
                  </div>
                  <p className="text-lg font-semibold text-text-primary">
                    {formatCurrency(payoffResult.monthlyPayment)}
                  </p>
                </div>

                <div className="p-3 rounded-lg bg-background-hover/50">
                  <div className="flex items-center gap-2 mb-1">
                    <TrendingDown className="w-4 h-4 text-semantic-error" />
                    <p className="text-xs text-text-tertiary">Total Interest</p>
                  </div>
                  <p className="text-lg font-semibold text-semantic-error">
                    {formatCurrency(payoffResult.totalInterest)}
                  </p>
                </div>

                {payoffResult.interestSaved > 0 && (
                  <div className="p-3 rounded-lg bg-semantic-success/10 border border-semantic-success/30">
                    <div className="flex items-center gap-2 mb-1">
                      <TrendingDown className="w-4 h-4 text-semantic-success" />
                      <p className="text-xs text-text-tertiary">Interest Saved</p>
                    </div>
                    <p className="text-lg font-semibold text-semantic-success">
                      {formatCurrency(payoffResult.interestSaved)}
                    </p>
                    <p className="text-xs text-semantic-success">
                      {payoffResult.monthsSaved} months earlier
                    </p>
                  </div>
                )}
              </div>
            </div>
          )}

          {showResults && !payoffResult && selectedAccount && (
            <div className="p-4 rounded-lg bg-semantic-warning/10 border border-semantic-warning/30">
              <p className="text-semantic-warning text-sm">
                Unable to calculate payoff. Please ensure the loan has a valid balance and interest rate.
              </p>
            </div>
          )}
        </>
      )}
    </GlassCard>
  );
}
