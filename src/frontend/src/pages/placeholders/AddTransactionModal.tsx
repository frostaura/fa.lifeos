import { useEffect, useRef, useState } from 'react';
import { X } from 'lucide-react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Input } from '@components/atoms/Input';
import { Select } from '@components/atoms/Select';
import { CURRENCIES } from '@/types';

interface TransactionFormData {
  description: string;
  amount: number;
  currency: string;
  type: 'income' | 'expense' | 'transfer';
  category?: string;
  date: string;
  accountId?: string;
}

interface AddTransactionModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit?: (data: TransactionFormData) => void;
  defaultAccountId?: string;
}

const transactionTypes = [
  { value: 'income', label: 'Income' },
  { value: 'expense', label: 'Expense' },
  { value: 'transfer', label: 'Transfer' },
];

const categories = [
  { value: 'salary', label: 'Salary' },
  { value: 'investment', label: 'Investment' },
  { value: 'food', label: 'Food & Dining' },
  { value: 'transport', label: 'Transport' },
  { value: 'utilities', label: 'Utilities' },
  { value: 'entertainment', label: 'Entertainment' },
  { value: 'shopping', label: 'Shopping' },
  { value: 'health', label: 'Health' },
  { value: 'other', label: 'Other' },
];

export function AddTransactionModal({ 
  isOpen, 
  onClose, 
  onSubmit,
  defaultAccountId,
}: AddTransactionModalProps) {
  const modalRef = useRef<HTMLDivElement>(null);
  const mouseDownTargetRef = useRef<EventTarget | null>(null);
  const [description, setDescription] = useState('');
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState('ZAR');
  const [type, setType] = useState<'income' | 'expense' | 'transfer'>('expense');
  const [category, setCategory] = useState('');
  const [date, setDate] = useState(new Date().toISOString().split('T')[0]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
      // Reset form
      setDescription('');
      setAmount('');
      setCurrency('ZAR');
      setType('expense');
      setCategory('');
      setDate(new Date().toISOString().split('T')[0]);
      setErrors({});
    }
    return () => {
      document.body.style.overflow = '';
    };
  }, [isOpen]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const newErrors: Record<string, string> = {};
    
    if (!description.trim()) newErrors.description = 'Description is required';
    if (!amount || parseFloat(amount) <= 0) newErrors.amount = 'Amount must be positive';
    if (!date) newErrors.date = 'Date is required';
    
    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    setIsSubmitting(true);
    onSubmit?.({
      description,
      amount: parseFloat(amount) || 0,
      currency,
      type,
      category: category || undefined,
      date,
      accountId: defaultAccountId,
    });
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
          <h2 className="text-xl font-semibold text-text-primary">Add Transaction</h2>
          <button
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-background-hover transition-colors"
          >
            <X className="w-5 h-5 text-text-secondary" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Description"
            placeholder="e.g., Grocery shopping"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            error={errors.description}
          />

          <Select
            label="Type"
            options={transactionTypes}
            value={type}
            onChange={(e) => setType(e.target.value as 'income' | 'expense' | 'transfer')}
          />

          <div className="grid grid-cols-2 gap-4">
            <Input
              label="Amount"
              type="number"
              step="0.01"
              placeholder="0.00"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              error={errors.amount}
            />

            <Select
              label="Currency"
              options={CURRENCIES.map(c => ({ value: c.value, label: `${c.symbol} ${c.value}` }))}
              value={currency}
              onChange={(e) => setCurrency(e.target.value)}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <Select
              label="Category"
              options={categories}
              value={category}
              onChange={(e) => setCategory(e.target.value)}
            />

            <Input
              label="Date"
              type="date"
              value={date}
              onChange={(e) => setDate(e.target.value)}
              error={errors.date}
            />
          </div>

          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="ghost" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" loading={isSubmitting}>
              Add Transaction
            </Button>
          </div>
        </form>
      </GlassCard>
    </div>
  );
}
