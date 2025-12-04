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

      {/* Purple orb - subtle, roaming */}
      <div
        className="absolute top-0 left-0 w-[900px] h-[900px] lg:w-[1400px] lg:h-[1400px] rounded-full animate-gradient-shift z-[1]"
        style={{
          background: 'radial-gradient(circle, rgba(139, 92, 246, 0.35) 0%, rgba(139, 92, 246, 0.15) 40%, transparent 70%)',
          filter: 'blur(60px)',
        }}
      />
      
      {/* Cyan orb - subtle, opposite direction */}
      <div
        className="absolute top-0 left-0 w-[900px] h-[900px] lg:w-[1400px] lg:h-[1400px] rounded-full animate-gradient-shift-reverse z-[1]"
        style={{
          background: 'radial-gradient(circle, rgba(34, 211, 238, 0.3) 0%, rgba(34, 211, 238, 0.12) 40%, transparent 70%)',
          filter: 'blur(60px)',
        }}
      />
      
      {/* Pink orb - subtle, diagonal */}
      <div
        className="absolute top-0 left-0 w-[700px] h-[700px] lg:w-[1100px] lg:h-[1100px] rounded-full animate-gradient-shift-diagonal z-[1]"
        style={{
          background: 'radial-gradient(circle, rgba(236, 72, 153, 0.25) 0%, rgba(236, 72, 153, 0.1) 40%, transparent 70%)',
          filter: 'blur(60px)',
        }}
      />

      {/* Noise texture overlay */}
      <div
        className="absolute inset-0 opacity-[0.015] z-[2]"
        style={{
          backgroundImage: `url("data:image/svg+xml,%3Csvg viewBox='0 0 256 256' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='noiseFilter'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.65' numOctaves='4' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23noiseFilter)'/%3E%3C/svg%3E")`,
        }}
      />

      {children}
    </div>
  );
}
