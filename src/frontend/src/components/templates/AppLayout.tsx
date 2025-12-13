import { Outlet } from 'react-router-dom';
import { Sidebar } from '@components/organisms/Sidebar';
import { GradientBackground } from '@components/atoms/GradientBackground';
import { useAppSelector } from '@hooks/useAppDispatch';
import { useBreakpoint } from '@hooks/useBreakpoint';
import { useGetProfileQuery } from '@services/endpoints/settings';
import { cn } from '@utils/cn';

export function AppLayout() {
  const { sidebarCollapsed } = useAppSelector((state) => state.ui);
  const isMobile = useBreakpoint('lg');
  const { data: profile } = useGetProfileQuery();

  return (
    <div className="min-h-screen">
      <GradientBackground 
        orbColor1={profile?.appearance?.orbColor1}
        orbColor2={profile?.appearance?.orbColor2}
        orbColor3={profile?.appearance?.orbColor3}
      />
      <Sidebar />

      <main
        className={cn(
          'h-screen transition-all duration-300 flex flex-col',
          isMobile
            ? 'pt-14 px-2 sm:px-4'
            : sidebarCollapsed
            ? 'lg:pl-16'
            : 'lg:pl-64'
        )}
      >
        <div className="max-w-7xl mx-auto py-4 md:py-6 px-2 sm:px-4 lg:px-8 flex-1 flex flex-col min-h-0">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
