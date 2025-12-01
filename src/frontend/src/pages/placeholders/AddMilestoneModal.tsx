import { useEffect, useRef, useState } from 'react';
import { X } from 'lucide-react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Input } from '@components/atoms/Input';
import { useCreateMilestoneMutation } from '@/services';

interface AddMilestoneModalProps {
  isOpen: boolean;
  onClose: () => void;
  dimensionId: string;
}

export function AddMilestoneModal({ isOpen, onClose, dimensionId }: AddMilestoneModalProps) {
  const modalRef = useRef<HTMLDivElement>(null);
  const mouseDownTargetRef = useRef<EventTarget | null>(null);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [targetDate, setTargetDate] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});

  const [createMilestone, { isLoading }] = useCreateMilestoneMutation();

  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
      // Reset form
      setTitle('');
      setDescription('');
      setTargetDate('');
      setErrors({});
    }
    return () => {
      document.body.style.overflow = '';
    };
  }, [isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const newErrors: Record<string, string> = {};

    if (!title.trim()) {
      newErrors.title = 'Title is required';
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    try {
      await createMilestone({
        title: title.trim(),
        description: description.trim() || undefined,
        dimensionId,
        targetDate: targetDate || undefined,
      }).unwrap();
      onClose();
    } catch (error) {
      console.error('Failed to create milestone:', error);
      setErrors({ submit: 'Failed to create milestone. Please try again.' });
    }
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
          <h2 className="text-xl font-semibold text-text-primary">Add Milestone</h2>
          <button
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-background-hover transition-colors"
          >
            <X className="w-5 h-5 text-text-secondary" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Title"
            placeholder="e.g., Run a marathon"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            error={errors.title}
          />

          <div>
            <label className="block text-sm font-medium text-text-secondary mb-2">
              Description (Optional)
            </label>
            <textarea
              className="w-full px-4 py-3 bg-background-secondary border border-white/10 rounded-lg text-text-primary placeholder-text-tertiary focus:outline-none focus:ring-2 focus:ring-accent-primary focus:border-transparent resize-none"
              placeholder="e.g., Complete a full marathon under 4 hours"
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>

          <Input
            label="Target Date (Optional)"
            type="date"
            value={targetDate}
            onChange={(e) => setTargetDate(e.target.value)}
          />

          {errors.submit && (
            <p className="text-red-500 text-sm">{errors.submit}</p>
          )}

          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="ghost" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" loading={isLoading}>
              Add Milestone
            </Button>
          </div>
        </form>
      </GlassCard>
    </div>
  );
}
