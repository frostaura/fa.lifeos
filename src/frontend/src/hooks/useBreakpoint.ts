import { useState, useEffect } from 'react';

type Breakpoint = 'xs' | 'sm' | 'md' | 'lg' | 'xl' | '2xl';

const breakpoints: Record<Breakpoint, number> = {
  xs: 320,
  sm: 640,
  md: 768,
  lg: 1024,
  xl: 1280,
  '2xl': 1440,
};

export function useBreakpoint(breakpoint: Breakpoint): boolean {
  const [isBelow, setIsBelow] = useState(false);

  useEffect(() => {
    const checkBreakpoint = () => {
      setIsBelow(window.innerWidth < breakpoints[breakpoint]);
    };

    checkBreakpoint();
    window.addEventListener('resize', checkBreakpoint);
    return () => window.removeEventListener('resize', checkBreakpoint);
  }, [breakpoint]);

  return isBelow;
}
