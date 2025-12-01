import { useEffect, useRef, useState } from 'react';
import { X } from 'lucide-react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Input } from '@components/atoms/Input';
import { Select } from '@components/atoms/Select';
import { CURRENCIES, type AccountType, type Account } from '@/types';

interface AccountFormData {
  name: string;
  type: AccountType;
  balance: number;
  currency: string;
  institution?: string;
  isLiability?: boolean;
  interestRateAnnual?: number;
  monthlyFee?: number;
}

interface AddAccountModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit?: (data: AccountFormData) => void;
  editingAccount?: Account | null;
  onUpdate?: (id: string, data: AccountFormData) => void;
}

const accountTypes: { value: AccountType; label: string }[] = [
  { value: 'bank', label: 'Bank Account' },
  { value: 'investment', label: 'Investment' },
  { value: 'crypto', label: 'Cryptocurrency' },
  { value: 'credit', label: 'Credit Card' },
  { value: 'loan', label: 'Loan' },
  { value: 'property', label: 'Property' },
  { value: 'other', label: 'Other' },
];

export function AddAccountModal({ isOpen, onClose, onSubmit, editingAccount, onUpdate }: AddAccountModalProps) {
  const modalRef = useRef<HTMLDivElement>(null);
  const mouseDownTargetRef = useRef<EventTarget | null>(null);
  const [name, setName] = useState('');
  const [type, setType] = useState<AccountType>('bank');
  const [balance, setBalance] = useState('0');
  const [currency, setCurrency] = useState('ZAR');
  const [institution, setInstitution] = useState('');
  const [isLiability, setIsLiability] = useState(false);
  const [interestRate, setInterestRate] = useState('');
  const [monthlyFee, setMonthlyFee] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const isEditing = !!editingAccount;

  useEffect(() => {
    if (isOpen && editingAccount) {
      // Populate form with existing account data
      setName(editingAccount.name);
      setType(editingAccount.type);
      setBalance(Math.abs(editingAccount.balance).toString());
      setCurrency(editingAccount.currency);
      setInstitution(editingAccount.institution || '');
      setIsLiability(editingAccount.isLiability || false);
      // Interest rate is already stored as percentage (21.75 = 21.75%)
      setInterestRate(editingAccount.interestRateAnnual ? editingAccount.interestRateAnnual.toString() : '');
      setMonthlyFee(editingAccount.monthlyFee?.toString() || '');
    } else if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
      // Reset form
      setName('');
      setType('bank');
      setBalance('0');
      setCurrency('ZAR');
      setInstitution('');
      setIsLiability(false);
      setInterestRate('');
      setMonthlyFee('');
      setErrors({});
    }
    return () => {
      document.body.style.overflow = '';
    };
  }, [isOpen, editingAccount]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const newErrors: Record<string, string> = {};
    
    if (!name.trim()) newErrors.name = 'Account name is required';
    if (!currency) newErrors.currency = 'Currency is required';
    
    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    const formData: AccountFormData = {
      name,
      type,
      balance: parseFloat(balance) || 0,
      currency,
      institution: institution || undefined,
      isLiability,
      // Interest rate is stored as percentage (21.75 = 21.75%)
      interestRateAnnual: interestRate ? parseFloat(interestRate) : undefined,
      monthlyFee: monthlyFee ? parseFloat(monthlyFee) : undefined,
    };

    setIsSubmitting(true);
    
    if (isEditing && editingAccount && onUpdate) {
      onUpdate(editingAccount.id, formData);
    } else {
      onSubmit?.(formData);
    }
    
    setIsSubmitting(false);
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div 
      className="fixed inset-0 z-50 flex items-center justify-center"
      onMouseDown={(e) => {
        mouseDownTargetRef.current = e.target;
      }}
      onClick={(e) => {
        // Only close if both mousedown and mouseup happened on the backdrop
        // This prevents closing when dragging text selection from modal to backdrop
        if (e.target === e.currentTarget && mouseDownTargetRef.current === e.target) {
          onClose();
        }
        mouseDownTargetRef.current = null;
      }}
    >
      <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" />
      
      <GlassCard
        ref={modalRef}
        variant="elevated"
        className="relative z-10 w-full max-w-lg mx-4 p-6"
      >
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-xl font-semibold text-text-primary">
            {isEditing ? 'Edit Account' : 'Add Account'}
          </h2>
          <button
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-background-hover transition-colors"
          >
            <X className="w-5 h-5 text-text-secondary" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Account Name"
            placeholder="e.g., Main Savings"
            value={name}
            onChange={(e) => setName(e.target.value)}
            error={errors.name}
          />

          <Select
            label="Account Type"
            options={accountTypes}
            value={type}
            onChange={(e) => setType(e.target.value as AccountType)}
          />

          <div className="grid grid-cols-2 gap-4">
            <Input
              label="Balance"
              type="number"
              step="0.01"
              placeholder="0.00"
              value={balance}
              onChange={(e) => setBalance(e.target.value)}
            />

            <Select
              label="Currency"
              options={CURRENCIES.map(c => ({ value: c.value, label: `${c.symbol} ${c.value}` }))}
              value={currency}
              onChange={(e) => setCurrency(e.target.value)}
              error={errors.currency}
            />
          </div>

          <Input
            label="Institution (Optional)"
            placeholder="e.g., Standard Bank"
            value={institution}
            onChange={(e) => setInstitution(e.target.value)}
          />

          <div className="grid grid-cols-2 gap-4">
            <Input
              label="Interest Rate % (Optional)"
              type="number"
              step="0.01"
              placeholder="e.g., 10.5"
              value={interestRate}
              onChange={(e) => setInterestRate(e.target.value)}
            />

            <Input
              label="Monthly Fee (Optional)"
              type="number"
              step="0.01"
              placeholder="e.g., 50"
              value={monthlyFee}
              onChange={(e) => setMonthlyFee(e.target.value)}
            />
          </div>

          <div className="flex items-center">
            <label className="flex items-center gap-3 cursor-pointer">
              <input
                type="checkbox"
                checked={isLiability}
                onChange={(e) => setIsLiability(e.target.checked)}
                className="w-5 h-5 rounded border-white/20 bg-background-secondary text-accent-primary focus:ring-accent-primary focus:ring-offset-0"
              />
              <span className="text-sm text-text-primary">This is a liability (debt)</span>
            </label>
          </div>

          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="ghost" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" loading={isSubmitting}>
              {isEditing ? 'Update Account' : 'Add Account'}
            </Button>
          </div>
        </form>
      </GlassCard>
    </div>
  );
}
