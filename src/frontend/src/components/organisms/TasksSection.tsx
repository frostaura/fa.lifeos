import { useState } from 'react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import {
  ListTodo,
  Plus,
  Filter,
  Loader2,
} from 'lucide-react';
import {
  useGetTasksQuery,
  useDeleteTaskMutation,
  useCompleteTaskDetailMutation,
} from '@/services';
import type { LifeTaskItem } from '@/services';
import { TaskRow } from './TaskRow';
import { AddEditTaskModal } from './AddEditTaskModal';
import { confirmToast } from '@utils/confirmToast';

interface TasksSectionProps {
  dimensionId: string;
  dimensionColor: string;
}

type FilterType = 'all' | 'active' | 'completed';
type TaskTypeFilter = 'all' | 'habit' | 'one_off' | 'scheduled_event';

export function TasksSection({ dimensionId, dimensionColor }: TasksSectionProps) {
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [editTask, setEditTask] = useState<LifeTaskItem | null>(null);
  const [filterStatus, setFilterStatus] = useState<FilterType>('all');
  const [filterType, setFilterType] = useState<TaskTypeFilter>('all');

  const { data, isLoading, error } = useGetTasksQuery({
    dimensionId,
    isCompleted: filterStatus === 'completed' ? true : filterStatus === 'active' ? false : undefined,
    isActive: filterStatus === 'active' ? true : undefined,
    taskType: filterType !== 'all' ? filterType : undefined,
    perPage: 100,
  });

  const [deleteTask, { isLoading: isDeleting }] = useDeleteTaskMutation();
  const [completeTask, { isLoading: isCompleting }] = useCompleteTaskDetailMutation();

  const handleDeleteTask = async (taskId: string) => {
    const confirmed = await confirmToast({
      message: 'Are you sure you want to delete this task?',
    });
    if (!confirmed) {
      return;
    }
    try {
      await deleteTask(taskId).unwrap();
    } catch (error) {
      console.error('Failed to delete task:', error);
    }
  };

  const handleCompleteTask = async (taskId: string) => {
    try {
      await completeTask({ id: taskId }).unwrap();
    } catch (error) {
      console.error('Failed to complete task:', error);
    }
  };

  const handleEditTask = (task: LifeTaskItem) => {
    setEditTask(task);
    setIsAddModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsAddModalOpen(false);
    setEditTask(null);
  };

  const tasks = data?.data || [];

  return (
    <>
      <GlassCard variant="elevated" className="p-6">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-4">
          <div className="flex items-center gap-2">
            <ListTodo className="w-5 h-5" style={{ color: dimensionColor }} />
            <h2 className="text-xl font-semibold text-text-primary">Tasks</h2>
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="secondary"
              size="sm"
              onClick={() => setIsAddModalOpen(true)}
            >
              <Plus className="w-4 h-4 mr-1" />
              Add Task
            </Button>
          </div>
        </div>

        {/* Filters */}
        <div className="flex flex-wrap items-center gap-2 mb-4">
          <div className="flex items-center gap-1 text-text-tertiary">
            <Filter className="w-4 h-4" />
          </div>
          
          {/* Status filter */}
          <select
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value as FilterType)}
            className="px-3 py-1.5 text-sm bg-glass-light border border-glass-border rounded-lg text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple/50"
          >
            <option value="all">All Status</option>
            <option value="active">Active</option>
            <option value="completed">Completed</option>
          </select>

          {/* Type filter */}
          <select
            value={filterType}
            onChange={(e) => setFilterType(e.target.value as TaskTypeFilter)}
            className="px-3 py-1.5 text-sm bg-glass-light border border-glass-border rounded-lg text-text-primary focus:outline-none focus:ring-2 focus:ring-accent-purple/50"
          >
            <option value="all">All Types</option>
            <option value="habit">Habits</option>
            <option value="one_off">One-off</option>
            <option value="scheduled_event">Scheduled</option>
          </select>
        </div>

        {/* Loading state */}
        {isLoading && (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="w-6 h-6 animate-spin text-accent-primary" />
          </div>
        )}

        {/* Error state */}
        {error && !isLoading && (
          <p className="text-red-500 text-center py-4">Failed to load tasks</p>
        )}

        {/* Empty state */}
        {!isLoading && !error && tasks.length === 0 && (
          <p className="text-text-tertiary text-center py-8">
            {filterStatus !== 'all' || filterType !== 'all'
              ? 'No tasks match your filters'
              : 'No tasks for this dimension. Add one to get started!'}
          </p>
        )}

        {/* Task list */}
        {!isLoading && !error && tasks.length > 0 && (
          <div className="space-y-2">
            {tasks.map((task) => (
              <TaskRow
                key={task.id}
                task={task}
                onComplete={() => handleCompleteTask(task.id)}
                onEdit={() => handleEditTask(task)}
                onDelete={() => handleDeleteTask(task.id)}
                isCompleting={isCompleting}
                isDeleting={isDeleting}
              />
            ))}
          </div>
        )}

        {/* Task count */}
        {!isLoading && !error && data?.meta && (
          <p className="text-xs text-text-tertiary mt-4 text-right">
            Showing {tasks.length} of {data.meta.total} tasks
          </p>
        )}
      </GlassCard>

      {/* Add/Edit Task Modal */}
      <AddEditTaskModal
        isOpen={isAddModalOpen}
        onClose={handleCloseModal}
        dimensionId={dimensionId}
        editTask={editTask}
      />
    </>
  );
}
