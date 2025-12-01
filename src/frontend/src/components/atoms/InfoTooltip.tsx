import { useState, useRef, useEffect } from 'react';
import { Info } from 'lucide-react';
import { cn } from '@utils/cn';

interface InfoTooltipProps {
  content: string | React.ReactNode;
  className?: string;
  iconClassName?: string;
  position?: 'top' | 'bottom' | 'left' | 'right';
}

export function InfoTooltip({ 
  content, 
  className, 
  iconClassName,
  position = 'top' 
}: InfoTooltipProps) {
  const [isVisible, setIsVisible] = useState(false);
  const [adjustedPosition, setAdjustedPosition] = useState(position);
  const tooltipRef = useRef<HTMLDivElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (isVisible && tooltipRef.current && containerRef.current) {
      const tooltipRect = tooltipRef.current.getBoundingClientRect();
      
      // Adjust position if tooltip would overflow viewport
      if (position === 'top' && tooltipRect.top < 0) {
        setAdjustedPosition('bottom');
      } else if (position === 'bottom' && tooltipRect.bottom > window.innerHeight) {
        setAdjustedPosition('top');
      } else if (position === 'left' && tooltipRect.left < 0) {
        setAdjustedPosition('right');
      } else if (position === 'right' && tooltipRect.right > window.innerWidth) {
        setAdjustedPosition('left');
      } else {
        setAdjustedPosition(position);
      }
    }
  }, [isVisible, position]);

  const positionClasses = {
    top: 'bottom-full left-1/2 -translate-x-1/2 mb-2',
    bottom: 'top-full left-1/2 -translate-x-1/2 mt-2',
    left: 'right-full top-1/2 -translate-y-1/2 mr-2',
    right: 'left-full top-1/2 -translate-y-1/2 ml-2',
  };

  const arrowClasses = {
    top: 'top-full left-1/2 -translate-x-1/2 border-l-transparent border-r-transparent border-b-transparent border-t-[#2a2a38]',
    bottom: 'bottom-full left-1/2 -translate-x-1/2 border-l-transparent border-r-transparent border-t-transparent border-b-[#2a2a38]',
    left: 'left-full top-1/2 -translate-y-1/2 border-t-transparent border-b-transparent border-r-transparent border-l-[#2a2a38]',
    right: 'right-full top-1/2 -translate-y-1/2 border-t-transparent border-b-transparent border-l-transparent border-r-[#2a2a38]',
  };

  return (
    <div 
      ref={containerRef}
      className={cn("relative inline-flex items-center", className)}
      onMouseEnter={() => setIsVisible(true)}
      onMouseLeave={() => setIsVisible(false)}
      onFocus={() => setIsVisible(true)}
      onBlur={() => setIsVisible(false)}
    >
      <button
        type="button"
        className={cn(
          "p-0.5 rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-accent-purple/50",
          "text-text-tertiary hover:text-accent-purple hover:bg-accent-purple/10",
          iconClassName
        )}
        aria-label="More information"
        tabIndex={0}
      >
        <Info className="w-3.5 h-3.5" />
      </button>

      {isVisible && (
        <div
          ref={tooltipRef}
          className={cn(
            "absolute z-50 w-64 px-3 py-2.5",
            "bg-[#2a2a38] border border-glass-border rounded-lg shadow-xl",
            "text-sm text-text-secondary leading-relaxed",
            "animate-in fade-in-0 zoom-in-95 duration-150",
            positionClasses[adjustedPosition]
          )}
          role="tooltip"
        >
          {content}
          {/* Arrow */}
          <div
            className={cn(
              "absolute w-0 h-0 border-[6px]",
              arrowClasses[adjustedPosition]
            )}
          />
        </div>
      )}
    </div>
  );
}
