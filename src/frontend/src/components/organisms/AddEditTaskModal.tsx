import { useEffect, useRef, useState } from 'react';
import { X } from 'lucide-react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Input } from '@components/atoms/Input';
import { useCreateTaskMutation, useUpdateTaskMutation } from '@/services';
import type { LifeTaskItem, CreateTaskRequest, UpdateTaskRequest } from '@/services';

interface AddEditTaskModalProps {
  isOpen: boolean;
  onClose: () => void;
  dimensionId: string;
  editTask?: LifeTaskItem | null;
}

type TaskType = 'habit' | 'one_off' | 'scheduled_event';
type Frequency = 'daily' | 'weekly' | 'monthly' | 'ad_hoc';

export function AddEditTaskModal({ isOpen, onClose, dimensionId, editTask }: AddEditTaskModalProps) {
  const modalRef = useRef<HTMLDivElement>(null);
  const mouseDownTargetRef = useRef<EventTarget | null>(null);
  
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [taskType, setTaskType] = useState<TaskType>('one_off');
  const [frequency, setFrequency] = useState<Frequency>('daily');
  const [scheduledDate, setScheduledDate] = useState('');
  const [scheduledTime, setScheduledTime] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});

  const [createTask, { isLoading: isCreating }] = useCreateTaskMutation();
  const [updateTask, { isLoading: isUpdating }] = useUpdateTaskMutation();
  const isLoading = isCreating || isUpdating;
  const isEditing = !!editTask;

  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
      
      // Populate form if editing
      if (editTask) {
        const attrs = editTask.attributes;
        setTitle(attrs.title);
        setDescription(attrs.description || '');
        setTaskType(attrs.taskType);
        setFrequency(attrs.frequency);
        setScheduledDate(attrs.scheduledDate || '');
        setScheduledTime(attrs.scheduledTime || '');
      }
    } else {
      document.body.style.overflow = '';
      // Reset form
      setTitle('');
      setDescription('');
      setTaskType('one_off');
      setFrequency('daily');
      setScheduledDate('');
      setScheduledTime('');
      setErrors({});
    }
    return () => {
      document.body.style.overflow = '';
    };
  }, [isOpen, editTask]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const newErrors: Record<string, string> = {};

    if (!title.trim()) {
      newErrors.title = 'Title is required';
    }

    if (taskType === 'scheduled_event' && !scheduledDate) {
      newErrors.scheduledDate = 'Scheduled date is required for events';
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    try {
      if (isEditing && editTask) {
        const updatePayload: { id: string } & UpdateTaskRequest = {
          id: editTask.id,
          title: title.trim(),
          description: description.trim() || undefined,
          frequency: taskType === 'habit' ? frequency : undefined,
          scheduledDate: taskType === 'scheduled_event' ? scheduledDate : undefined,
          scheduledTime: taskType === 'scheduled_event' && scheduledTime ? scheduledTime : undefined,
        };
        await updateTask(updatePayload).unwrap();
      } else {
        const createPayload: CreateTaskRequest = {
          title: title.trim(),
          description: description.trim() || undefined,
          taskType,
          frequency: taskType === 'habit' ? frequency : undefined,
          dimensionId,
          scheduledDate: taskType === 'scheduled_event' ? scheduledDate : undefined,
          scheduledTime: taskType === 'scheduled_event' && scheduledTime ? scheduledTime : undefined,
          startDate: new Date().toISOString().split('T')[0],
        };
        await createTask(createPayload).unwrap();
      }
      onClose();
    } catch (error) {
      console.error('Failed to save task:', error);
      setErrors({ submit: `Failed to ${isEditing ? 'update' : 'create'} task. Please try again.` });
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
        className="relative z-10 w-full max-w-lg mx-4 p-6 max-h-[90vh] overflow-y-auto"
      >
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-xl font-semibold text-text-primary">
            {isEditing ? 'Edit Task' : 'Add Task'}
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
            label="Title"
            placeholder="e.g., Morning run"
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
              placeholder="Add a description..."
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>

          {/* Task Type - Disabled when editing */}
          <div>
            <label className="block text-sm font-medium text-text-secondary mb-2">
              Task Type
            </label>
            <div className="grid grid-cols-3 gap-2">
              {[
                { value: 'one_off' as const, label: 'One-off' },
                { value: 'habit' as const, label: 'Habit' },
                { value: 'scheduled_event' as const, label: 'Event' },
              ].map((type) => (
                <button
                  key={type.value}
                  type="button"
                  onClick={() => !isEditing && setTaskType(type.value)}
                  disabled={isEditing}
                  className={`px-4 py-2 rounded-lg border transition-colors ${
                    taskType === type.value
                      ? 'bg-accent-purple/20 border-accent-purple text-accent-purple'
                      : 'bg-glass-light border-glass-border text-text-secondary hover:text-text-primary'
                  } ${isEditing ? 'opacity-60 cursor-not-allowed' : ''}`}
                >
                  {type.label}
                </button>
              ))}
            </div>
            {isEditing && (
              <p className="text-xs text-text-tertiary mt-1">Task type cannot be changed after creation</p>
            )}
          </div>

          {/* Frequency for habits */}
          {taskType === 'habit' && (
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-2">
                Frequency
              </label>
              <select
                value={frequency}
                onChange={(e) => setFrequency(e.target.value as Frequency)}
                className="w-full px-4 py-2 bg-glass-medium border border-glass-border rounded-lg text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple/50"
              >
                <option value="daily">Daily</option>
                <option value="weekly">Weekly</option>
                <option value="monthly">Monthly</option>
                <option value="ad_hoc">Ad-hoc</option>
              </select>
            </div>
          )}

          {/* Scheduled date/time for events */}
          {taskType === 'scheduled_event' && (
            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Date"
                type="date"
                value={scheduledDate}
                onChange={(e) => setScheduledDate(e.target.value)}
                error={errors.scheduledDate}
              />
              <Input
                label="Time (Optional)"
                type="time"
                value={scheduledTime}
                onChange={(e) => setScheduledTime(e.target.value)}
              />
            </div>
          )}

          {errors.submit && (
            <p className="text-red-500 text-sm">{errors.submit}</p>
          )}

          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="ghost" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" loading={isLoading}>
              {isEditing ? 'Save Changes' : 'Add Task'}
            </Button>
          </div>
        </form>
      </GlassCard>
    </div>
  );
}
