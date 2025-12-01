import { useEffect, useRef, useState } from 'react';
import { X } from 'lucide-react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Input } from '@components/atoms/Input';
import { Select } from '@components/atoms/Select';
import { CURRENCIES, type FutureEvent, type EventType } from '@/types';

interface FutureEventsEditorProps {
  onAdd: (event: Omit<FutureEvent, 'id' | 'scenarioId'>) => void;
  onClose: () => void;
}

const eventTypes: { value: EventType; label: string }[] = [
  { value: 'income_change', label: 'Income Change' },
  { value: 'expense_change', label: 'Expense Change' },
  { value: 'one_time_income', label: 'One-time Income' },
  { value: 'one_time_expense', label: 'One-time Expense' },
  { value: 'asset_purchase', label: 'Asset Purchase' },
  { value: 'asset_sale', label: 'Asset Sale' },
  { value: 'market_adjustment', label: 'Market Adjustment' },
];

const frequencyOptions = [
  { value: 'monthly', label: 'Monthly' },
  { value: 'quarterly', label: 'Quarterly' },
  { value: 'yearly', label: 'Yearly' },
];

export function FutureEventsEditor({ onAdd, onClose }: FutureEventsEditorProps) {
  const modalRef = useRef<HTMLDivElement>(null);
  const mouseDownTargetRef = useRef<EventTarget | null>(null);
  const [name, setName] = useState('');
  const [type, setType] = useState<EventType>('one_time_income');
  const [date, setDate] = useState('');
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState('ZAR');
  const [isRecurring, setIsRecurring] = useState(false);
  const [recurringFrequency, setRecurringFrequency] = useState<'monthly' | 'quarterly' | 'yearly'>('monthly');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    document.body.style.overflow = 'hidden';
    return () => {
      document.body.style.overflow = '';
    };
  }, []);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const newErrors: Record<string, string> = {};
    
    if (!name.trim()) newErrors.name = 'Event name is required';
    if (!date) newErrors.date = 'Date is required';
    if (!amount || parseFloat(amount) <= 0) newErrors.amount = 'Amount must be positive';
    
    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    setIsSubmitting(true);
    onAdd({
      name,
      type,
      date,
      amount: parseFloat(amount) || 0,
      currency,
      isRecurring,
      recurringFrequency: isRecurring ? recurringFrequency : undefined,
    });
    setIsSubmitting(false);
    onClose();
  };

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
          <h2 className="text-xl font-semibold text-text-primary">Add Future Event</h2>
          <button
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-background-hover transition-colors"
          >
            <X className="w-5 h-5 text-text-secondary" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Event Name"
            placeholder="e.g., Annual Bonus"
            value={name}
            onChange={(e) => setName(e.target.value)}
            error={errors.name}
          />

          <Select
            label="Event Type"
            options={eventTypes}
            value={type}
            onChange={(e) => setType(e.target.value as EventType)}
          />

          <Input
            label="Event Date"
            type="date"
            value={date}
            onChange={(e) => setDate(e.target.value)}
            error={errors.date}
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

          <label className="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              className="w-4 h-4 rounded border-glass-border bg-glass-medium text-accent-purple focus:ring-accent-purple"
              checked={isRecurring}
              onChange={(e) => setIsRecurring(e.target.checked)}
            />
            <span className="text-text-primary">Recurring event</span>
          </label>

          {isRecurring && (
            <Select
              label="Frequency"
              options={frequencyOptions}
              value={recurringFrequency}
              onChange={(e) => setRecurringFrequency(e.target.value as 'monthly' | 'quarterly' | 'yearly')}
            />
          )}

          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="ghost" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" loading={isSubmitting}>
              Add Event
            </Button>
          </div>
        </form>
      </GlassCard>
    </div>
  );
}
