# Frontend Design Guide

## Technology Stack (Default)

### Core Framework
- **Framework**: React 18+
- **Language**: TypeScript 5+
- **Build Tool**: Vite
- **Package Manager**: npm
- **Linting**: ESLint + Prettier

### State Management
- **Global State**: Redux Toolkit
- **Server State**: RTK Query
- **Form State**: React Hook Form
- **URL State**: React Router v6

### PWA Requirements (MANDATORY)
- **Service Worker**: Workbox
- **Offline Storage**: IndexedDB + Cache API
- **Sync**: Background Sync API
- **Install**: Web App Manifest
- **Updates**: Prompt for new versions

## Project Structure

```
src/
├── components/          # Reusable UI components
│   ├── common/         # Buttons, Inputs, Cards
│   ├── layout/         # Header, Footer, Sidebar
│   └── features/       # Feature-specific components
├── pages/              # Route components
├── hooks/              # Custom React hooks
├── services/           # API and external services
├── store/              # State management
├── utils/              # Helper functions
├── types/              # TypeScript definitions
├── styles/             # Global styles and themes
└── assets/             # Images, fonts, icons
```

## Component Architecture

### Component Types

#### Presentational Components
```tsx
// Pure UI components with no business logic
interface ButtonProps {
  variant: 'primary' | 'secondary';
  size: 'small' | 'medium' | 'large';
  onClick: () => void;
  children: React.ReactNode;
}

const Button: React.FC<ButtonProps> = ({ variant, size, onClick, children }) => {
  return (
    <button className={`btn btn-${variant} btn-${size}`} onClick={onClick}>
      {children}
    </button>
  );
};
```

#### Container Components
```tsx
// Business logic and state management
const UserListContainer: React.FC = () => {
  const { data, loading, error } = useQuery('users');

  if (loading) return <Spinner />;
  if (error) return <ErrorMessage error={error} />;

  return <UserList users={data} />;
};
```

## Styling Strategy

### CSS Architecture
- **Methodology**: [BEM/Atomic/CSS Modules]
- **Preprocessor**: [SASS/PostCSS/CSS-in-JS]
- **Framework**: [Tailwind/Bootstrap/Material-UI]

### Design Tokens
```css
:root {
  /* Colors */
  --color-primary: #007bff;
  --color-secondary: #6c757d;
  --color-success: #28a745;
  --color-danger: #dc3545;

  /* Typography */
  --font-family: 'Inter', sans-serif;
  --font-size-base: 16px;
  --line-height-base: 1.5;

  /* Spacing */
  --spacing-xs: 4px;
  --spacing-sm: 8px;
  --spacing-md: 16px;
  --spacing-lg: 24px;
  --spacing-xl: 32px;

  /* Breakpoints */
  --breakpoint-sm: 576px;
  --breakpoint-md: 768px;
  --breakpoint-lg: 992px;
  --breakpoint-xl: 1200px;
}
```

## Routing

### Route Structure
```javascript
const routes = [
  { path: '/', component: Home },
  { path: '/login', component: Login, public: true },
  { path: '/dashboard', component: Dashboard, requiresAuth: true },
  { path: '/users/:id', component: UserProfile },
  { path: '/settings/*', component: Settings, children: [...] },
  { path: '*', component: NotFound }
];
```

### Route Guards
```javascript
const ProtectedRoute = ({ children, requiresAuth, requiredRole }) => {
  const { user, loading } = useAuth();

  if (loading) return <LoadingSpinner />;
  if (requiresAuth && !user) return <Navigate to="/login" />;
  if (requiredRole && user.role !== requiredRole) return <AccessDenied />;

  return children;
};
```

## Data Fetching

### API Integration
```typescript
class ApiService {
  private baseUrl = process.env.REACT_APP_API_URL;

  async get<T>(endpoint: string): Promise<T> {
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      headers: this.getHeaders(),
    });
    return this.handleResponse(response);
  }

  private getHeaders(): HeadersInit {
    return {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${getToken()}`,
    };
  }
}
```

### Data Fetching Patterns
```tsx
// Using React Query
const useUsers = () => {
  return useQuery({
    queryKey: ['users'],
    queryFn: () => apiService.get('/users'),
    staleTime: 5 * 60 * 1000, // 5 minutes
    cacheTime: 10 * 60 * 1000, // 10 minutes
  });
};

// Optimistic Updates
const updateUser = useMutation({
  mutationFn: (user) => apiService.put(`/users/${user.id}`, user),
  onMutate: async (newUser) => {
    await queryClient.cancelQueries(['users']);
    const previousUsers = queryClient.getQueryData(['users']);
    queryClient.setQueryData(['users'], old => [...old, newUser]);
    return { previousUsers };
  },
  onError: (err, newUser, context) => {
    queryClient.setQueryData(['users'], context.previousUsers);
  },
  onSettled: () => {
    queryClient.invalidateQueries(['users']);
  },
});
```

## Forms & Validation

### Form Management
```tsx
const schema = yup.object({
  email: yup.string().email().required(),
  password: yup.string().min(8).required(),
});

const LoginForm = () => {
  const { register, handleSubmit, formState: { errors } } = useForm({
    resolver: yupResolver(schema)
  });

  const onSubmit = async (data) => {
    await login(data);
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <Input {...register('email')} error={errors.email} />
      <Input {...register('password')} type="password" error={errors.password} />
      <Button type="submit">Login</Button>
    </form>
  );
};
```

## Performance Optimization

### Code Splitting
```javascript
// Route-based splitting
const Dashboard = lazy(() => import('./pages/Dashboard'));

// Component-based splitting
const HeavyComponent = lazy(() => import('./components/HeavyComponent'));
```

### Optimization Techniques
- React.memo for expensive components
- useMemo for expensive calculations
- useCallback for stable function references
- Virtual scrolling for long lists
- Image lazy loading
- Bundle size analysis

## Testing Strategy

### Testing Pyramid
```
         /\
        /E2E\        Playwright/Cypress
       /------\
      /Integra-\     React Testing Library
     /  tion    \
    /------------\
   /    Unit      \  Jest/Vitest
  /________________\
```

### Test Examples
```tsx
// Unit Test
describe('Button', () => {
  it('renders with correct text', () => {
    render(<Button>Click me</Button>);
    expect(screen.getByText('Click me')).toBeInTheDocument();
  });
});

// Integration Test
describe('LoginForm', () => {
  it('submits with valid data', async () => {
    render(<LoginForm />);
    await userEvent.type(screen.getByLabelText('Email'), 'test@example.com');
    await userEvent.type(screen.getByLabelText('Password'), 'password123');
    await userEvent.click(screen.getByRole('button', { name: 'Login' }));
    expect(mockLogin).toHaveBeenCalledWith({
      email: 'test@example.com',
      password: 'password123'
    });
  });
});
```

## Accessibility (a11y)

### WCAG 2.1 AA Compliance
- Semantic HTML
- ARIA labels and roles
- Keyboard navigation
- Focus management
- Color contrast ratios
- Screen reader support

### Implementation
```tsx
<button
  aria-label="Close dialog"
  aria-pressed={isPressed}
  aria-disabled={isDisabled}
  role="button"
  tabIndex={0}
  onKeyDown={handleKeyDown}
>
  <span aria-hidden="true">×</span>
</button>
```

## Build & Deployment

### Environment Configuration
```javascript
// .env.development
REACT_APP_API_URL=http://localhost:3000
REACT_APP_ENV=development

// .env.production
REACT_APP_API_URL=https://api.example.com
REACT_APP_ENV=production
```

### Build Optimization
```javascript
// vite.config.js
export default {
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['react', 'react-dom'],
          utils: ['lodash', 'date-fns'],
        },
      },
    },
    minify: 'terser',
    sourcemap: true,
  },
};
```

---

## Feature: Enhanced Dimensions Pages

### Overview
The Dimensions feature provides a comprehensive view of life dimensions with goal tracking, task management, and metric visualization. This enhancement addresses user feedback about limited functionality.

### User Requirements Addressed
1. **More functionality on Dimensions page** - Add summary stats, quick actions, metric previews
2. **Task management** - Full CRUD for tasks within dimension context
3. **Info/help text** - Collapsible info sections explaining each dimension
4. **Goals linked to metrics** - Visual metric progress for milestones with targets

### Page: Dimensions List (`/dimensions`)

#### Layout Structure
```
┌─────────────────────────────────────────────────────────────┐
│ Dimensions                                                   │
│ Track and optimize all areas of your life                   │
├─────────────────────────────────────────────────────────────┤
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────┐ │
│ │ [Icon]      │ │ [Icon]      │ │ [Icon]      │ │ [Icon]  │ │
│ │ Health   >  │ │ Mind     >  │ │ Work     >  │ │ Money > │ │
│ │             │ │             │ │             │ │         │ │
│ │ Score: 78   │ │ Score: 85   │ │ Score: 62   │ │ Scr: 71 │ │
│ │ ████████░░  │ │ █████████░  │ │ ██████░░░░  │ │ ███████░│ │
│ │             │ │             │ │             │ │         │ │
│ │ 3 tasks     │ │ 2 tasks     │ │ 5 tasks     │ │ 1 task  │ │
│ │ 2 milestones│ │ 1 milestone │ │ 3 milestones│ │ 2 miles │ │
│ │             │ │             │ │             │ │         │ │
│ │ [⚡ Quick]  │ │ [⚡ Quick]  │ │ [⚡ Quick]  │ │[⚡Quick]│ │
│ └─────────────┘ └─────────────┘ └─────────────┘ └─────────┘ │
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────┐ │
│ │ Relation... │ │ Play        │ │ Growth      │ │Community│ │
│ └─────────────┘ └─────────────┘ └─────────────┘ └─────────┘ │
└─────────────────────────────────────────────────────────────┘
```

#### Enhanced Card Information
Each dimension card displays:
- **Icon & Name** - Visual identifier
- **Score with progress bar** - Current dimension score (0-100)
- **Active counts** - Number of tasks and milestones
- **Quick Action button** - Opens quick-add modal (task or milestone)
- **Trend indicator** - Optional score trend (up/down/stable)

#### Quick Actions Dropdown
```tsx
interface QuickAction {
  label: string;
  icon: React.ComponentType;
  action: () => void;
}

// Actions per dimension card
const quickActions: QuickAction[] = [
  { label: 'Add Task', icon: PlusCircle, action: openAddTaskModal },
  { label: 'Add Milestone', icon: Target, action: openAddMilestoneModal },
  { label: 'View Metrics', icon: BarChart2, action: navigateToMetrics },
];
```

### Page: Dimension Detail (`/dimensions/:dimensionId`)

#### Layout Structure
```
┌─────────────────────────────────────────────────────────────┐
│ ← Back to Dimensions                                        │
├─────────────────────────────────────────────────────────────┤
│ [Icon]  Health                                              │
│         Physical well-being and fitness                     │
├─────────────────────────────────────────────────────────────┤
│ ┌─ ⓘ What is Health? ─────────────────────────────────────┐ │
│ │ The Health dimension tracks your physical well-being...  │ │
│ │ This includes exercise habits, nutrition metrics,        │ │
│ │ sleep quality, and health-related goals.                 │ │
│ │                                                          │ │
│ │ Key areas: Exercise • Nutrition • Sleep • Medical        │ │
│ └─────────────────────────────────────────────[Collapse]──┘ │
├─────────────────────────────────────────────────────────────┤
│ ┌────────────┐ ┌────────────┐ ┌────────────┐               │
│ │ Score      │ │ Weight     │ │ Active     │               │
│ │   78/100   │ │   12.5%    │ │  5 items   │               │
│ │ ████████░░ │ │            │ │ 2M + 3T    │               │
│ └────────────┘ └────────────┘ └────────────┘               │
├─────────────────────────────────────────────────────────────┤
│ 📊 METRICS                                    [View All →] │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Weight        68kg → 65kg target    ████████░░ 85%      │ │
│ │ Steps/Day     8,500 → 10,000 target ████████░░ 85%      │ │
│ │ Sleep Hours   7.2 → 8.0 target      █████████░ 90%      │ │
│ └─────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│ 🎯 MILESTONES                               [+ Add Milestone]│
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ ○ Run a marathon         Target: Dec 2025              │ │
│ │   └─ Linked: Running distance ≥ 42km  [███░░░░ 28%]    │ │
│ │                                        [Edit] [Delete] │ │
│ ├─────────────────────────────────────────────────────────┤ │
│ │ ● Reach target weight    Completed: Nov 2024           │ │
│ │   └─ Linked: Weight ≤ 70kg            [██████████ ✓]   │ │
│ └─────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│ ✅ TASKS                                        [+ Add Task]│
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Filter: [All▾] [Active▾] [Type▾]           Search: [__] │ │
│ ├─────────────────────────────────────────────────────────┤ │
│ │ □ Morning run              Habit  Daily    [✓][✎][🗑]   │ │
│ │   └─ Streak: 12 days 🔥                                │ │
│ │ □ Gym session              Habit  3x/week  [✓][✎][🗑]   │ │
│ │ □ Book doctor appointment  One-off         [✓][✎][🗑]   │ │
│ │ ☑ Meal prep Sunday         Scheduled Dec 15 [↩][✎][🗑]  │ │
│ └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

#### Section: Dimension Info (Collapsible)

```tsx
interface DimensionInfo {
  dimensionCode: string;
  title: string;           // "What is {dimension}?"
  description: string;     // Multi-paragraph explanation
  keyAreas: string[];      // Bullet points of focus areas
  tips?: string[];         // Optional usage tips
}

// Info content per dimension (stored in frontend constants or fetched)
const DIMENSION_INFO: Record<string, DimensionInfo> = {
  health: {
    dimensionCode: 'health',
    title: 'What is Health?',
    description: 'The Health dimension tracks your physical well-being...',
    keyAreas: ['Exercise', 'Nutrition', 'Sleep', 'Medical Checkups'],
    tips: ['Set realistic daily step goals', 'Track water intake']
  },
  // ... other dimensions
};
```

#### Section: Linked Metrics Display

```tsx
interface LinkedMetricDisplay {
  code: string;
  name: string;
  currentValue: number | null;
  targetValue: number | null;
  targetDirection: 'AtOrAbove' | 'AtOrBelow';
  unit: string;
  progressPercent: number;    // Calculated: (current/target) * 100
  trend: 'up' | 'down' | 'stable';
}

// Metric card component
<MetricProgressCard
  metric={metric}
  showTarget={true}
  onClick={() => navigate(`/metrics/${metric.code}`)}
/>
```

#### Section: Milestones with Metric Linkage

Enhanced milestone display showing:
- Title and target date
- Status indicator (active/completed/abandoned)
- **Linked metric progress** (if `targetMetricCode` is set):
  - Metric name and current value
  - Target value with direction indicator
  - Progress bar with percentage
- Edit/Delete actions

```tsx
interface MilestoneWithProgress {
  id: string;
  title: string;
  description?: string;
  targetDate?: string;
  status: 'active' | 'completed' | 'abandoned';
  // Metric linkage
  targetMetricCode?: string;
  targetMetricValue?: number;
  // Computed from metric data
  currentMetricValue?: number;
  metricName?: string;
  metricUnit?: string;
  progressPercent?: number;
}
```

#### Section: Task Management (Full CRUD)

##### Task List Features
- **Filtering**: By type (habit/one_off/scheduled), status (active/completed), search
- **Sorting**: By due date, title, creation date
- **Inline actions**: Complete, Edit, Delete, Uncomplete (for mistakes)

##### Task Item Display
```tsx
interface TaskDisplay {
  id: string;
  title: string;
  description?: string;
  taskType: 'habit' | 'one_off' | 'scheduled_event';
  frequency?: string;
  scheduledDate?: string;
  isCompleted: boolean;
  isActive: boolean;
  // For habits
  streakDays?: number;
  linkedMetricCode?: string;
  // UI state
  isExpanded: boolean;
}

// Task row with actions
<TaskRow
  task={task}
  onComplete={() => completeTask(task.id)}
  onEdit={() => openEditModal(task)}
  onDelete={() => confirmDelete(task.id)}
  onUncomplete={() => uncompleteTask(task.id)}
/>
```

##### Add/Edit Task Modal

```tsx
interface TaskFormData {
  title: string;
  description?: string;
  taskType: 'habit' | 'one_off' | 'scheduled_event';
  frequency?: 'daily' | 'weekly' | 'monthly' | 'ad_hoc';
  dimensionId: string;        // Pre-filled from context
  milestoneId?: string;       // Optional: link to milestone
  linkedMetricCode?: string;  // Optional: for habits
  scheduledDate?: string;     // For scheduled_event
  scheduledTime?: string;
  startDate?: string;
  endDate?: string;
  tags?: string[];
}

// Form sections
1. Basic Info (title, description)
2. Task Type selector with contextual fields
3. Scheduling (frequency, dates)
4. Linking (milestone dropdown, metric dropdown)
5. Tags (multi-select or free-form)
```

### Component Hierarchy

```
src/pages/
├── Dimensions.tsx (enhanced)
│   └── components/
│       ├── DimensionCard.tsx (enhanced with counts, quick actions)
│       └── QuickActionDropdown.tsx
│
└── DimensionDetail.tsx (enhanced)
    └── components/
        ├── DimensionInfoSection.tsx (collapsible info)
        ├── DimensionStatsRow.tsx (score, weight, counts)
        ├── LinkedMetricsSection.tsx (metric progress cards)
        ├── MilestonesSection.tsx (with metric progress)
        │   ├── MilestoneCard.tsx
        │   └── AddMilestoneModal.tsx (enhanced)
        └── TasksSection.tsx (full CRUD)
            ├── TaskFilters.tsx
            ├── TaskList.tsx
            ├── TaskRow.tsx
            └── AddEditTaskModal.tsx
```

### State Management (RTK Query)

```typescript
// New/enhanced endpoints needed
const tasksApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    // Get tasks for dimension with full details
    getDimensionTasks: builder.query<TaskListResponse, {
      dimensionId: string;
      taskType?: string;
      isCompleted?: boolean;
      isActive?: boolean;
      page?: number;
      perPage?: number;
    }>({
      query: (params) => ({
        url: '/api/tasks',
        params: {
          dimensionId: params.dimensionId,
          taskType: params.taskType,
          isCompleted: params.isCompleted,
          isActive: params.isActive,
          page: params.page || 1,
          perPage: params.perPage || 50,
        },
      }),
      providesTags: ['Tasks', 'Dimensions'],
    }),

    // Create task
    createTask: builder.mutation<TaskDetailResponse, CreateTaskRequest>({
      query: (body) => ({
        url: '/api/tasks',
        method: 'POST',
        body,
      }),
      invalidatesTags: ['Tasks', 'Dimensions', 'Dashboard'],
    }),

    // Update task
    updateTask: builder.mutation<TaskDetailResponse, { id: string } & UpdateTaskRequest>({
      query: ({ id, ...body }) => ({
        url: `/api/tasks/${id}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: ['Tasks', 'Dimensions'],
    }),

    // Complete task
    completeTask: builder.mutation<TaskCompleteResponse, { id: string; metricValue?: number }>({
      query: ({ id, metricValue }) => ({
        url: `/api/tasks/${id}/complete`,
        method: 'POST',
        body: { metricValue },
      }),
      invalidatesTags: ['Tasks', 'Dimensions', 'Dashboard', 'Streaks'],
    }),

    // Delete task
    deleteTask: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/tasks/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Tasks', 'Dimensions', 'Dashboard'],
    }),
  }),
});

// Enhanced dimension query to include metrics
const dimensionsApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    getDimensionWithMetrics: builder.query<DimensionDetailWithMetrics, string>({
      query: (id) => `/api/dimensions/${id}?include=metrics`,
      providesTags: (result, error, id) => [{ type: 'Dimensions', id }],
    }),
  }),
});
```

### TypeScript Types

```typescript
// Task types (add to types/index.ts)
export interface LifeTask {
  id: string;
  title: string;
  description?: string;
  taskType: 'habit' | 'one_off' | 'scheduled_event';
  frequency: 'daily' | 'weekly' | 'monthly' | 'ad_hoc';
  dimensionId?: string;
  dimensionCode?: string;
  milestoneId?: string;
  linkedMetricCode?: string;
  scheduledDate?: string;
  scheduledTime?: string;
  startDate: string;
  endDate?: string;
  isCompleted: boolean;
  completedAt?: string;
  isActive: boolean;
  tags?: string[];
  // Computed
  streakDays?: number;
}

export interface CreateTaskRequest {
  title: string;
  description?: string;
  taskType: string;
  frequency?: string;
  dimensionId?: string;
  milestoneId?: string;
  linkedMetricCode?: string;
  scheduledDate?: string;
  scheduledTime?: string;
  startDate?: string;
  endDate?: string;
  tags?: string[];
}

export interface UpdateTaskRequest {
  title?: string;
  description?: string;
  frequency?: string;
  scheduledDate?: string;
  scheduledTime?: string;
  endDate?: string;
  isActive?: boolean;
  tags?: string[];
}

export interface TaskListResponse {
  data: Array<{
    id: string;
    type: 'task';
    attributes: LifeTask;
  }>;
  meta: {
    page: number;
    perPage: number;
    total: number;
    totalPages: number;
  };
}

// Enhanced dimension types
export interface DimensionWithMetrics extends DimensionDetailData {
  linkedMetrics: LinkedMetricDisplay[];
}

export interface LinkedMetricDisplay {
  code: string;
  name: string;
  currentValue: number | null;
  targetValue: number | null;
  targetDirection: 'AtOrAbove' | 'AtOrBelow';
  unit: string;
  progressPercent: number;
}
```

### UI/UX Specifications

#### Info Section Behavior
- **Default state**: Collapsed (shows only title)
- **Expanded state**: Shows full description, key areas, tips
- **Persistence**: Remember collapse state in localStorage per dimension
- **Animation**: Smooth expand/collapse transition (200ms)

#### Task Actions
| Action | Icon | Behavior |
|--------|------|----------|
| Complete | ✓ (CheckCircle) | Mark complete, update streak if habit |
| Edit | ✎ (Pencil) | Open edit modal |
| Delete | 🗑 (Trash) | Confirm dialog, then delete |
| Uncomplete | ↩ (Undo) | Only for recently completed, reactivates task |

#### Milestone Progress Display
- Progress bar color based on percentage:
  - 0-25%: Gray
  - 26-50%: Yellow
  - 51-75%: Blue
  - 76-99%: Green
  - 100%: Green with checkmark
- Show "On track" / "Behind" indicator based on target date proximity

#### Responsive Breakpoints
- **Mobile (< 640px)**: Stack all sections vertically, full-width cards
- **Tablet (640-1024px)**: 2-column grid for tasks/milestones
- **Desktop (> 1024px)**: 3-column stats, side-by-side sections

### Accessibility Requirements
- All interactive elements keyboard accessible
- Proper ARIA labels on action buttons
- Screen reader announcements for task completion
- Focus management when modals open/close
- Color contrast meets WCAG 2.1 AA