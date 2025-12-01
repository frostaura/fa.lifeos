import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

interface AuthState {
  token: string | null;
  user: {
    id: string;
    email: string;
  } | null;
  isAuthenticated: boolean;
}

// Initialize from localStorage
const storedToken = localStorage.getItem('accessToken');
const storedUser = localStorage.getItem('user');

// Parse user safely - handle "undefined" string and invalid JSON
const parseUser = (userStr: string | null): { id: string; email: string } | null => {
  if (!userStr || userStr === 'undefined' || userStr === 'null') {
    return null;
  }
  try {
    return JSON.parse(userStr);
  } catch {
    return null;
  }
};

const initialState: AuthState = {
  token: storedToken || null,
  user: parseUser(storedUser),
  isAuthenticated: !!storedToken,
};

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    setCredentials: (
      state,
      action: PayloadAction<{ token: string; user: { id: string; email: string } }>
    ) => {
      state.token = action.payload.token;
      state.user = action.payload.user;
      state.isAuthenticated = true;
    },
    logout: (state) => {
      state.token = null;
      state.user = null;
      state.isAuthenticated = false;
    },
  },
});

export const { setCredentials, logout } = authSlice.actions;
export default authSlice.reducer;
