import { NavLink, Outlet } from 'react-router-dom';
import { GlassCard } from '@components/atoms/GlassCard';
import { LayoutDashboard, ClipboardList } from 'lucide-react';
import { cn } from '@utils/cn';

// Tab navigation items for Dashboard page
const dashboardNav = [
  { icon: LayoutDashboard, label: 'Overview', path: '/' },
  { icon: ClipboardList, label: 'Reviews', path: '/reviews' },
];

// Layout wrapper for Dashboard with tab navigation
export function DashboardLayoutWrapper() {
  return (
    <>
      {/* Sticky Header with Title - positioned at the very top */}
      <div className="sticky top-0 z-20 bg-background-primary/95 backdrop-blur-md border-b border-glass-border rounded-b-xl mb-4">
        <div className="py-4">
          <h1 className="text-xl md:text-2xl lg:text-3xl font-bold text-text-primary">Dashboard</h1>
          <p className="text-text-secondary mt-0.5 text-xs md:text-sm">Your LifeOS command center</p>
        </div>
      </div>

      <div className="space-y-4">
      {/* Tab Navigation */}
      <GlassCard variant="default" className="p-2">
        <nav className="flex flex-wrap gap-1">
          {dashboardNav.map((item) => (
            <NavLink
              key={item.path}
              to={item.path}
              end={item.path === '/'}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-1.5 px-3 py-2 rounded-lg transition-colors text-xs md:text-sm',
                  isActive
                    ? 'bg-accent-purple/20 text-accent-purple'
                    : 'text-text-secondary hover:text-text-primary hover:bg-background-hover'
                )
              }
            >
              <item.icon className="w-4 h-4" />
              <span className="font-medium whitespace-nowrap">{item.label}</span>
            </NavLink>
          ))}
        </nav>
      </GlassCard>

      {/* Content Area */}
      <Outlet />
      </div>
    </>
  );
}
