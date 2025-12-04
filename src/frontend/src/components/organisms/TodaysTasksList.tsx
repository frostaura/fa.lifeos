import { GlassCard } from '@components/atoms/GlassCard';
import { TaskItemRow } from '@components/molecules/TaskItemRow';
import { CheckCircle } from 'lucide-react';
import { cn } from '@utils/cn';
import type { TaskItem } from '@/types';

interface TodaysTasksListProps {
  tasks: TaskItem[];
  onToggleTask?: (id: string, completed: boolean) => void;
  className?: string;
}

export function TodaysTasksList({ tasks, onToggleTask, className }: TodaysTasksListProps) {
  const completedCount = tasks.filter((t) => t.completed).length;
  const totalCount = tasks.length;
  const progress = totalCount > 0 ? Math.round((completedCount / totalCount) * 100) : 0;

  return (
    <GlassCard variant="default" className={cn('p-4 md:p-6', className)}>
      <div className="flex items-center justify-between mb-3 md:mb-4">
        <div className="flex items-center gap-2">
          <CheckCircle className="w-4 h-4 md:w-5 md:h-5 text-accent-cyan" />
          <h2 className="text-base md:text-lg font-semibold text-text-primary whitespace-nowrap">Today's Tasks</h2>
        </div>
        <span className="text-xs md:text-sm text-text-secondary whitespace-nowrap">
          {completedCount}/{totalCount} done
        </span>
      </div>

      {/* Progress indicator */}
      <div className="h-1 bg-glass-light rounded-full overflow-hidden mb-3 md:mb-4">
        <div
          className="h-full bg-gradient-to-r from-accent-purple to-accent-cyan transition-all duration-300"
          style={{ width: `${progress}%` }}
        />
      </div>

      {tasks.length === 0 ? (
        <p className="text-text-tertiary text-xs md:text-sm">No tasks for today. Enjoy your day!</p>
      ) : (
        <div className="space-y-1">
          {tasks.map((task) => (
            <TaskItemRow
              key={task.id}
              task={task}
              onToggle={onToggleTask}
            />
          ))}
        </div>
      )}
    </GlassCard>
  );
}
