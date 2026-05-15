import { create } from 'zustand';
import { AuthResult } from '../types/api';

export interface AuthSession {
  accessToken: string;
  refreshToken: string;
  userId: string;
  name: string;
  email: string;
  role: AuthResult['role'];
}

interface AuthState {
  session: AuthSession | null;
  hydrated: boolean;
  setSession: (session: AuthSession) => void;
  clearSession: () => void;
  setHydrated: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  session: null,
  hydrated: false,
  setSession: (session) => set({ session }),
  clearSession: () => set({ session: null }),
  setHydrated: () => set({ hydrated: true }),
}));
