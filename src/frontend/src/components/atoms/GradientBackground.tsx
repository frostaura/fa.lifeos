import { cn } from '@utils/cn';

interface GradientBackgroundProps {
  className?: string;
  children?: React.ReactNode;
  orbColor1?: string;
  orbColor2?: string;
  orbColor3?: string;
}

export function GradientBackground({ className, children, orbColor1 = '#8B5CF6', orbColor2 = '#22D3EE', orbColor3 = '#EC4899' }: GradientBackgroundProps) {
  // Convert hex to rgb for radial gradient
  const hexToRgb = (hex: string) => {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    return result ? {
      r: parseInt(result[1], 16),
      g: parseInt(result[2], 16),
      b: parseInt(result[3], 16)
    } : { r: 139, g: 92, b: 246 };
  };

  const rgb1 = hexToRgb(orbColor1);
  const rgb2 = hexToRgb(orbColor2);
  const rgb3 = hexToRgb(orbColor3);

  return (
    <div className={cn('fixed inset-0 -z-10 overflow-hidden', className)}>
      {/* Base dark background */}
      <div className="absolute inset-0 bg-background-primary" />

      {/* Purple orb - subtle, roaming */}
      <div
        className="absolute top-0 left-0 w-[900px] h-[900px] lg:w-[1400px] lg:h-[1400px] rounded-full animate-gradient-shift z-[1]"
        style={{
          background: `radial-gradient(circle, rgba(${rgb1.r}, ${rgb1.g}, ${rgb1.b}, 0.35) 0%, rgba(${rgb1.r}, ${rgb1.g}, ${rgb1.b}, 0.15) 40%, transparent 70%)`,
          filter: 'blur(60px)',
        }}
      />
      
      {/* Cyan orb - subtle, opposite direction */}
      <div
        className="absolute top-0 left-0 w-[900px] h-[900px] lg:w-[1400px] lg:h-[1400px] rounded-full animate-gradient-shift-reverse z-[1]"
        style={{
          background: `radial-gradient(circle, rgba(${rgb2.r}, ${rgb2.g}, ${rgb2.b}, 0.3) 0%, rgba(${rgb2.r}, ${rgb2.g}, ${rgb2.b}, 0.12) 40%, transparent 70%)`,
          filter: 'blur(60px)',
        }}
      />
      
      {/* Pink orb - subtle, diagonal */}
      <div
        className="absolute top-0 left-0 w-[700px] h-[700px] lg:w-[1100px] lg:h-[1100px] rounded-full animate-gradient-shift-diagonal z-[1]"
        style={{
          background: `radial-gradient(circle, rgba(${rgb3.r}, ${rgb3.g}, ${rgb3.b}, 0.25) 0%, rgba(${rgb3.r}, ${rgb3.g}, ${rgb3.b}, 0.1) 40%, transparent 70%)`,
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
