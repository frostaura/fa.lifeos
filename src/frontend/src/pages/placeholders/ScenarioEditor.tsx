import { useForm } from 'react-hook-form';
import { useRef } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { X } from 'lucide-react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Input } from '@components/atoms/Input';
import type { Scenario } from '@/types';

const scenarioSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  description: z.string().optional(),
  startDate: z.string().min(1, 'Start date is required'),
  endDate: z.string().min(1, 'End date is required'),
  isActive: z.boolean(),
});

type ScenarioFormData = z.infer<typeof scenarioSchema>;

interface ScenarioEditorProps {
  scenario?: Scenario;
  onSave: (data: Partial<Scenario>) => void;
  onClose: () => void;
}

export function ScenarioEditor({ scenario, onSave, onClose }: ScenarioEditorProps) {
  const mouseDownTargetRef = useRef<EventTarget | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ScenarioFormData>({
    resolver: zodResolver(scenarioSchema),
    defaultValues: {
      name: scenario?.name || '',
      description: scenario?.description || '',
      startDate: scenario?.startDate || new Date().toISOString().split('T')[0],
      endDate: scenario?.endDate || '',
      isActive: scenario?.isActive ?? false,
    },
  });

  const handleFormSubmit = (data: ScenarioFormData) => {
    onSave(data);
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
      
      <GlassCard variant="elevated" className="relative z-10 w-full max-w-lg mx-4 p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-xl font-semibold text-text-primary">
            {scenario ? 'Edit Scenario' : 'New Scenario'}
          </h2>
          <button
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-background-hover transition-colors"
          >
            <X className="w-5 h-5 text-text-secondary" />
          </button>
        </div>

        <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">
          <Input
            label="Scenario Name"
            placeholder="e.g., Early Retirement Plan"
            error={errors.name?.message}
            {...register('name')}
          />

          <div>
            <label className="block text-sm font-medium text-text-secondary mb-1.5">
              Description
            </label>
            <textarea
              className="w-full px-3 py-2 rounded-lg bg-glass-medium border border-glass-border text-text-primary placeholder-text-tertiary focus:outline-none focus:ring-2 focus:ring-accent-purple/50 focus:border-accent-purple transition-colors resize-none"
              rows={3}
              placeholder="Describe your financial goal..."
              {...register('description')}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <Input
              label="Start Date"
              type="date"
              error={errors.startDate?.message}
              {...register('startDate')}
            />
            <Input
              label="End Date"
              type="date"
              error={errors.endDate?.message}
              {...register('endDate')}
            />
          </div>

          <label className="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              className="w-4 h-4 rounded border-glass-border bg-glass-medium text-accent-purple focus:ring-accent-purple"
              {...register('isActive')}
            />
            <span className="text-text-primary">Set as active scenario</span>
          </label>

          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="ghost" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" loading={isSubmitting}>
              {scenario ? 'Save Changes' : 'Create Scenario'}
            </Button>
          </div>
        </form>
      </GlassCard>
    </div>
  );
}
