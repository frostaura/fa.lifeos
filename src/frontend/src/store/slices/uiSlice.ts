import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

interface UIState {
  sidebarCollapsed: boolean;
  sidebarOpen: boolean; // For mobile
  theme: 'dark' | 'light';
  currency: string;
}

const initialState: UIState = {
  sidebarCollapsed: false,
  sidebarOpen: false,
  theme: 'dark',
  currency: 'ZAR',
};

const uiSlice = createSlice({
  name: 'ui',
  initialState,
  reducers: {
    toggleSidebar: (state) => {
      state.sidebarCollapsed = !state.sidebarCollapsed;
    },
    toggleMobileSidebar: (state) => {
      state.sidebarOpen = !state.sidebarOpen;
    },
    closeMobileSidebar: (state) => {
      state.sidebarOpen = false;
    },
    setCurrency: (state, action: PayloadAction<string>) => {
      state.currency = action.payload;
    },
    setTheme: (state, action: PayloadAction<'dark' | 'light'>) => {
      state.theme = action.payload;
    },
  },
});

export const {
  toggleSidebar,
  toggleMobileSidebar,
  closeMobileSidebar,
  setCurrency,
  setTheme,
} = uiSlice.actions;

export default uiSlice.reducer;
