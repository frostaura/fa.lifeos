import { useLogoutMutation } from '@/services/endpoints/auth';
import { useAppDispatch, useAppSelector } from '@hooks/useAppDispatch';
import { useBreakpoint } from '@hooks/useBreakpoint';
import { logout } from '@store/slices/authSlice';
import { closeMobileSidebar, toggleMobileSidebar, toggleSidebar } from '@store/slices/uiSlice';
import { cn } from '@utils/cn';
import {
    Activity,
    ChevronLeft,
    ChevronRight,
    ClipboardList,
    Grid3X3,
    Heart,
    LayoutDashboard,
    LogOut,
    Menu,
    Settings,
    Wallet,
    X,
} from 'lucide-react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';

const navItems = [
    { icon: LayoutDashboard, label: 'Dashboard', path: '/' },
    { icon: Grid3X3, label: 'Dimensions', path: '/dimensions' },
    { icon: Wallet, label: 'Finances', path: '/finances' },
    { icon: Heart, label: 'Health', path: '/health' },
    { icon: Activity, label: 'Metrics', path: '/metrics' },
    { icon: ClipboardList, label: 'Reviews', path: '/reviews' },
    { icon: Settings, label: 'Settings', path: '/settings' },
];

export function Sidebar() {
    const dispatch = useAppDispatch();
    const navigate = useNavigate();
    const { sidebarCollapsed, sidebarOpen } = useAppSelector((state) => state.ui);
    const isMobile = useBreakpoint('lg');
    const location = useLocation();
    const [logoutMutation, { isLoading: isLoggingOut }] = useLogoutMutation();

    const handleToggle = () => {
        if (isMobile) {
            dispatch(toggleMobileSidebar());
        } else {
            dispatch(toggleSidebar());
        }
    };

    const handleNavClick = () => {
        if (isMobile) {
            dispatch(closeMobileSidebar());
        }
    };

    const handleSignOut = async () => {
        try {
            await logoutMutation().unwrap();
        } catch {
            // Continue with local logout even if API call fails
        }
        localStorage.removeItem('accessToken');
        localStorage.removeItem('user');
        dispatch(logout());
        navigate('/login');
    };

    // Mobile overlay
    if (isMobile) {
        return (
            <>
                {/* Mobile hamburger button */}
                <button
                    onClick={handleToggle}
                    className="fixed top-4 left-4 z-50 p-2 rounded-lg bg-glass-medium backdrop-blur-md border border-glass-border lg:hidden"
                    aria-label="Toggle menu"
                >
                    {sidebarOpen ? (
                        <X className="w-6 h-6 text-text-primary" />
                    ) : (
                        <Menu className="w-6 h-6 text-text-primary" />
                    )}
                </button>

                {/* Mobile overlay */}
                {sidebarOpen && (
                    <div
                        className="fixed inset-0 z-40 bg-black/50 backdrop-blur-sm lg:hidden"
                        onClick={() => dispatch(closeMobileSidebar())}
                    />
                )}

                {/* Mobile sidebar */}
                <aside
                    className={cn(
                        'fixed top-0 left-0 z-40 h-full w-64 bg-background-secondary border-r border-glass-border transform transition-transform duration-300 lg:hidden',
                        sidebarOpen ? 'translate-x-0' : '-translate-x-full'
                    )}
                >
                    <div className="flex flex-col h-full pt-16 px-3">
                        <div className="mb-6 px-3 text-center">
                            <h1 className="text-3xl text-center font-bold text-gradient" style={{ "width": "100%" }}>LifeOS</h1>
                        </div>

                        <nav className="flex-1 space-y-1">
                            {navItems.map((item) => (
                                <NavLink
                                    key={item.path}
                                    to={item.path}
                                    onClick={handleNavClick}
                                    className={({ isActive }) =>
                                        cn(
                                            'flex items-center gap-3 px-3 py-2.5 rounded-lg transition-all duration-200',
                                            isActive
                                                ? 'bg-accent-purple/20 text-accent-purple shadow-glow-sm'
                                                : 'text-text-secondary hover:text-text-primary hover:bg-background-hover'
                                        )
                                    }
                                >
                                    <item.icon className="w-5 h-5" />
                                    <span className="font-medium">{item.label}</span>
                                </NavLink>
                            ))}
                        </nav>

                        {/* Sign Out button */}
                        <div className="px-3 py-4 border-t border-glass-border">
                            <button
                                onClick={handleSignOut}
                                disabled={isLoggingOut}
                                className={cn(
                                    'flex items-center gap-3 w-full px-3 py-2.5 rounded-lg transition-all duration-200',
                                    'text-text-secondary hover:text-red-400 hover:bg-red-500/10',
                                    isLoggingOut && 'opacity-50 cursor-not-allowed'
                                )}
                            >
                                <LogOut className={cn('w-5 h-5', isLoggingOut && 'animate-pulse')} />
                                <span className="font-medium">{isLoggingOut ? 'Signing out...' : 'Sign Out'}</span>
                            </button>
                        </div>
                    </div>
                </aside>
            </>
        );
    }

    // Desktop sidebar
    return (
        <aside
            className={cn(
                'fixed top-0 left-0 h-full bg-background-secondary/50 backdrop-blur-lg border-r border-glass-border transition-all duration-300 z-40 hidden lg:flex lg:flex-col',
                sidebarCollapsed ? 'w-16' : 'w-64'
            )}
        >
            {/* Logo */}
            <div className="flex items-center justify-between h-16 px-4 border-b border-glass-border">
                {!sidebarCollapsed && (
                    <h1 className="text-3xl text-center font-bold text-gradient" style={{ "width": "100%" }}>LifeOS</h1>
                )}
                <button
                    onClick={handleToggle}
                    className="p-1.5 rounded-lg hover:bg-background-hover transition-colors"
                    aria-label={sidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
                >
                    {sidebarCollapsed ? (
                        <ChevronRight className="w-5 h-5 text-text-secondary" />
                    ) : (
                        <ChevronLeft className="w-5 h-5 text-text-secondary" />
                    )}
                </button>
            </div>

            {/* Navigation */}
            <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
                {navItems.map((item) => {
                    const isActive = location.pathname === item.path ||
                        (item.path !== '/' && location.pathname.startsWith(item.path));

                    return (
                        <NavLink
                            key={item.path}
                            to={item.path}
                            className={cn(
                                'flex items-center gap-3 px-3 py-2.5 rounded-lg transition-all duration-200 group',
                                isActive
                                    ? 'bg-gradient-to-r from-accent-purple/20 to-accent-cyan/10 text-accent-purple shadow-glow-sm'
                                    : 'text-text-secondary hover:text-text-primary hover:bg-background-hover'
                            )}
                            title={sidebarCollapsed ? item.label : undefined}
                        >
                            <item.icon
                                className={cn(
                                    'w-5 h-5 flex-shrink-0',
                                    isActive && 'drop-shadow-[0_0_8px_rgba(139,92,246,0.5)]'
                                )}
                            />
                            {!sidebarCollapsed && (
                                <span className="font-medium truncate">{item.label}</span>
                            )}
                        </NavLink>
                    );
                })}
            </nav>

            {/* Sign Out button */}
            <div className="px-3 py-4 border-t border-glass-border">
                <button
                    onClick={handleSignOut}
                    disabled={isLoggingOut}
                    className={cn(
                        'flex items-center gap-3 w-full px-3 py-2.5 rounded-lg transition-all duration-200',
                        'text-text-secondary hover:text-red-400 hover:bg-red-500/10',
                        isLoggingOut && 'opacity-50 cursor-not-allowed'
                    )}
                    title={sidebarCollapsed ? 'Sign Out' : undefined}
                >
                    <LogOut className={cn('w-5 h-5 flex-shrink-0', isLoggingOut && 'animate-pulse')} />
                    {!sidebarCollapsed && (
                        <span className="font-medium truncate">{isLoggingOut ? 'Signing out...' : 'Sign Out'}</span>
                    )}
                </button>
            </div>
        </aside>
    );
}
