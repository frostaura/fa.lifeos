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
          <div className={cn('font-bold text-white', config.fontSize)}>
            {Math.round(lifeScore)}
          </div>
          <div className={cn('text-gray-400 font-medium', config.label)}>
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
              <div className={cn('font-bold text-white', 
                size === 'small' ? 'text-lg' : size === 'medium' ? 'text-xl' : 'text-2xl')}>
                {Math.round(healthIndex)}
              </div>
            </div>
          </div>
          <div className={cn('text-center text-gray-400 font-medium', config.label)}>
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
              <div className={cn('font-bold text-white',
                size === 'small' ? 'text-lg' : size === 'medium' ? 'text-xl' : 'text-2xl')}>
                {Math.round(adherenceIndex)}
              </div>
            </div>
          </div>
          <div className={cn('text-center text-gray-400 font-medium', config.label)}>
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
              <div className={cn('font-bold text-white',
                size === 'small' ? 'text-lg' : size === 'medium' ? 'text-xl' : 'text-2xl')}>
                {Math.round(wealthHealthScore)}
              </div>
            </div>
          </div>
          <div className={cn('text-center text-gray-400 font-medium', config.label)}>
            Wealth
          </div>
        </div>
      </div>
    </div>
  );
};
