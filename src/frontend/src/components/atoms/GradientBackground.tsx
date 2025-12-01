import { cn } from '@utils/cn';

interface GradientBackgroundProps {
  className?: string;
  children?: React.ReactNode;
}

export function GradientBackground({ className, children }: GradientBackgroundProps) {
  return (
    <div className={cn('fixed inset-0 -z-10 overflow-hidden', className)}>
      {/* Base dark background */}
      <div className="absolute inset-0 bg-background-primary" />

      {/* Animated gradient orbs */}
      <div
        className="absolute -top-1/2 -left-1/2 w-full h-full opacity-30 animate-gradient-shift"
        style={{
          background:
            'radial-gradient(ellipse at center, rgba(139, 92, 246, 0.3) 0%, transparent 70%)',
          backgroundSize: '200% 200%',
        }}
      />
      <div
        className="absolute -bottom-1/2 -right-1/2 w-full h-full opacity-20 animate-gradient-shift"
        style={{
          background:
            'radial-gradient(ellipse at center, rgba(34, 211, 238, 0.3) 0%, transparent 70%)',
          backgroundSize: '200% 200%',
          animationDelay: '4s',
        }}
      />
      <div
        className="absolute top-1/4 right-1/4 w-1/2 h-1/2 opacity-15 animate-gradient-shift"
        style={{
          background:
            'radial-gradient(ellipse at center, rgba(236, 72, 153, 0.3) 0%, transparent 70%)',
          backgroundSize: '200% 200%',
          animationDelay: '2s',
        }}
      />

      {/* Noise texture overlay */}
      <div
        className="absolute inset-0 opacity-[0.015]"
        style={{
          backgroundImage: `url("data:image/svg+xml,%3Csvg viewBox='0 0 256 256' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='noiseFilter'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.65' numOctaves='4' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23noiseFilter)'/%3E%3C/svg%3E")`,
        }}
      />

      {children}
    </div>
  );
}
