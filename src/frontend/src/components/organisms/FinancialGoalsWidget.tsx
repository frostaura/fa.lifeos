import { useState, useEffect, useRef } from 'react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Input } from '@components/atoms/Input';
import { ProgressBar } from '@components/atoms/ProgressBar';
import { Spinner } from '@components/atoms/Spinner';
import { Plus, Target, X, Trash2, Edit2 } from 'lucide-react';
import { cn } from '@utils/cn';
import { formatCurrency } from '@components/molecules/CurrencySelector';
import {
  useGetFinancialGoalsQuery,
  useCreateFinancialGoalMutation,
  useUpdateFinancialGoalMutation,
  useDeleteFinancialGoalMutation,
} from '@/services';
import type { FinancialGoal } from '@/types';

interface AddGoalModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: { name: string; targetAmount: number; currentAmount: number; targetDate?: string }) => void;
  isSubmitting: boolean;
}

function AddGoalModal({ isOpen, onClose, onSubmit, isSubmitting }: AddGoalModalProps) {
  const modalRef = useRef<HTMLDivElement>(null);
  const mouseDownTargetRef = useRef<EventTarget | null>(null);
  const [name, setName] = useState('');
  const [targetAmount, setTargetAmount] = useState('');
  const [currentAmount, setCurrentAmount] = useState('0');
  const [targetDate, setTargetDate] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
    return () => {
      document.body.style.overflow = '';
    };
  }, [isOpen]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const newErrors: Record<string, string> = {};
    
    if (!name.trim()) newErrors.name = 'Goal name is required';
    if (!targetAmount || parseFloat(targetAmount) <= 0) newErrors.targetAmount = 'Target amount is required';
    
    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    onSubmit({
      name,
      targetAmount: parseFloat(targetAmount),
      currentAmount: parseFloat(currentAmount) || 0,
      targetDate: targetDate || undefined,
    });
  };

  const handleClose = () => {
    setName('');
    setTargetAmount('');
    setCurrentAmount('0');
    setTargetDate('');
    setErrors({});
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
          handleClose();
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
          <h2 className="text-xl font-semibold text-text-primary">Add Financial Goal</h2>
          <button
            onClick={handleClose}
            className="p-2 rounded-lg hover:bg-background-hover transition-colors"
          >
            <X className="w-5 h-5 text-text-secondary" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Goal Name"
            placeholder="e.g., Emergency Fund"
            value={name}
            onChange={(e) => setName(e.target.value)}
            error={errors.name}
          />

          <div className="grid grid-cols-2 gap-4">
            <Input
              label="Target Amount"
              type="number"
              step="0.01"
              placeholder="100000"
              value={targetAmount}
              onChange={(e) => setTargetAmount(e.target.value)}
              error={errors.targetAmount}
            />

            <Input
              label="Current Amount"
              type="number"
              step="0.01"
              placeholder="0"
              value={currentAmount}
              onChange={(e) => setCurrentAmount(e.target.value)}
            />
          </div>

          <Input
            label="Target Date (Optional)"
            type="date"
            value={targetDate}
            onChange={(e) => setTargetDate(e.target.value)}
          />

          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="ghost" onClick={handleClose}>
              Cancel
            </Button>
            <Button type="submit" loading={isSubmitting}>
              Add Goal
            </Button>
          </div>
        </form>
      </GlassCard>
    </div>
  );
}

interface UpdateProgressModalProps {
  isOpen: boolean;
  onClose: () => void;
  goal: FinancialGoal | null;
  onSubmit: (goalId: string, newAmount: number) => void;
  isSubmitting: boolean;
}

function UpdateProgressModal({ isOpen, onClose, goal, onSubmit, isSubmitting }: UpdateProgressModalProps) {
  const mouseDownTargetRef = useRef<EventTarget | null>(null);
  // Initialize amount from goal when available
  const initialAmount = goal?.currentAmount.toString() ?? '';
  const [amount, setAmount] = useState(initialAmount);
  
  // Track goal ID changes to reset amount
  const prevGoalId = useRef(goal?.id);
  if (goal?.id !== prevGoalId.current) {
    prevGoalId.current = goal?.id;
    if (goal) {
      // Reset amount when goal changes during render
      setTimeout(() => setAmount(goal.currentAmount.toString()), 0);
    }
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (goal && amount) {
      onSubmit(goal.id, parseFloat(amount));
    }
  };
  
  const handleClose = () => {
    setAmount('');
    onClose();
  };

  if (!isOpen || !goal) return null;

  return (
    <div 
      className="fixed inset-0 z-50 flex items-center justify-center"
      onMouseDown={(e) => {
        mouseDownTargetRef.current = e.target;
      }}
      onClick={(e) => {
        if (e.target === e.currentTarget && mouseDownTargetRef.current === e.target) {
          handleClose();
        }
        mouseDownTargetRef.current = null;
      }}
    >
      <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" />
      
      <GlassCard variant="elevated" className="relative z-10 w-full max-w-md mx-4 p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-xl font-semibold text-text-primary">Update Progress</h2>
          <button onClick={handleClose} className="p-2 rounded-lg hover:bg-background-hover transition-colors">
            <X className="w-5 h-5 text-text-secondary" />
          </button>
        </div>

        <p className="text-text-secondary mb-4">{goal.name}</p>

        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Current Amount"
            type="number"
            step="0.01"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
          />

          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="ghost" onClick={handleClose}>Cancel</Button>
            <Button type="submit" loading={isSubmitting}>Update</Button>
          </div>
        </form>
      </GlassCard>
    </div>
  );
}

export function FinancialGoalsWidget() {
  const { data: goalsData, isLoading, error } = useGetFinancialGoalsQuery();
  const [createGoal, { isLoading: isCreating }] = useCreateFinancialGoalMutation();
  const [updateGoal, { isLoading: isUpdating }] = useUpdateFinancialGoalMutation();
  const [deleteGoal] = useDeleteFinancialGoalMutation();
  
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [isUpdateModalOpen, setIsUpdateModalOpen] = useState(false);
  const [selectedGoal, setSelectedGoal] = useState<FinancialGoal | null>(null);

  const handleAddGoal = async (data: { name: string; targetAmount: number; currentAmount: number; targetDate?: string }) => {
    try {
      await createGoal({
        name: data.name,
        targetAmount: data.targetAmount,
        currentAmount: data.currentAmount,
        targetDate: data.targetDate,
      }).unwrap();
      setIsAddModalOpen(false);
    } catch (err) {
      console.error('Failed to create goal:', err);
    }
  };

  const handleUpdateProgress = async (goalId: string, newAmount: number) => {
    try {
      await updateGoal({ id: goalId, currentAmount: newAmount }).unwrap();
      setIsUpdateModalOpen(false);
      setSelectedGoal(null);
    } catch (err) {
      console.error('Failed to update goal:', err);
    }
  };

  const handleDeleteGoal = async (goalId: string) => {
    if (window.confirm('Are you sure you want to delete this goal?')) {
      try {
        await deleteGoal(goalId).unwrap();
      } catch (err) {
        console.error('Failed to delete goal:', err);
      }
    }
  };

  const openUpdateModal = (goal: FinancialGoal) => {
    setSelectedGoal(goal);
    setIsUpdateModalOpen(true);
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
        <p className="text-semantic-error">Failed to load financial goals</p>
      </GlassCard>
    );
  }

  const goals = goalsData?.goals || [];
  const summary = goalsData?.summary;

  return (
    <>
      <GlassCard variant="default" className="p-6">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Target className="w-5 h-5 text-accent-cyan" />
            <h2 className="text-lg font-semibold text-text-primary">Financial Goals</h2>
          </div>
          <button
            onClick={() => setIsAddModalOpen(true)}
            className="text-accent-purple hover:text-accent-purple/80 transition-colors"
          >
            <Plus className="w-5 h-5" />
          </button>
        </div>

        {/* Summary */}
        {summary && (
          <div className="mb-4 p-3 rounded-lg bg-background-hover/50">
            <div className="flex items-center justify-between text-sm">
              <span className="text-text-secondary">Overall Progress</span>
              <span className="text-text-primary font-medium">
                {formatCurrency(summary.totalCurrentAmount)} / {formatCurrency(summary.totalTargetAmount)}
              </span>
            </div>
            <ProgressBar 
              value={summary.overallProgressPercent} 
              color="#06b6d4" 
              height="sm" 
              className="mt-2"
            />
            <p className="text-xs text-text-tertiary mt-1">
              {summary.overallProgressPercent.toFixed(1)}% complete
            </p>
          </div>
        )}

        {/* Goals List */}
        <div className="space-y-3">
          {goals.length === 0 ? (
            <p className="text-text-tertiary text-center py-8">No goals yet. Add your first financial goal!</p>
          ) : (
            goals.map((goal) => (
              <div 
                key={goal.id} 
                className="p-3 rounded-lg bg-background-hover/30 hover:bg-background-hover/50 transition-colors"
              >
                <div className="flex items-center justify-between mb-2">
                  <span className="font-medium text-text-primary">{goal.name}</span>
                  <div className="flex items-center gap-2">
                    <button 
                      onClick={() => openUpdateModal(goal)}
                      className="p-1 hover:bg-background-hover rounded transition-colors"
                    >
                      <Edit2 className="w-4 h-4 text-text-tertiary" />
                    </button>
                    <button 
                      onClick={() => handleDeleteGoal(goal.id)}
                      className="p-1 hover:bg-semantic-error/20 rounded transition-colors"
                    >
                      <Trash2 className="w-4 h-4 text-text-tertiary hover:text-semantic-error" />
                    </button>
                  </div>
                </div>
                
                <div className="flex items-center justify-between text-sm mb-1">
                  <span className="text-text-secondary">
                    {formatCurrency(goal.currentAmount, goal.currency)} / {formatCurrency(goal.targetAmount, goal.currency)}
                  </span>
                  <span className={cn(
                    'text-xs',
                    goal.progressPercent >= 100 ? 'text-semantic-success' : 'text-text-tertiary'
                  )}>
                    {goal.progressPercent.toFixed(0)}%
                  </span>
                </div>
                
                <ProgressBar 
                  value={goal.progressPercent} 
                  color={goal.progressPercent >= 100 ? '#22c55e' : '#8b5cf6'} 
                  height="sm"
                />
                
                {goal.monthsToAcquire !== undefined && goal.monthsToAcquire > 0 && (
                  <p className="text-xs text-text-tertiary mt-1">
                    ~{goal.monthsToAcquire} months to acquire
                  </p>
                )}
              </div>
            ))
          )}
        </div>
      </GlassCard>

      <AddGoalModal
        isOpen={isAddModalOpen}
        onClose={() => setIsAddModalOpen(false)}
        onSubmit={handleAddGoal}
        isSubmitting={isCreating}
      />

      <UpdateProgressModal
        isOpen={isUpdateModalOpen}
        onClose={() => {
          setIsUpdateModalOpen(false);
          setSelectedGoal(null);
        }}
        goal={selectedGoal}
        onSubmit={handleUpdateProgress}
        isSubmitting={isUpdating}
      />
    </>
  );
}
