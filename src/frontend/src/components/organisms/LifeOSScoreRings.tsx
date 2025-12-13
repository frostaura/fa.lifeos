import React from 'react';
import { ProgressRing } from '../atoms';
import { cn } from '../../utils/cn';

interface LifeOSScoreRingsProps {
  lifeScore: number;
  healthIndex: number;
  adherenceIndex: number;
  wealthHealthScore: number;
  size?: 'small' | 'medium' | 'large';
  className?: string;
}

export const LifeOSScoreRings: React.FC<LifeOSScoreRingsProps> = ({
  lifeScore,
  healthIndex,
  adherenceIndex,
  wealthHealthScore,
  size = 'large',
  className,
}) => {
  const sizeConfig = {
    small: { outer: 120, inner: 80, stroke: 8, fontSize: 'text-2xl', label: 'text-xs' },
    medium: { outer: 160, inner: 120, stroke: 10, fontSize: 'text-3xl', label: 'text-sm' },
    large: { outer: 200, inner: 160, stroke: 12, fontSize: 'text-4xl', label: 'text-base' },
  };

  const config = sizeConfig[size];

  return (
    <div className={cn('flex flex-col items-center gap-6', className)}>
      {/* Main LifeOS Score */}
      <div className="relative flex items-center justify-center">
        <ProgressRing
          progress={lifeScore}
          size={config.outer}
          strokeWidth={config.stroke}
          color="from-purple-500 to-pink-500"
        />
        <div className="absolute inset-0 flex flex-col items-center justify-center">
          <div className={cn('font-bold', config.fontSize)} style={{ color: 'var(--text-primary)' }}>
            {Math.round(lifeScore)}
          </div>
          <div className={cn('font-medium', config.label)} style={{ color: 'var(--text-secondary)' }}>
            LifeOS Score
          </div>
        </div>
      </div>

      {/* Three Component Rings */}
      <div className="grid grid-cols-3 gap-4 md:gap-8">
        {/* Health Index */}
        <div className="flex flex-col items-center gap-2">
          <div className="relative">
            <ProgressRing
              progress={healthIndex}
              size={config.inner}
              strokeWidth={config.stroke - 2}
              color="from-green-400 to-emerald-600"
            />
            <div className="absolute inset-0 flex items-center justify-center">
              <div className={cn('font-bold', 
                size === 'small' ? 'text-lg' : size === 'medium' ? 'text-xl' : 'text-2xl')}
                style={{ color: 'var(--text-primary)' }}>
                {Math.round(healthIndex)}
              </div>
            </div>
          </div>
          <div className={cn('text-center font-medium', config.label)} style={{ color: 'var(--text-secondary)' }}>
            Health
          </div>
        </div>

        {/* Adherence Index */}
        <div className="flex flex-col items-center gap-2">
          <div className="relative">
            <ProgressRing
              progress={adherenceIndex}
              size={config.inner}
              strokeWidth={config.stroke - 2}
              color="from-blue-400 to-cyan-600"
            />
            <div className="absolute inset-0 flex items-center justify-center">
              <div className={cn('font-bold',
                size === 'small' ? 'text-lg' : size === 'medium' ? 'text-xl' : 'text-2xl')}
                style={{ color: 'var(--text-primary)' }}>
                {Math.round(adherenceIndex)}
              </div>
            </div>
          </div>
          <div className={cn('text-center font-medium', config.label)} style={{ color: 'var(--text-secondary)' }}>
            Adherence
          </div>
        </div>

        {/* Wealth Health */}
        <div className="flex flex-col items-center gap-2">
          <div className="relative">
            <ProgressRing
              progress={wealthHealthScore}
              size={config.inner}
              strokeWidth={config.stroke - 2}
              color="from-amber-400 to-orange-600"
            />
            <div className="absolute inset-0 flex items-center justify-center">
              <div className={cn('font-bold',
                size === 'small' ? 'text-lg' : size === 'medium' ? 'text-xl' : 'text-2xl')}
                style={{ color: 'var(--text-primary)' }}>
                {Math.round(wealthHealthScore)}
              </div>
            </div>
          </div>
          <div className={cn('text-center font-medium', config.label)} style={{ color: 'var(--text-secondary)' }}>
            Wealth
          </div>
        </div>
      </div>
    </div>
  );
};
