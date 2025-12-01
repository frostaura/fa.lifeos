import { Link } from 'react-router-dom';
import { GlassCard } from '@components/atoms/GlassCard';
import { ChevronRight, AlertCircle, Loader2 } from 'lucide-react';
import { useGetDimensionsQuery } from '@/services';
import { getDimensionIcon, getDimensionColor } from '@utils/dimensionIcons';
import type { DimensionItemResponse } from '@/types';

export function Dimensions() {
  const { data, isLoading, error } = useGetDimensionsQuery();

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-8 h-8 animate-spin text-accent-primary" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center h-64 space-y-4">
        <AlertCircle className="w-12 h-12 text-red-500" />
        <p className="text-text-secondary">Failed to load dimensions</p>
      </div>
    );
  }

  const dimensions: DimensionItemResponse[] = data?.data || [];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-text-primary">Dimensions</h1>
        <p className="text-text-secondary mt-1">
          Track and optimize all areas of your life
        </p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        {dimensions.map((dim) => {
          const Icon = getDimensionIcon(dim.attributes.code);
          const color = getDimensionColor(dim.attributes.code);
          const score = dim.attributes.currentScore;

          return (
            <Link key={dim.id} to={`/dimensions/${dim.id}`}>
              <GlassCard
                variant="elevated"
                className="p-6 h-full hover:shadow-glow-sm transition-all group"
              >
                {/* Header */}
                <div className="flex items-center justify-between mb-4">
                  <div
                    className="p-3 rounded-xl"
                    style={{ backgroundColor: `${color}20` }}
                  >
                    <Icon className="w-6 h-6" style={{ color }} />
                  </div>
                  <ChevronRight className="w-5 h-5 text-text-tertiary group-hover:text-text-primary transition-colors" />
                </div>

                {/* Title */}
                <h3 className="text-xl font-semibold text-text-primary mb-1">
                  {dim.attributes.name}
                </h3>
                <p className="text-text-tertiary text-sm mb-4">
                  {dim.attributes.description || 'No description'}
                </p>

                {/* Score */}
                <div className="flex items-center justify-between">
                  <div>
                    <span className="text-3xl font-bold text-text-primary">
                      {score}
                    </span>
                    <span className="text-text-tertiary text-sm">/100</span>
                  </div>
                  <div className="text-right">
                    <span className="text-sm text-text-secondary">
                      Weight: {Math.round(dim.attributes.weight * 100)}%
                    </span>
                  </div>
                </div>

                {/* Progress bar */}
                <div className="mt-4 h-2 bg-glass-light rounded-full overflow-hidden">
                  <div
                    className="h-full rounded-full transition-all duration-500"
                    style={{
                      width: `${score}%`,
                      backgroundColor: color,
                    }}
                  />
                </div>
              </GlassCard>
            </Link>
          );
        })}
      </div>
    </div>
  );
}
