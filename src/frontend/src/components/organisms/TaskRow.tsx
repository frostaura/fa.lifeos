import { useState } from 'react';
import {
  CheckCircle2,
  Circle,
  Pencil,
  Trash2,
  Undo2,
  Clock,
  Calendar,
  Repeat,
  Flame,
} from 'lucide-react';
import { cn } from '@utils/cn';
import type { LifeTaskItem } from '@/services';

interface TaskRowProps {
  task: LifeTaskItem;
  onComplete: () => void;
  onEdit: () => void;
  onDelete: () => void;
  onUncomplete?: () => void;
  isCompleting?: boolean;
  isDeleting?: boolean;
}

export function TaskRow({
  task,
  onComplete,
  onEdit,
  onDelete,
  onUncomplete,
  isCompleting = false,
  isDeleting = false,
}: TaskRowProps) {
  const [isHovered, setIsHovered] = useState(false);
  const { attributes } = task;

  const getTaskTypeIcon = () => {
    switch (attributes.taskType) {
      case 'habit':
        return <Repeat className="w-3 h-3" />;
      case 'scheduled_event':
        return <Calendar className="w-3 h-3" />;
      default:
        return <Clock className="w-3 h-3" />;
    }
  };

  const getTaskTypeLabel = () => {
    switch (attributes.taskType) {
      case 'habit':
        return attributes.frequency;
      case 'scheduled_event':
        return attributes.scheduledDate 
          ? new Date(attributes.scheduledDate).toLocaleDateString()
          : 'Scheduled';
      default:
        return 'One-off';
    }
  };

  const isCompleted = attributes.isCompleted;
  
  // Calculate if recently completed (within 24 hours) for uncomplete option
  const canUncomplete = isCompleted && attributes.completedAt && onUncomplete && 
    (Date.now() - new Date(attributes.completedAt).getTime()) < 24 * 60 * 60 * 1000;

  return (
    <div
      className={cn(
        'flex items-center justify-between p-3 rounded-lg transition-all',
        'bg-glass-light hover:bg-glass-medium',
        isCompleted && 'opacity-60'
      )}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      <div className="flex items-center gap-3 flex-1 min-w-0">
        {/* Complete/Uncomplete button */}
        <button
          onClick={isCompleted && canUncomplete ? onUncomplete : !isCompleted ? onComplete : undefined}
          disabled={isCompleting || (isCompleted && !canUncomplete)}
          className={cn(
            'flex-shrink-0 p-1 rounded-lg transition-colors',
            isCompleted
              ? canUncomplete
                ? 'text-green-500 hover:bg-green-500/20'
                : 'text-green-500'
              : 'text-text-tertiary hover:text-green-500 hover:bg-green-500/20'
          )}
          title={isCompleted ? (canUncomplete ? 'Mark incomplete' : 'Completed') : 'Mark complete'}
        >
          {isCompleted ? (
            canUncomplete && isHovered ? (
              <Undo2 className="w-5 h-5" />
            ) : (
              <CheckCircle2 className="w-5 h-5" />
            )
          ) : (
            <Circle className="w-5 h-5" />
          )}
        </button>

        {/* Task info */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <span
              className={cn(
                'text-text-primary truncate',
                isCompleted && 'line-through text-text-secondary'
              )}
            >
              {attributes.title}
            </span>
          </div>
          
          {/* Streak indicator for habits */}
          {attributes.taskType === 'habit' && attributes.streakDays && attributes.streakDays > 0 && (
            <div className="flex items-center gap-1 mt-0.5">
              <Flame className="w-3 h-3 text-orange-500" />
              <span className="text-xs text-orange-500">
                {attributes.streakDays} day streak
              </span>
            </div>
          )}
        </div>
      </div>

      {/* Task type and actions */}
      <div className="flex items-center gap-2">
        {/* Type badge */}
        <span className="flex items-center gap-1 px-2 py-0.5 rounded-full bg-glass-medium text-text-secondary text-xs">
          {getTaskTypeIcon()}
          <span className="capitalize">{getTaskTypeLabel()}</span>
        </span>

        {/* Actions (show on hover or always on mobile) */}
        <div className={cn(
          'flex items-center gap-1 transition-opacity',
          isHovered ? 'opacity-100' : 'opacity-0 md:opacity-0'
        )}>
          <button
            onClick={onEdit}
            className="p-1.5 rounded-lg hover:bg-glass-medium transition-colors text-text-tertiary hover:text-text-primary"
            title="Edit task"
          >
            <Pencil className="w-4 h-4" />
          </button>
          <button
            onClick={onDelete}
            disabled={isDeleting}
            className="p-1.5 rounded-lg hover:bg-red-500/20 transition-colors text-text-tertiary hover:text-red-500"
            title="Delete task"
          >
            <Trash2 className="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>
  );
}
