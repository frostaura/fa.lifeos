import { Outlet } from 'react-router-dom';
import { Sidebar } from '@components/organisms/Sidebar';
import { GradientBackground } from '@components/atoms/GradientBackground';
import { useAppSelector } from '@hooks/useAppDispatch';
import { useBreakpoint } from '@hooks/useBreakpoint';
import { cn } from '@utils/cn';

export function AppLayout() {
  const { sidebarCollapsed } = useAppSelector((state) => state.ui);
  const isMobile = useBreakpoint('lg');

  return (
    <div className="min-h-screen">
      <GradientBackground />
      <Sidebar />

      <main
        className={cn(
          'min-h-screen transition-all duration-300',
          isMobile
            ? 'pt-16 pb-20 px-4'
            : sidebarCollapsed
            ? 'lg:pl-16'
            : 'lg:pl-64'
        )}
      >
        <div className="max-w-7xl mx-auto py-6 px-4 lg:px-8">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
