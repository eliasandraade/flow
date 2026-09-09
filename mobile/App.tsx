import React, { useEffect } from 'react';
import { View, ActivityIndicator } from 'react-native';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import * as SecureStore from 'expo-secure-store';
import { useAuthStore } from './src/store/authStore';
import { AppNavigator } from './src/navigation/AppNavigator';
import { theme } from './src/theme';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, staleTime: 30_000 } },
});

export default function App() {
  const { hydrated, setSession, setHydrated } = useAuthStore();

  useEffect(() => {
    async function hydrate() {
      try {
        const accessToken = await SecureStore.getItemAsync('accessToken');
        const userId = await SecureStore.getItemAsync('userId');
        const role = await SecureStore.getItemAsync('role');
        if (accessToken && userId && role) {
          const refreshToken = await SecureStore.getItemAsync('refreshToken') ?? '';
          const name = await SecureStore.getItemAsync('name') ?? '';
          const email = await SecureStore.getItemAsync('email') ?? '';
          setSession({ accessToken, refreshToken, userId, name, email, role: role as any });
        }
      } finally {
        setHydrated();
      }
    }
    hydrate();
  }, []);

  if (!hydrated) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center' }}>
        <ActivityIndicator size="large" color={theme.colors.primary} />
      </View>
    );
  }

  return (
    <QueryClientProvider client={queryClient}>
      <SafeAreaProvider>
        <AppNavigator />
      </SafeAreaProvider>
    </QueryClientProvider>
  );
}
