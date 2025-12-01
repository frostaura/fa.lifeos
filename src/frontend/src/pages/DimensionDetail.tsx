import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import {
  ArrowLeft,
  AlertCircle,
  Loader2,
  Target,
  CheckCircle2,
  Clock,
  ListTodo,
  Plus,
  Trash2,
} from 'lucide-react';
import { useGetDimensionQuery, useGetMilestonesQuery, useDeleteMilestoneMutation } from '@/services';
import { getDimensionIcon, getDimensionColor } from '@utils/dimensionIcons';
import type { TaskReference } from '@/types';
import { AddMilestoneModal } from './placeholders/AddMilestoneModal';

export function DimensionDetail() {
  const { dimensionId } = useParams<{ dimensionId: string }>();
  const [isAddMilestoneOpen, setIsAddMilestoneOpen] = useState(false);
  
  const { data, isLoading, error } = useGetDimensionQuery(dimensionId || '');
  const { data: milestonesData } = useGetMilestonesQuery(
    dimensionId ? { dimensionId } : undefined,
    { skip: !dimensionId }
  );
  const [deleteMilestone, { isLoading: isDeleting }] = useDeleteMilestoneMutation();

  const handleDeleteMilestone = async (milestoneId: string) => {
    if (!window.confirm('Are you sure you want to delete this milestone?')) {
      return;
    }
    try {
      await deleteMilestone(milestoneId).unwrap();
    } catch (error) {
      console.error('Failed to delete milestone:', error);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-8 h-8 animate-spin text-accent-primary" />
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="flex flex-col items-center justify-center h-64 space-y-4">
        <AlertCircle className="w-12 h-12 text-red-500" />
        <p className="text-text-secondary">Failed to load dimension</p>
        <Link
          to="/dimensions"
          className="text-accent-primary hover:underline flex items-center gap-2"
        >
          <ArrowLeft className="w-4 h-4" />
          Back to Dimensions
        </Link>
      </div>
    );
  }

  const dimension = data.data;
  const { attributes, relationships } = dimension;
  const Icon = getDimensionIcon(attributes.code);
  const color = getDimensionColor(attributes.code);
  
  // Use milestones from RTK Query if available, otherwise fall back to relationships
  const milestones = milestonesData?.data || [];
  const activeTasks: TaskReference[] = relationships?.activeTasks || [];

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'completed':
        return 'text-green-500';
      case 'in_progress':
      case 'active':
        return 'text-blue-500';
      case 'not_started':
        return 'text-gray-500';
      default:
        return 'text-text-secondary';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'completed':
        return <CheckCircle2 className="w-4 h-4 text-green-500" />;
      case 'in_progress':
      case 'active':
        return <Clock className="w-4 h-4 text-blue-500" />;
      default:
        return <Target className="w-4 h-4 text-gray-500" />;
    }
  };

  return (
    <div className="space-y-6">
      {/* Back link and Header */}
      <div>
        <Link
          to="/dimensions"
          className="text-text-secondary hover:text-text-primary flex items-center gap-2 mb-4"
        >
          <ArrowLeft className="w-4 h-4" />
          Back to Dimensions
        </Link>

        <div className="flex items-center gap-4">
          <div
            className="p-4 rounded-xl"
            style={{ backgroundColor: `${color}20` }}
          >
            <Icon className="w-8 h-8" style={{ color }} />
          </div>
          <div>
            <h1 className="text-3xl font-bold text-text-primary">
              {attributes.name}
            </h1>
            <p className="text-text-secondary mt-1">
              {attributes.description || 'No description'}
            </p>
          </div>
        </div>
      </div>

      {/* Dimension Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <GlassCard variant="default" className="p-6">
          <p className="text-text-tertiary text-sm mb-1">Current Score</p>
          <div className="flex items-baseline gap-1">
            <span className="text-3xl font-bold text-text-primary">
              {attributes.currentScore}
            </span>
            <span className="text-text-tertiary">/100</span>
          </div>
          <div className="mt-3 h-2 bg-glass-light rounded-full overflow-hidden">
            <div
              className="h-full rounded-full transition-all duration-500"
              style={{
                width: `${attributes.currentScore}%`,
                backgroundColor: color,
              }}
            />
          </div>
        </GlassCard>

        <GlassCard variant="default" className="p-6">
          <p className="text-text-tertiary text-sm mb-1">Weight</p>
          <div className="flex items-baseline gap-1">
            <span className="text-3xl font-bold text-text-primary">
              {Math.round(attributes.weight * 100)}%
            </span>
          </div>
          <p className="text-text-tertiary text-sm mt-2">
            Default: {Math.round(attributes.defaultWeight * 100)}%
          </p>
        </GlassCard>

        <GlassCard variant="default" className="p-6">
          <p className="text-text-tertiary text-sm mb-1">Active Items</p>
          <div className="flex items-baseline gap-1">
            <span className="text-3xl font-bold text-text-primary">
              {milestones.length + activeTasks.length}
            </span>
          </div>
          <p className="text-text-tertiary text-sm mt-2">
            {milestones.length} milestones, {activeTasks.length} tasks
          </p>
        </GlassCard>
      </div>

      {/* Milestones Section */}
      <GlassCard variant="elevated" className="p-6">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Target className="w-5 h-5" style={{ color }} />
            <h2 className="text-xl font-semibold text-text-primary">Milestones</h2>
          </div>
          <Button
            variant="secondary"
            size="sm"
            onClick={() => setIsAddMilestoneOpen(true)}
          >
            <Plus className="w-4 h-4 mr-1" />
            Add Milestone
          </Button>
        </div>

        {milestones.length === 0 ? (
          <p className="text-text-tertiary">No milestones for this dimension</p>
        ) : (
          <div className="space-y-3">
            {milestones.map((milestone) => (
              <div
                key={milestone.id}
                className="flex items-center justify-between p-3 bg-glass-light rounded-lg"
              >
                <div className="flex items-center gap-3">
                  {getStatusIcon(milestone.attributes.status)}
                  <div>
                    <span className="text-text-primary">{milestone.attributes.title}</span>
                    {milestone.attributes.targetDate && (
                      <p className="text-xs text-text-tertiary mt-0.5">
                        Target: {new Date(milestone.attributes.targetDate).toLocaleDateString()}
                      </p>
                    )}
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <span
                    className={`text-sm capitalize ${getStatusColor(milestone.attributes.status)}`}
                  >
                    {milestone.attributes.status.replace('_', ' ')}
                  </span>
                  <button
                    onClick={() => handleDeleteMilestone(milestone.id)}
                    disabled={isDeleting}
                    className="p-1.5 rounded-lg hover:bg-red-500/20 transition-colors text-text-tertiary hover:text-red-500"
                    title="Delete milestone"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </GlassCard>

      {/* Tasks Section */}
      <GlassCard variant="elevated" className="p-6">
        <div className="flex items-center gap-2 mb-4">
          <ListTodo className="w-5 h-5" style={{ color }} />
          <h2 className="text-xl font-semibold text-text-primary">Active Tasks</h2>
        </div>

        {activeTasks.length === 0 ? (
          <p className="text-text-tertiary">No active tasks for this dimension</p>
        ) : (
          <div className="space-y-3">
            {activeTasks.map((task) => (
              <div
                key={task.id}
                className="flex items-center justify-between p-3 bg-glass-light rounded-lg"
              >
                <div className="flex items-center gap-3">
                  <ListTodo className="w-4 h-4 text-text-tertiary" />
                  <span className="text-text-primary">{task.title}</span>
                </div>
                <span className="text-sm text-text-secondary capitalize">
                  {task.taskType.replace('_', ' ')}
                </span>
              </div>
            ))}
          </div>
        )}
      </GlassCard>

      {/* Add Milestone Modal */}
      {dimensionId && (
        <AddMilestoneModal
          isOpen={isAddMilestoneOpen}
          onClose={() => setIsAddMilestoneOpen(false)}
          dimensionId={dimensionId}
        />
      )}
    </div>
  );
}
