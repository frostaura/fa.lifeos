import { forwardRef } from 'react';
import { cn } from '@utils/cn';

interface GlassCardProps {
  variant?: 'default' | 'elevated' | 'outlined' | 'solid';
  glow?: 'none' | 'subtle' | 'accent';
  children: React.ReactNode;
  className?: string;
  onClick?: () => void;
}

export const GlassCard = forwardRef<HTMLDivElement, GlassCardProps>(
  function GlassCard(
    {
      variant = 'default',
      glow = 'none',
      children,
      className,
      onClick,
    },
    ref
  ) {
    const baseStyles = 'rounded-xl backdrop-blur-md transition-all duration-200';

    const variantStyles = {
      default: 'bg-glass-medium border border-glass-border',
      elevated: 'bg-glass-heavy border border-glass-border shadow-elevated',
      outlined: 'bg-transparent border border-glass-border',
      solid: 'bg-background-secondary/70 backdrop-blur-xl border border-glass-border shadow-lg',
    };

    const glowStyles = {
      none: '',
      subtle: 'shadow-glow-sm',
      accent: 'shadow-glow-md hover:shadow-glow-lg',
    };

    const interactiveStyles = onClick
      ? 'cursor-pointer hover:bg-background-hover'
      : '';

    return (
      <div
        ref={ref}
        className={cn(
          baseStyles,
          variantStyles[variant],
          glowStyles[glow],
          interactiveStyles,
          className
        )}
        onClick={onClick}
        role={onClick ? 'button' : undefined}
        tabIndex={onClick ? 0 : undefined}
        onKeyDown={
          onClick
            ? (e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  onClick();
                }
              }
            : undefined
        }
      >
        {children}
      </div>
    );
  }
);
