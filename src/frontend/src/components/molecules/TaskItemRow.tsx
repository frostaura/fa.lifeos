import { Check } from 'lucide-react';
import { cn } from '@utils/cn';
import type { TaskItem } from '@/types';

interface TaskItemRowProps {
  task: TaskItem;
  onToggle?: (id: string, completed: boolean) => void;
  className?: string;
}

export function TaskItemRow({ task, onToggle, className }: TaskItemRowProps) {
  const handleToggle = () => {
    onToggle?.(task.id, !task.completed);
  };

  return (
    <div
      className={cn(
        'flex items-center gap-3 p-2 rounded-lg hover:bg-background-hover/50 transition-colors',
        className
      )}
    >
      <button
        onClick={handleToggle}
        className={cn(
          'flex-shrink-0 w-5 h-5 rounded border-2 flex items-center justify-center transition-colors',
          task.completed
            ? 'bg-semantic-success border-semantic-success'
            : 'border-text-tertiary hover:border-accent-purple'
        )}
        aria-label={task.completed ? 'Mark as incomplete' : 'Mark as complete'}
      >
        {task.completed && <Check className="w-3 h-3 text-white" />}
      </button>
      <span
        className={cn(
          'flex-1',
          task.completed ? 'text-text-tertiary line-through' : 'text-text-primary'
        )}
      >
        {task.title}
      </span>
    </div>
  );
}
