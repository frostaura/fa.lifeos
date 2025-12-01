import { Link } from 'react-router-dom';
import { GlassCard } from '@components/atoms/GlassCard';
import { Home } from 'lucide-react';

export function NotFound() {
  return (
    <div className="min-h-[60vh] flex items-center justify-center">
      <GlassCard variant="elevated" className="p-12 text-center max-w-md">
        <h1 className="text-8xl font-bold text-gradient mb-4">404</h1>
        <h2 className="text-2xl font-semibold text-text-primary mb-2">Page Not Found</h2>
        <p className="text-text-secondary mb-8">
          The page you're looking for doesn't exist or has been moved.
        </p>
        <Link
          to="/"
          className="inline-flex items-center gap-2 px-6 py-3 bg-accent-purple rounded-lg text-white font-medium hover:bg-accent-purple/80 transition-colors"
        >
          <Home className="w-5 h-5" />
          Back to Dashboard
        </Link>
      </GlassCard>
    </div>
  );
}
