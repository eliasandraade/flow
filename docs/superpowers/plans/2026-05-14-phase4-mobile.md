# Phase 4: Mobile Application Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a working React Native (Expo) mobile app connecting to the live Flow API, demonstrating the full system across Operator, Manager, and Leadership roles.

**Architecture:** Expo managed workflow, TypeScript. On startup `App.tsx` hydrates auth from SecureStore into Zustand. `AppNavigator` gates on auth state and routes by role: Operator → idea creation/review stack, Manager → tabbed idea+project stacks, Leadership → dashboard screen. All API calls go through a typed `apiFetch` wrapper that attaches the JWT and clears auth on 401.

**Tech Stack:** React Native (Expo SDK 51), TypeScript, React Navigation 6 (native-stack + bottom-tabs), TanStack Query v5, Zustand v5, expo-secure-store.

---

## File Structure

```
mobile/                                  ← created at repo root
├── App.tsx                              # Root: hydrate auth from SecureStore, render AppNavigator
├── app.json
├── package.json
├── tsconfig.json
└── src/
    ├── api/
    │   └── client.ts                    # apiFetch + logout utility
    ├── store/
    │   └── authStore.ts                 # Zustand: { session, setSession, clearSession, hydrated }
    ├── navigation/
    │   └── AppNavigator.tsx             # Auth gate + role-based stack/tab routing
    ├── types/
    │   └── api.ts                       # TypeScript interfaces mirroring API DTOs
    └── screens/
        ├── auth/
        │   └── LoginScreen.tsx
        ├── operator/
        │   ├── MyIdeasScreen.tsx        # Lists own ideas; links to submit + detail
        │   ├── SubmitIdeaScreen.tsx     # Form: POST /ideas
        │   └── IdeaDetailScreen.tsx     # Detail + "Submit for Review" if Draft
        ├── manager/
        │   ├── IdeaQueueScreen.tsx      # All ideas; links to detail for review
        │   ├── ManagerIdeaDetailScreen.tsx  # Approve / Reject actions
        │   ├── ProjectListScreen.tsx    # All projects with status
        │   └── ProjectDetailScreen.tsx # State transition actions (start/complete/block/unblock)
        └── leadership/
            └── DashboardScreen.tsx      # 7 KPI metrics + blocked project list
```

---

## API Reference

Base URL: `http://localhost:5153/api/v1`
> **Note:** On an Android emulator replace `localhost` with `10.0.2.2`. On a real device replace with your machine's LAN IP (e.g. `192.168.1.X`).

| Method | Path | Role | Purpose |
|--------|------|------|---------|
| POST | `/auth/login` | Public | Login → `AuthResult` |
| POST | `/auth/logout` | Any | Revoke refresh token |
| GET | `/ideas` | Any | List ideas (scoped by role server-side) |
| POST | `/ideas` | Operator | Create idea |
| GET | `/ideas/{id}` | Any | Idea detail |
| POST | `/ideas/{id}/submit` | Operator | Submit draft for review |
| POST | `/ideas/{id}/approve` | Manager | Approve idea |
| POST | `/ideas/{id}/reject` | Manager | Reject idea (comment required) |
| GET | `/projects` | Any | List projects |
| GET | `/projects/{id}` | Any | Project detail |
| POST | `/projects/{id}/start` | Manager | Planning → InProgress |
| POST | `/projects/{id}/complete` | Manager | InProgress → Completed |
| POST | `/projects/{id}/block` | Manager | InProgress → Blocked (reason required) |
| POST | `/projects/{id}/unblock` | Manager | Blocked → InProgress |
| GET | `/dashboard/summary` | Manager/Leadership | 7-metric dashboard |

---

## Task 15: Scaffold + Authentication Foundation

**Files:**
- Create: `mobile/` (full Expo scaffold)
- Create: `mobile/src/types/api.ts`
- Create: `mobile/src/api/client.ts`
- Create: `mobile/src/store/authStore.ts`
- Create: `mobile/src/screens/auth/LoginScreen.tsx`
- Create: `mobile/src/navigation/AppNavigator.tsx`
- Modify: `mobile/App.tsx`

- [ ] **Step 1: Scaffold the Expo app**

Run from the repo root (`C:\Users\Elias\Documents\Faculdade\Flow`):

```bash
npx create-expo-app@latest mobile --template blank-typescript
```

Expected: `mobile/` directory created containing `App.tsx`, `app.json`, `package.json`, `tsconfig.json`, and `node_modules/`.

- [ ] **Step 2: Install dependencies**

```bash
cd mobile
npx expo install expo-secure-store react-native-safe-area-context react-native-screens
npm install @react-navigation/native @react-navigation/native-stack @react-navigation/bottom-tabs
npm install @tanstack/react-query zustand
```

Expected: All packages present in `node_modules/` with no peer dependency errors.

- [ ] **Step 3: Create `src/types/api.ts`**

Create `mobile/src/types/api.ts`:

```typescript
export interface AuthResult {
  accessToken: string;
  refreshToken: string;
  userId: string;
  name: string;
  email: string;
  role: 'Operator' | 'Manager' | 'Leadership';
}

export interface IdeaSummary {
  id: string;
  title: string;
  problem: string;
  status: string;
  priority: string;
  submittedBy: string;
  linkedGuidelineId: string | null;
  createdAt: string;
}

export interface IdeaDetail {
  id: string;
  title: string;
  description: string;
  problem: string;
  status: string;
  priority: string;
  submittedBy: string;
  managerComment: string | null;
  linkedGuidelineId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ProjectSummary {
  id: string;
  title: string;
  status: string;
  priority: string;
  ownerId: string;
  sourceIdeaId: string | null;
  deadline: string | null;
  blockedReason: string | null;
  createdAt: string;
}

export interface ProjectDetail {
  id: string;
  title: string;
  description: string;
  status: string;
  priority: string;
  ownerId: string;
  sourceIdeaId: string | null;
  estimatedCost: number | null;
  actualCost: number | null;
  startDate: string | null;
  deadline: string | null;
  completedAt: string | null;
  blockedReason: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface BlockedProject {
  id: string;
  title: string;
  ownerId: string;
  blockedReason: string;
  daysBlocked: number;
}

export interface DashboardSummary {
  totalIdeas: number;
  approvedIdeas: number;
  rejectedIdeas: number;
  pendingIdeas: number;
  conversionRate: number;
  activeProjects: number;
  blockedProjects: number;
  completedProjects: number;
  totalRoi: number;
  averageCompletionDays: number;
  bottleneckIndex: number;
  blockedProjectList: BlockedProject[];
}
```

- [ ] **Step 4: Create `src/store/authStore.ts`**

Create `mobile/src/store/authStore.ts`:

```typescript
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
```

- [ ] **Step 5: Create `src/api/client.ts`**

Create `mobile/src/api/client.ts`:

```typescript
import * as SecureStore from 'expo-secure-store';
import { useAuthStore } from '../store/authStore';

// Android emulator: http://10.0.2.2:5153/api/v1
// iOS simulator / dev machine: http://localhost:5153/api/v1
// Real device on LAN: replace localhost with machine IP
export const API_BASE = 'http://localhost:5153/api/v1';

const STORE_KEYS = ['accessToken', 'refreshToken', 'userId', 'name', 'email', 'role'] as const;

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const token = await SecureStore.getItemAsync('accessToken');

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE}${path}`, { ...options, headers });

  if (response.status === 401) {
    // Token expired — wipe session; AppNavigator re-renders to LoginScreen
    for (const key of STORE_KEYS) {
      await SecureStore.deleteItemAsync(key);
    }
    useAuthStore.getState().clearSession();
    throw new Error('Session expired. Please log in again.');
  }

  if (!response.ok) {
    let message = `HTTP ${response.status}`;
    try {
      const body = await response.json();
      message = body?.detail ?? body?.title ?? body?.errors?.[0] ?? message;
    } catch {
      // non-JSON error body — keep default message
    }
    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as unknown as T;
  }

  return response.json() as Promise<T>;
}

export async function logout(refreshToken: string): Promise<void> {
  try {
    await apiFetch('/auth/logout', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
    });
  } catch {
    // best-effort — always clear local state regardless
  }
  for (const key of STORE_KEYS) {
    await SecureStore.deleteItemAsync(key);
  }
  useAuthStore.getState().clearSession();
}
```

- [ ] **Step 6: Create `src/screens/auth/LoginScreen.tsx`**

Create `mobile/src/screens/auth/LoginScreen.tsx`:

```typescript
import React, { useState } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  Alert,
  ActivityIndicator,
} from 'react-native';
import * as SecureStore from 'expo-secure-store';
import { API_BASE } from '../../api/client';
import { useAuthStore } from '../../store/authStore';
import { AuthResult } from '../../types/api';

export function LoginScreen() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const setSession = useAuthStore((s) => s.setSession);

  async function handleLogin() {
    if (!email.trim() || !password.trim()) {
      Alert.alert('Validation', 'Email and password are required.');
      return;
    }
    setLoading(true);
    try {
      const response = await fetch(`${API_BASE}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      });
      if (!response.ok) {
        const body = await response.json().catch(() => ({}));
        throw new Error(body?.detail ?? body?.title ?? 'Invalid credentials');
      }
      const data: AuthResult = await response.json();

      // Persist to SecureStore so auth survives app restarts
      await SecureStore.setItemAsync('accessToken', data.accessToken);
      await SecureStore.setItemAsync('refreshToken', data.refreshToken);
      await SecureStore.setItemAsync('userId', data.userId);
      await SecureStore.setItemAsync('name', data.name);
      await SecureStore.setItemAsync('email', data.email);
      await SecureStore.setItemAsync('role', data.role);

      setSession({
        accessToken: data.accessToken,
        refreshToken: data.refreshToken,
        userId: data.userId,
        name: data.name,
        email: data.email,
        role: data.role,
      });
    } catch (err: any) {
      Alert.alert('Login Failed', err.message ?? 'Unknown error');
    } finally {
      setLoading(false);
    }
  }

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Flow</Text>
      <Text style={styles.subtitle}>Innovation Management</Text>
      <TextInput
        style={styles.input}
        placeholder="Email"
        autoCapitalize="none"
        keyboardType="email-address"
        value={email}
        onChangeText={setEmail}
      />
      <TextInput
        style={styles.input}
        placeholder="Password"
        secureTextEntry
        value={password}
        onChangeText={setPassword}
      />
      {loading ? (
        <ActivityIndicator size="large" color="#2563EB" style={{ marginTop: 8 }} />
      ) : (
        <TouchableOpacity style={styles.button} onPress={handleLogin}>
          <Text style={styles.buttonText}>Sign In</Text>
        </TouchableOpacity>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1, justifyContent: 'center', padding: 24, backgroundColor: '#F9FAFB',
  },
  title: {
    fontSize: 36, fontWeight: 'bold', color: '#1E3A5F', textAlign: 'center',
  },
  subtitle: {
    fontSize: 14, color: '#6B7280', textAlign: 'center', marginBottom: 36,
  },
  input: {
    borderWidth: 1, borderColor: '#D1D5DB', borderRadius: 8,
    padding: 12, marginBottom: 12, backgroundColor: '#FFF', fontSize: 16,
  },
  button: {
    backgroundColor: '#2563EB', borderRadius: 8,
    padding: 14, alignItems: 'center', marginTop: 8,
  },
  buttonText: { color: '#FFF', fontWeight: '600', fontSize: 16 },
});
```

- [ ] **Step 7: Create `src/navigation/AppNavigator.tsx`**

Create `mobile/src/navigation/AppNavigator.tsx`:

```typescript
import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { useAuthStore } from '../store/authStore';
import { LoginScreen } from '../screens/auth/LoginScreen';
import { MyIdeasScreen } from '../screens/operator/MyIdeasScreen';
import { SubmitIdeaScreen } from '../screens/operator/SubmitIdeaScreen';
import { IdeaDetailScreen } from '../screens/operator/IdeaDetailScreen';
import { IdeaQueueScreen } from '../screens/manager/IdeaQueueScreen';
import { ManagerIdeaDetailScreen } from '../screens/manager/ManagerIdeaDetailScreen';
import { ProjectListScreen } from '../screens/manager/ProjectListScreen';
import { ProjectDetailScreen } from '../screens/manager/ProjectDetailScreen';
import { DashboardScreen } from '../screens/leadership/DashboardScreen';

const AuthStack = createNativeStackNavigator();
const OperatorStack = createNativeStackNavigator();
const ManagerIdeasStack = createNativeStackNavigator();
const ManagerProjectsStack = createNativeStackNavigator();
const ManagerTabs = createBottomTabNavigator();
const LeadershipStack = createNativeStackNavigator();

function OperatorNavigator() {
  return (
    <OperatorStack.Navigator>
      <OperatorStack.Screen
        name="MyIdeas"
        component={MyIdeasScreen}
        options={{ title: 'My Ideas' }}
      />
      <OperatorStack.Screen
        name="SubmitIdea"
        component={SubmitIdeaScreen}
        options={{ title: 'Submit Idea' }}
      />
      <OperatorStack.Screen
        name="IdeaDetail"
        component={IdeaDetailScreen}
        options={{ title: 'Idea' }}
      />
    </OperatorStack.Navigator>
  );
}

function ManagerIdeasNavigator() {
  return (
    <ManagerIdeasStack.Navigator>
      <ManagerIdeasStack.Screen
        name="IdeaQueue"
        component={IdeaQueueScreen}
        options={{ title: 'Ideas' }}
      />
      <ManagerIdeasStack.Screen
        name="ManagerIdeaDetail"
        component={ManagerIdeaDetailScreen}
        options={{ title: 'Idea Review' }}
      />
    </ManagerIdeasStack.Navigator>
  );
}

function ManagerProjectsNavigator() {
  return (
    <ManagerProjectsStack.Navigator>
      <ManagerProjectsStack.Screen
        name="ProjectList"
        component={ProjectListScreen}
        options={{ title: 'Projects' }}
      />
      <ManagerProjectsStack.Screen
        name="ProjectDetail"
        component={ProjectDetailScreen}
        options={{ title: 'Project' }}
      />
    </ManagerProjectsStack.Navigator>
  );
}

function ManagerNavigator() {
  return (
    <ManagerTabs.Navigator>
      <ManagerTabs.Screen
        name="Ideas"
        component={ManagerIdeasNavigator}
        options={{ headerShown: false }}
      />
      <ManagerTabs.Screen
        name="Projects"
        component={ManagerProjectsNavigator}
        options={{ headerShown: false }}
      />
    </ManagerTabs.Navigator>
  );
}

function LeadershipNavigator() {
  return (
    <LeadershipStack.Navigator>
      <LeadershipStack.Screen
        name="Dashboard"
        component={DashboardScreen}
        options={{ title: 'Dashboard' }}
      />
    </LeadershipStack.Navigator>
  );
}

export function AppNavigator() {
  const session = useAuthStore((s) => s.session);

  return (
    <NavigationContainer>
      {!session ? (
        <AuthStack.Navigator screenOptions={{ headerShown: false }}>
          <AuthStack.Screen name="Login" component={LoginScreen} />
        </AuthStack.Navigator>
      ) : session.role === 'Operator' ? (
        <OperatorNavigator />
      ) : session.role === 'Manager' ? (
        <ManagerNavigator />
      ) : (
        <LeadershipNavigator />
      )}
    </NavigationContainer>
  );
}
```

- [ ] **Step 8: Replace `App.tsx`**

Overwrite `mobile/App.tsx` with:

```typescript
import React, { useEffect } from 'react';
import { View, ActivityIndicator } from 'react-native';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import * as SecureStore from 'expo-secure-store';
import { useAuthStore } from './src/store/authStore';
import { AppNavigator } from './src/navigation/AppNavigator';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, staleTime: 30_000 },
  },
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
        <ActivityIndicator size="large" color="#2563EB" />
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
```

- [ ] **Step 9: Verify the scaffold starts**

```bash
cd mobile
npx expo start
```

Expected: Metro bundler starts without TypeScript errors. Press `i` (iOS) or `a` (Android) — LoginScreen renders with "Flow" heading and "Sign In" button. No red error overlays.

- [ ] **Step 10: Commit**

```bash
cd ..
git add mobile/
git commit -m "feat: scaffold Expo app with auth foundation and role-based navigation"
```

---

## Task 16: Operator Screens

**Files:**
- Create: `mobile/src/screens/operator/MyIdeasScreen.tsx`
- Create: `mobile/src/screens/operator/SubmitIdeaScreen.tsx`
- Create: `mobile/src/screens/operator/IdeaDetailScreen.tsx`

- [ ] **Step 1: Create `MyIdeasScreen.tsx`**

Create `mobile/src/screens/operator/MyIdeasScreen.tsx`:

```typescript
import React from 'react';
import {
  View, Text, FlatList, TouchableOpacity,
  StyleSheet, ActivityIndicator,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { apiFetch, logout } from '../../api/client';
import { useAuthStore } from '../../store/authStore';
import { IdeaSummary } from '../../types/api';

const STATUS_COLORS: Record<string, string> = {
  Draft: '#6B7280',
  UnderReview: '#D97706',
  Approved: '#059669',
  Rejected: '#DC2626',
};

export function MyIdeasScreen({ navigation }: any) {
  const session = useAuthStore((s) => s.session);

  const { data: ideas, isLoading, error, refetch } = useQuery<IdeaSummary[]>({
    queryKey: ['ideas'],
    queryFn: () => apiFetch<IdeaSummary[]>('/ideas'),
  });

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color="#2563EB" />;
  }
  if (error) {
    return <Text style={styles.errorText}>{(error as Error).message}</Text>;
  }

  return (
    <View style={styles.container}>
      <TouchableOpacity
        style={styles.newButton}
        onPress={() => navigation.navigate('SubmitIdea')}
      >
        <Text style={styles.newButtonText}>+ New Idea</Text>
      </TouchableOpacity>

      <FlatList
        data={ideas ?? []}
        keyExtractor={(item) => item.id}
        onRefresh={refetch}
        refreshing={isLoading}
        renderItem={({ item }) => (
          <TouchableOpacity
            style={styles.card}
            onPress={() => navigation.navigate('IdeaDetail', { id: item.id })}
          >
            <Text style={styles.cardTitle}>{item.title}</Text>
            <View style={styles.metaRow}>
              <Text style={[styles.status, { color: STATUS_COLORS[item.status] ?? '#6B7280' }]}>
                {item.status}
              </Text>
              <Text style={styles.priority}>{item.priority}</Text>
            </View>
            <Text style={styles.problem} numberOfLines={2}>{item.problem}</Text>
          </TouchableOpacity>
        )}
        ListEmptyComponent={
          <Text style={styles.emptyText}>No ideas yet. Tap "+ New Idea" to submit your first.</Text>
        }
        contentContainerStyle={{ paddingBottom: 24 }}
      />

      <TouchableOpacity
        style={styles.logoutBtn}
        onPress={() => logout(session?.refreshToken ?? '')}
      >
        <Text style={styles.logoutText}>Sign Out</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB', padding: 16 },
  newButton: {
    backgroundColor: '#2563EB', borderRadius: 8,
    padding: 12, alignItems: 'center', marginBottom: 16,
  },
  newButtonText: { color: '#FFF', fontWeight: '600', fontSize: 15 },
  card: {
    backgroundColor: '#FFF', borderRadius: 8, padding: 14,
    marginBottom: 12, borderWidth: 1, borderColor: '#E5E7EB',
  },
  cardTitle: { fontSize: 16, fontWeight: '600', color: '#111827', marginBottom: 6 },
  metaRow: { flexDirection: 'row', gap: 8, marginBottom: 4 },
  status: { fontSize: 13, fontWeight: '500' },
  priority: { color: '#6B7280', fontSize: 12, alignSelf: 'center' },
  problem: { color: '#6B7280', fontSize: 13, marginTop: 4 },
  emptyText: { textAlign: 'center', color: '#9CA3AF', marginTop: 48, fontSize: 15, lineHeight: 22 },
  errorText: { textAlign: 'center', color: '#EF4444', marginTop: 48, padding: 16 },
  logoutBtn: { alignItems: 'center', padding: 14, marginTop: 8 },
  logoutText: { color: '#6B7280', fontSize: 14 },
});
```

- [ ] **Step 2: Create `SubmitIdeaScreen.tsx`**

Create `mobile/src/screens/operator/SubmitIdeaScreen.tsx`:

```typescript
import React, { useState } from 'react';
import {
  View, Text, TextInput, TouchableOpacity,
  StyleSheet, Alert, ActivityIndicator, ScrollView,
} from 'react-native';
import { useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../../api/client';
import { IdeaSummary } from '../../types/api';

export function SubmitIdeaScreen({ navigation }: any) {
  const [title, setTitle] = useState('');
  const [problem, setProblem] = useState('');
  const [description, setDescription] = useState('');
  const [loading, setLoading] = useState(false);
  const queryClient = useQueryClient();

  async function handleCreate() {
    if (!title.trim() || !problem.trim() || !description.trim()) {
      Alert.alert('Validation', 'Title, problem, and description are all required.');
      return;
    }
    setLoading(true);
    try {
      await apiFetch<IdeaSummary>('/ideas', {
        method: 'POST',
        body: JSON.stringify({ title, problem, description, linkedGuidelineId: null }),
      });
      await queryClient.invalidateQueries({ queryKey: ['ideas'] });
      Alert.alert('Success', 'Idea created.', [
        { text: 'OK', onPress: () => navigation.goBack() },
      ]);
    } catch (err: any) {
      Alert.alert('Error', err.message ?? 'Could not create idea');
    } finally {
      setLoading(false);
    }
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={{ paddingBottom: 32 }}>
      <Text style={styles.label}>Title *</Text>
      <TextInput
        style={styles.input}
        value={title}
        onChangeText={setTitle}
        placeholder="Brief, descriptive title"
      />

      <Text style={styles.label}>Problem *</Text>
      <TextInput
        style={[styles.input, styles.multiline]}
        value={problem}
        onChangeText={setProblem}
        placeholder="What problem does this idea solve?"
        multiline
        numberOfLines={3}
      />

      <Text style={styles.label}>Description *</Text>
      <TextInput
        style={[styles.input, styles.multiline]}
        value={description}
        onChangeText={setDescription}
        placeholder="Describe your idea in detail"
        multiline
        numberOfLines={5}
      />

      {loading ? (
        <ActivityIndicator size="large" color="#2563EB" style={{ marginTop: 20 }} />
      ) : (
        <TouchableOpacity style={styles.button} onPress={handleCreate}>
          <Text style={styles.buttonText}>Create Idea</Text>
        </TouchableOpacity>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB', padding: 16 },
  label: { fontSize: 14, fontWeight: '600', color: '#374151', marginBottom: 4, marginTop: 14 },
  input: {
    borderWidth: 1, borderColor: '#D1D5DB', borderRadius: 8,
    padding: 12, backgroundColor: '#FFF', fontSize: 15,
  },
  multiline: { minHeight: 80, textAlignVertical: 'top' },
  button: {
    backgroundColor: '#2563EB', borderRadius: 8,
    padding: 14, alignItems: 'center', marginTop: 24,
  },
  buttonText: { color: '#FFF', fontWeight: '600', fontSize: 16 },
});
```

- [ ] **Step 3: Create `IdeaDetailScreen.tsx`**

Create `mobile/src/screens/operator/IdeaDetailScreen.tsx`:

```typescript
import React from 'react';
import {
  View, Text, TouchableOpacity, StyleSheet,
  ScrollView, ActivityIndicator, Alert,
} from 'react-native';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../../api/client';
import { IdeaDetail } from '../../types/api';

export function IdeaDetailScreen({ route }: any) {
  const { id } = route.params as { id: string };
  const queryClient = useQueryClient();

  const { data: idea, isLoading, error } = useQuery<IdeaDetail>({
    queryKey: ['idea', id],
    queryFn: () => apiFetch<IdeaDetail>(`/ideas/${id}`),
  });

  async function handleSubmitForReview() {
    try {
      await apiFetch(`/ideas/${id}/submit`, { method: 'POST' });
      await queryClient.invalidateQueries({ queryKey: ['ideas'] });
      await queryClient.invalidateQueries({ queryKey: ['idea', id] });
      Alert.alert('Submitted', 'Your idea has been submitted for manager review.');
    } catch (err: any) {
      Alert.alert('Error', err.message ?? 'Could not submit idea');
    }
  }

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color="#2563EB" />;
  }
  if (error || !idea) {
    return (
      <Text style={styles.errorText}>{(error as Error)?.message ?? 'Idea not found'}</Text>
    );
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={{ paddingBottom: 32 }}>
      <Text style={styles.title}>{idea.title}</Text>
      <View style={styles.metaRow}>
        <Text style={styles.badge}>{idea.status}</Text>
        <Text style={styles.priority}>{idea.priority}</Text>
      </View>

      <Text style={styles.sectionLabel}>Problem</Text>
      <Text style={styles.body}>{idea.problem}</Text>

      <Text style={styles.sectionLabel}>Description</Text>
      <Text style={styles.body}>{idea.description}</Text>

      {idea.managerComment ? (
        <>
          <Text style={styles.sectionLabel}>Manager Comment</Text>
          <View style={styles.commentBox}>
            <Text style={styles.commentText}>{idea.managerComment}</Text>
          </View>
        </>
      ) : null}

      <Text style={styles.timestamp}>
        Submitted: {new Date(idea.createdAt).toLocaleDateString()}
      </Text>

      {idea.status === 'Draft' && (
        <TouchableOpacity style={styles.submitBtn} onPress={handleSubmitForReview}>
          <Text style={styles.submitBtnText}>Submit for Review</Text>
        </TouchableOpacity>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB', padding: 16 },
  title: { fontSize: 20, fontWeight: 'bold', color: '#111827', marginBottom: 8 },
  metaRow: { flexDirection: 'row', gap: 8, marginBottom: 16 },
  badge: {
    backgroundColor: '#EEF2FF', color: '#4338CA',
    paddingHorizontal: 10, paddingVertical: 3,
    borderRadius: 12, fontSize: 13, fontWeight: '500',
  },
  priority: { color: '#6B7280', fontSize: 13, alignSelf: 'center' },
  sectionLabel: {
    fontSize: 13, fontWeight: '600', color: '#374151',
    marginTop: 16, marginBottom: 4,
  },
  body: { fontSize: 15, color: '#374151', lineHeight: 22 },
  commentBox: {
    backgroundColor: '#FEF3C7', borderRadius: 6,
    padding: 10, marginTop: 4,
  },
  commentText: { fontSize: 14, color: '#92400E', lineHeight: 20 },
  timestamp: { fontSize: 12, color: '#9CA3AF', marginTop: 16 },
  submitBtn: {
    backgroundColor: '#059669', borderRadius: 8,
    padding: 14, alignItems: 'center', marginTop: 24,
  },
  submitBtnText: { color: '#FFF', fontWeight: '600', fontSize: 16 },
  errorText: { textAlign: 'center', color: '#EF4444', marginTop: 48, padding: 16 },
});
```

- [ ] **Step 4: Verify operator flow**

Log in as an Operator user. Verify:

1. My Ideas screen loads with an empty list or existing ideas
2. Pull-to-refresh re-fetches the list
3. Tap "+ New Idea" → SubmitIdeaScreen renders with three fields
4. Leave a field blank and tap "Create Idea" → validation alert fires
5. Fill all fields → "Create Idea" succeeds → returns to MyIdeasScreen → new idea appears with "Draft" status
6. Tap the idea → IdeaDetailScreen shows title, problem, description, and "Submit for Review" button
7. Tap "Submit for Review" → success alert → status updates to "UnderReview" → button disappears
8. "Sign Out" clears the session and returns to LoginScreen

- [ ] **Step 5: Commit**

```bash
git add mobile/src/screens/operator/
git commit -m "feat: add operator idea screens (list, create, detail, submit for review)"
```

---

## Task 17: Manager Screens

**Files:**
- Create: `mobile/src/screens/manager/IdeaQueueScreen.tsx`
- Create: `mobile/src/screens/manager/ManagerIdeaDetailScreen.tsx`
- Create: `mobile/src/screens/manager/ProjectListScreen.tsx`
- Create: `mobile/src/screens/manager/ProjectDetailScreen.tsx`

- [ ] **Step 1: Create `IdeaQueueScreen.tsx`**

Create `mobile/src/screens/manager/IdeaQueueScreen.tsx`:

```typescript
import React from 'react';
import {
  View, Text, FlatList, TouchableOpacity,
  StyleSheet, ActivityIndicator,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { apiFetch } from '../../api/client';
import { IdeaSummary } from '../../types/api';

const STATUS_COLORS: Record<string, string> = {
  Draft: '#6B7280',
  UnderReview: '#D97706',
  Approved: '#059669',
  Rejected: '#DC2626',
};

export function IdeaQueueScreen({ navigation }: any) {
  const { data: ideas, isLoading, error, refetch } = useQuery<IdeaSummary[]>({
    queryKey: ['ideas', 'all'],
    queryFn: () => apiFetch<IdeaSummary[]>('/ideas'),
  });

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color="#2563EB" />;
  }
  if (error) {
    return <Text style={styles.errorText}>{(error as Error).message}</Text>;
  }

  const pending = (ideas ?? []).filter((i) => i.status === 'UnderReview');
  const rest = (ideas ?? []).filter((i) => i.status !== 'UnderReview');
  const sorted = [...pending, ...rest];

  return (
    <View style={styles.container}>
      <FlatList
        data={sorted}
        keyExtractor={(item) => item.id}
        onRefresh={refetch}
        refreshing={isLoading}
        renderItem={({ item }) => (
          <TouchableOpacity
            style={[
              styles.card,
              item.status === 'UnderReview' && styles.cardHighlight,
            ]}
            onPress={() => navigation.navigate('ManagerIdeaDetail', { id: item.id })}
          >
            <Text style={styles.cardTitle}>{item.title}</Text>
            <View style={styles.metaRow}>
              <Text style={[styles.status, { color: STATUS_COLORS[item.status] ?? '#6B7280' }]}>
                {item.status}
              </Text>
              <Text style={styles.priority}>{item.priority}</Text>
            </View>
            <Text style={styles.problem} numberOfLines={2}>{item.problem}</Text>
          </TouchableOpacity>
        )}
        ListEmptyComponent={
          <Text style={styles.emptyText}>No ideas submitted yet.</Text>
        }
        contentContainerStyle={{ paddingBottom: 24 }}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB', padding: 16 },
  card: {
    backgroundColor: '#FFF', borderRadius: 8, padding: 14,
    marginBottom: 12, borderWidth: 1, borderColor: '#E5E7EB',
  },
  cardHighlight: { borderColor: '#FCD34D', backgroundColor: '#FFFBEB' },
  cardTitle: { fontSize: 16, fontWeight: '600', color: '#111827', marginBottom: 6 },
  metaRow: { flexDirection: 'row', gap: 8, marginBottom: 4 },
  status: { fontSize: 13, fontWeight: '500' },
  priority: { color: '#6B7280', fontSize: 12, alignSelf: 'center' },
  problem: { color: '#6B7280', fontSize: 13, marginTop: 4 },
  emptyText: { textAlign: 'center', color: '#9CA3AF', marginTop: 48, fontSize: 15 },
  errorText: { textAlign: 'center', color: '#EF4444', marginTop: 48, padding: 16 },
});
```

- [ ] **Step 2: Create `ManagerIdeaDetailScreen.tsx`**

Create `mobile/src/screens/manager/ManagerIdeaDetailScreen.tsx`:

```typescript
import React, { useState } from 'react';
import {
  View, Text, TouchableOpacity, StyleSheet, ScrollView,
  ActivityIndicator, Alert, TextInput,
} from 'react-native';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../../api/client';
import { IdeaDetail } from '../../types/api';

export function ManagerIdeaDetailScreen({ route }: any) {
  const { id } = route.params as { id: string };
  const queryClient = useQueryClient();
  const [comment, setComment] = useState('');
  const [actionLoading, setActionLoading] = useState(false);

  const { data: idea, isLoading, error } = useQuery<IdeaDetail>({
    queryKey: ['idea', id],
    queryFn: () => apiFetch<IdeaDetail>(`/ideas/${id}`),
  });

  async function invalidate() {
    await queryClient.invalidateQueries({ queryKey: ['idea', id] });
    await queryClient.invalidateQueries({ queryKey: ['ideas', 'all'] });
  }

  async function handleApprove() {
    setActionLoading(true);
    try {
      await apiFetch(`/ideas/${id}/approve`, {
        method: 'POST',
        body: JSON.stringify({ managerComment: comment.trim() || null }),
      });
      await invalidate();
      Alert.alert('Approved', 'The idea has been approved.');
    } catch (err: any) {
      Alert.alert('Error', err.message ?? 'Could not approve idea');
    } finally {
      setActionLoading(false);
    }
  }

  async function handleReject() {
    if (!comment.trim()) {
      Alert.alert('Validation', 'A rejection comment is required.');
      return;
    }
    setActionLoading(true);
    try {
      await apiFetch(`/ideas/${id}/reject`, {
        method: 'POST',
        body: JSON.stringify({ managerComment: comment }),
      });
      await invalidate();
      Alert.alert('Rejected', 'The idea has been rejected.');
    } catch (err: any) {
      Alert.alert('Error', err.message ?? 'Could not reject idea');
    } finally {
      setActionLoading(false);
    }
  }

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color="#2563EB" />;
  }
  if (error || !idea) {
    return <Text style={styles.errorText}>{(error as Error)?.message ?? 'Not found'}</Text>;
  }

  const canAct = idea.status === 'UnderReview';

  return (
    <ScrollView style={styles.container} contentContainerStyle={{ paddingBottom: 32 }}>
      <Text style={styles.title}>{idea.title}</Text>
      <View style={styles.metaRow}>
        <Text style={styles.badge}>{idea.status}</Text>
        <Text style={styles.priority}>{idea.priority}</Text>
      </View>

      <Text style={styles.sectionLabel}>Problem</Text>
      <Text style={styles.body}>{idea.problem}</Text>

      <Text style={styles.sectionLabel}>Description</Text>
      <Text style={styles.body}>{idea.description}</Text>

      {idea.managerComment ? (
        <>
          <Text style={styles.sectionLabel}>Manager Comment</Text>
          <Text style={styles.body}>{idea.managerComment}</Text>
        </>
      ) : null}

      {canAct && (
        <>
          <Text style={styles.sectionLabel}>Comment (required for rejection)</Text>
          <TextInput
            style={styles.input}
            value={comment}
            onChangeText={setComment}
            placeholder="Add a comment..."
            multiline
            numberOfLines={3}
          />
          {actionLoading ? (
            <ActivityIndicator size="large" color="#2563EB" style={{ marginTop: 16 }} />
          ) : (
            <View style={styles.actionsRow}>
              <TouchableOpacity
                style={[styles.actionBtn, styles.approveBtn]}
                onPress={handleApprove}
              >
                <Text style={styles.actionBtnText}>Approve</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.actionBtn, styles.rejectBtn]}
                onPress={handleReject}
              >
                <Text style={styles.actionBtnText}>Reject</Text>
              </TouchableOpacity>
            </View>
          )}
        </>
      )}

      {!canAct && (
        <Text style={styles.resolvedNote}>
          This idea has already been {idea.status.toLowerCase()}.
        </Text>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB', padding: 16 },
  title: { fontSize: 20, fontWeight: 'bold', color: '#111827', marginBottom: 8 },
  metaRow: { flexDirection: 'row', gap: 8, marginBottom: 16 },
  badge: {
    backgroundColor: '#FEF3C7', color: '#92400E',
    paddingHorizontal: 10, paddingVertical: 3,
    borderRadius: 12, fontSize: 13, fontWeight: '500',
  },
  priority: { color: '#6B7280', fontSize: 13, alignSelf: 'center' },
  sectionLabel: {
    fontSize: 13, fontWeight: '600', color: '#374151',
    marginTop: 16, marginBottom: 4,
  },
  body: { fontSize: 15, color: '#374151', lineHeight: 22 },
  input: {
    borderWidth: 1, borderColor: '#D1D5DB', borderRadius: 8,
    padding: 12, backgroundColor: '#FFF', fontSize: 15,
    minHeight: 72, textAlignVertical: 'top', marginTop: 4,
  },
  actionsRow: { flexDirection: 'row', gap: 12, marginTop: 16 },
  actionBtn: { flex: 1, padding: 14, borderRadius: 8, alignItems: 'center' },
  approveBtn: { backgroundColor: '#059669' },
  rejectBtn: { backgroundColor: '#DC2626' },
  actionBtnText: { color: '#FFF', fontWeight: '600', fontSize: 15 },
  resolvedNote: { marginTop: 24, textAlign: 'center', color: '#6B7280', fontSize: 14 },
  errorText: { textAlign: 'center', color: '#EF4444', marginTop: 48, padding: 16 },
});
```

- [ ] **Step 3: Create `ProjectListScreen.tsx`**

Create `mobile/src/screens/manager/ProjectListScreen.tsx`:

```typescript
import React from 'react';
import {
  View, Text, FlatList, TouchableOpacity,
  StyleSheet, ActivityIndicator,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { apiFetch, logout } from '../../api/client';
import { useAuthStore } from '../../store/authStore';
import { ProjectSummary } from '../../types/api';

const STATUS_COLORS: Record<string, string> = {
  Planning: '#6B7280',
  InProgress: '#2563EB',
  Blocked: '#DC2626',
  Completed: '#059669',
  Cancelled: '#9CA3AF',
};

export function ProjectListScreen({ navigation }: any) {
  const session = useAuthStore((s) => s.session);

  const { data: projects, isLoading, error, refetch } = useQuery<ProjectSummary[]>({
    queryKey: ['projects'],
    queryFn: () => apiFetch<ProjectSummary[]>('/projects'),
  });

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color="#2563EB" />;
  }
  if (error) {
    return <Text style={styles.errorText}>{(error as Error).message}</Text>;
  }

  return (
    <View style={styles.container}>
      <FlatList
        data={projects ?? []}
        keyExtractor={(item) => item.id}
        onRefresh={refetch}
        refreshing={isLoading}
        renderItem={({ item }) => (
          <TouchableOpacity
            style={[
              styles.card,
              item.status === 'Blocked' && styles.cardBlocked,
            ]}
            onPress={() => navigation.navigate('ProjectDetail', { id: item.id })}
          >
            <Text style={styles.cardTitle}>{item.title}</Text>
            <View style={styles.metaRow}>
              <Text style={[styles.status, { color: STATUS_COLORS[item.status] ?? '#6B7280' }]}>
                {item.status}
              </Text>
              <Text style={styles.priority}>{item.priority}</Text>
            </View>
            {item.blockedReason && (
              <Text style={styles.blockedReason} numberOfLines={1}>
                ⚠ {item.blockedReason}
              </Text>
            )}
          </TouchableOpacity>
        )}
        ListEmptyComponent={
          <Text style={styles.emptyText}>No projects yet.</Text>
        }
        contentContainerStyle={{ paddingBottom: 24 }}
      />

      <TouchableOpacity
        style={styles.logoutBtn}
        onPress={() => logout(session?.refreshToken ?? '')}
      >
        <Text style={styles.logoutText}>Sign Out</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB', padding: 16 },
  card: {
    backgroundColor: '#FFF', borderRadius: 8, padding: 14,
    marginBottom: 12, borderWidth: 1, borderColor: '#E5E7EB',
  },
  cardBlocked: { borderColor: '#FECACA', backgroundColor: '#FEF2F2' },
  cardTitle: { fontSize: 16, fontWeight: '600', color: '#111827', marginBottom: 6 },
  metaRow: { flexDirection: 'row', gap: 8 },
  status: { fontSize: 13, fontWeight: '600' },
  priority: { color: '#6B7280', fontSize: 12, alignSelf: 'center' },
  blockedReason: { color: '#DC2626', fontSize: 12, marginTop: 6 },
  emptyText: { textAlign: 'center', color: '#9CA3AF', marginTop: 48, fontSize: 15 },
  errorText: { textAlign: 'center', color: '#EF4444', marginTop: 48, padding: 16 },
  logoutBtn: { alignItems: 'center', padding: 14, marginTop: 8 },
  logoutText: { color: '#6B7280', fontSize: 14 },
});
```

- [ ] **Step 4: Create `ProjectDetailScreen.tsx`**

Create `mobile/src/screens/manager/ProjectDetailScreen.tsx`:

```typescript
import React, { useState } from 'react';
import {
  View, Text, TouchableOpacity, StyleSheet, ScrollView,
  ActivityIndicator, Alert, TextInput,
} from 'react-native';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../../api/client';
import { ProjectDetail } from '../../types/api';

export function ProjectDetailScreen({ route }: any) {
  const { id } = route.params as { id: string };
  const queryClient = useQueryClient();
  const [blockReason, setBlockReason] = useState('');
  const [actionLoading, setActionLoading] = useState(false);

  const { data: project, isLoading, error } = useQuery<ProjectDetail>({
    queryKey: ['project', id],
    queryFn: () => apiFetch<ProjectDetail>(`/projects/${id}`),
  });

  async function callAction(path: string, body?: object) {
    setActionLoading(true);
    try {
      await apiFetch(path, {
        method: 'POST',
        body: body ? JSON.stringify(body) : undefined,
      });
      await queryClient.invalidateQueries({ queryKey: ['project', id] });
      await queryClient.invalidateQueries({ queryKey: ['projects'] });
    } catch (err: any) {
      Alert.alert('Error', err.message ?? 'Action failed');
    } finally {
      setActionLoading(false);
    }
  }

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color="#2563EB" />;
  }
  if (error || !project) {
    return <Text style={styles.errorText}>{(error as Error)?.message ?? 'Not found'}</Text>;
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={{ paddingBottom: 32 }}>
      <Text style={styles.title}>{project.title}</Text>
      <View style={styles.metaRow}>
        <Text style={styles.badge}>{project.status}</Text>
        <Text style={styles.priority}>{project.priority}</Text>
      </View>

      <Text style={styles.sectionLabel}>Description</Text>
      <Text style={styles.body}>{project.description}</Text>

      {project.blockedReason ? (
        <>
          <Text style={styles.sectionLabel}>Blocked Reason</Text>
          <View style={styles.blockedBox}>
            <Text style={styles.blockedText}>{project.blockedReason}</Text>
          </View>
        </>
      ) : null}

      {project.estimatedCost != null && (
        <Text style={styles.meta}>
          Estimated Cost: ${project.estimatedCost.toLocaleString()}
        </Text>
      )}
      {project.deadline && (
        <Text style={styles.meta}>
          Deadline: {new Date(project.deadline).toLocaleDateString()}
        </Text>
      )}
      {project.startDate && (
        <Text style={styles.meta}>
          Started: {new Date(project.startDate).toLocaleDateString()}
        </Text>
      )}
      {project.completedAt && (
        <Text style={styles.meta}>
          Completed: {new Date(project.completedAt).toLocaleDateString()}
        </Text>
      )}

      {/* ── State-transition actions ─────────────────────────────────────── */}

      {actionLoading && (
        <ActivityIndicator size="large" color="#2563EB" style={{ marginTop: 20 }} />
      )}

      {!actionLoading && project.status === 'Planning' && (
        <TouchableOpacity
          style={[styles.actionBtn, { backgroundColor: '#2563EB' }]}
          onPress={() => callAction(`/projects/${id}/start`)}
        >
          <Text style={styles.actionBtnText}>Start Project</Text>
        </TouchableOpacity>
      )}

      {!actionLoading && project.status === 'InProgress' && (
        <View>
          <TouchableOpacity
            style={[styles.actionBtn, { backgroundColor: '#059669' }]}
            onPress={() => callAction(`/projects/${id}/complete`)}
          >
            <Text style={styles.actionBtnText}>Mark Complete</Text>
          </TouchableOpacity>

          <Text style={styles.sectionLabel}>Block Reason *</Text>
          <TextInput
            style={styles.input}
            value={blockReason}
            onChangeText={setBlockReason}
            placeholder="Why is this project blocked?"
            multiline
            numberOfLines={2}
          />
          <TouchableOpacity
            style={[styles.actionBtn, { backgroundColor: '#DC2626' }]}
            onPress={() => {
              if (!blockReason.trim()) {
                Alert.alert('Validation', 'A block reason is required.');
                return;
              }
              callAction(`/projects/${id}/block`, { reason: blockReason });
            }}
          >
            <Text style={styles.actionBtnText}>Block Project</Text>
          </TouchableOpacity>
        </View>
      )}

      {!actionLoading && project.status === 'Blocked' && (
        <TouchableOpacity
          style={[styles.actionBtn, { backgroundColor: '#D97706' }]}
          onPress={() => callAction(`/projects/${id}/unblock`)}
        >
          <Text style={styles.actionBtnText}>Unblock Project</Text>
        </TouchableOpacity>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB', padding: 16 },
  title: { fontSize: 20, fontWeight: 'bold', color: '#111827', marginBottom: 8 },
  metaRow: { flexDirection: 'row', gap: 8, marginBottom: 16 },
  badge: {
    backgroundColor: '#EEF2FF', color: '#4338CA',
    paddingHorizontal: 10, paddingVertical: 3,
    borderRadius: 12, fontSize: 13, fontWeight: '500',
  },
  priority: { color: '#6B7280', fontSize: 13, alignSelf: 'center' },
  sectionLabel: {
    fontSize: 13, fontWeight: '600', color: '#374151',
    marginTop: 16, marginBottom: 4,
  },
  body: { fontSize: 15, color: '#374151', lineHeight: 22 },
  blockedBox: {
    backgroundColor: '#FEF2F2', borderRadius: 6,
    padding: 10, marginTop: 4,
  },
  blockedText: { color: '#DC2626', fontSize: 14 },
  meta: { color: '#6B7280', fontSize: 13, marginTop: 6 },
  input: {
    borderWidth: 1, borderColor: '#D1D5DB', borderRadius: 8,
    padding: 12, backgroundColor: '#FFF', fontSize: 15,
    minHeight: 60, textAlignVertical: 'top', marginTop: 4,
  },
  actionBtn: {
    borderRadius: 8, padding: 14,
    alignItems: 'center', marginTop: 14,
  },
  actionBtnText: { color: '#FFF', fontWeight: '600', fontSize: 15 },
  errorText: { textAlign: 'center', color: '#EF4444', marginTop: 48, padding: 16 },
});
```

- [ ] **Step 5: Verify manager flow**

Log in as a Manager. Verify:

1. **Ideas tab**: All submitted ideas are shown; ideas with status "UnderReview" appear highlighted at the top
2. Tap an "UnderReview" idea → Approve and Reject buttons appear
3. Tap "Approve" (with or without a comment) → status updates to "Approved" in the queue
4. Tap another "UnderReview" idea → leave comment field blank → tap "Reject" → validation alert fires
5. Add a comment → tap "Reject" → status updates to "Rejected"
6. **Projects tab**: All projects listed; blocked projects appear with red border and reason
7. Tap a "Planning" project → "Start Project" button appears → tap it → status updates to "InProgress"
8. Tap the now "InProgress" project → "Mark Complete" and "Block Project" inputs appear
9. Fill in a block reason → tap "Block Project" → status updates to "Blocked"
10. Tap the "Blocked" project → "Unblock Project" button → status returns to "InProgress"
11. Tap again → "Mark Complete" → status updates to "Completed"; no action buttons shown
12. "Sign Out" on the Projects tab clears session and returns to LoginScreen

- [ ] **Step 6: Commit**

```bash
git add mobile/src/screens/manager/
git commit -m "feat: add manager screens (idea review, approve/reject, project state management)"
```

---

## Task 18: Leadership Dashboard + End-to-End Verification

**Files:**
- Create: `mobile/src/screens/leadership/DashboardScreen.tsx`

- [ ] **Step 1: Create `DashboardScreen.tsx`**

Create `mobile/src/screens/leadership/DashboardScreen.tsx`:

```typescript
import React from 'react';
import {
  View, Text, StyleSheet, ScrollView,
  ActivityIndicator, TouchableOpacity,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { apiFetch, logout } from '../../api/client';
import { useAuthStore } from '../../store/authStore';
import { DashboardSummary, BlockedProject } from '../../types/api';

export function DashboardScreen() {
  const session = useAuthStore((s) => s.session);

  const { data, isLoading, error, refetch } = useQuery<DashboardSummary>({
    queryKey: ['dashboard'],
    queryFn: () => apiFetch<DashboardSummary>('/dashboard/summary'),
    refetchInterval: 60_000, // auto-refresh every minute
  });

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color="#2563EB" />;
  }
  if (error || !data) {
    return (
      <Text style={styles.errorText}>
        {(error as Error)?.message ?? 'Failed to load dashboard'}
      </Text>
    );
  }

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={{ paddingBottom: 32 }}
      onScrollBeginDrag={refetch as any}
    >
      <Text style={styles.heading}>Innovation Overview</Text>

      {/* ── Ideas KPIs ─────────────────────────────────────────────────── */}
      <Text style={styles.sectionTitle}>Ideas</Text>
      <View style={styles.row}>
        <MetricCard label="Total" value={data.totalIdeas} />
        <MetricCard label="Approved" value={data.approvedIdeas} color="#059669" />
        <MetricCard label="Rejected" value={data.rejectedIdeas} color="#DC2626" />
      </View>
      <View style={styles.row}>
        <MetricCard label="Pending Review" value={data.pendingIdeas} color="#D97706" />
        <MetricCard
          label="Conversion"
          value={`${data.conversionRate.toFixed(1)}%`}
          color="#2563EB"
        />
      </View>

      {/* ── Project KPIs ────────────────────────────────────────────────── */}
      <Text style={styles.sectionTitle}>Projects</Text>
      <View style={styles.row}>
        <MetricCard label="Active" value={data.activeProjects} color="#2563EB" />
        <MetricCard label="Blocked" value={data.blockedProjects} color="#DC2626" />
        <MetricCard label="Completed" value={data.completedProjects} color="#059669" />
      </View>
      <View style={styles.row}>
        <MetricCard
          label="Avg Completion"
          value={`${data.averageCompletionDays}d`}
        />
        <MetricCard
          label="Bottleneck"
          value={`${data.bottleneckIndex.toFixed(1)}%`}
          color={data.bottleneckIndex > 30 ? '#DC2626' : '#059669'}
        />
        <MetricCard
          label="Total ROI"
          value={`${data.totalRoi.toFixed(0)}%`}
          color="#2563EB"
        />
      </View>

      {/* ── Blocked project list ─────────────────────────────────────────── */}
      {data.blockedProjectList.length > 0 && (
        <>
          <Text style={styles.sectionTitle}>Blocked Projects</Text>
          {data.blockedProjectList.map((p: BlockedProject) => (
            <View key={p.id} style={styles.blockedCard}>
              <View style={styles.blockedHeader}>
                <Text style={styles.blockedTitle}>{p.title}</Text>
                <Text style={styles.blockedDays}>
                  {p.daysBlocked}d blocked
                </Text>
              </View>
              <Text style={styles.blockedReason}>{p.blockedReason}</Text>
            </View>
          ))}
        </>
      )}

      <TouchableOpacity
        style={styles.logoutBtn}
        onPress={() => logout(session?.refreshToken ?? '')}
      >
        <Text style={styles.logoutText}>Sign Out</Text>
      </TouchableOpacity>
    </ScrollView>
  );
}

function MetricCard({
  label,
  value,
  color = '#111827',
}: {
  label: string;
  value: string | number;
  color?: string;
}) {
  return (
    <View style={cardStyles.card}>
      <Text style={[cardStyles.value, { color }]}>{value}</Text>
      <Text style={cardStyles.label}>{label}</Text>
    </View>
  );
}

const cardStyles = StyleSheet.create({
  card: {
    flex: 1, backgroundColor: '#FFF', borderRadius: 8,
    padding: 14, alignItems: 'center',
    borderWidth: 1, borderColor: '#E5E7EB',
  },
  value: { fontSize: 22, fontWeight: 'bold', marginBottom: 4 },
  label: { fontSize: 11, color: '#6B7280', textAlign: 'center' },
});

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB', padding: 16 },
  heading: { fontSize: 22, fontWeight: 'bold', color: '#1E3A5F', marginBottom: 4 },
  sectionTitle: {
    fontSize: 14, fontWeight: '700', color: '#374151',
    marginTop: 20, marginBottom: 10,
  },
  row: { flexDirection: 'row', gap: 8, marginBottom: 8 },
  blockedCard: {
    backgroundColor: '#FEF2F2', borderRadius: 8, padding: 12,
    marginBottom: 8, borderWidth: 1, borderColor: '#FECACA',
  },
  blockedHeader: {
    flexDirection: 'row', justifyContent: 'space-between', marginBottom: 4,
  },
  blockedTitle: { fontSize: 15, fontWeight: '600', color: '#111827', flex: 1 },
  blockedDays: { fontSize: 12, color: '#DC2626', fontWeight: '500' },
  blockedReason: { fontSize: 13, color: '#7F1D1D', lineHeight: 18 },
  logoutBtn: {
    marginTop: 32, borderRadius: 8, borderWidth: 1,
    borderColor: '#D1D5DB', padding: 14, alignItems: 'center',
  },
  logoutText: { color: '#374151', fontWeight: '600' },
  errorText: { textAlign: 'center', color: '#EF4444', marginTop: 48, padding: 16 },
});
```

- [ ] **Step 2: Verify leadership flow**

Log in as a Leadership user. Verify:

1. Dashboard loads with all metric cards populated
2. Ideas row: Total, Approved, Rejected, Pending Review, Conversion % all show numeric values
3. Projects row: Active, Blocked, Completed, Avg Completion (days), Bottleneck %, Total ROI all populated
4. If any blocked projects exist, they appear as red cards with title, days blocked, and reason
5. Metrics accurately reflect the data created in Operator and Manager flows above
6. "Sign Out" clears session and returns to LoginScreen

- [ ] **Step 3: End-to-end system verification**

With all three roles tested, verify the complete flow:

**A. Full idea lifecycle (Operator → Manager):**
1. Log in as Operator → create idea → submit for review
2. Log in as Manager → Ideas tab shows the idea highlighted → approve it
3. Log back in as Operator → idea shows "Approved" status

**B. Full project lifecycle (Manager):**
1. Log in as Manager → Projects tab → tap a "Planning" project → Start
2. Tap the InProgress project → Block with reason → confirm Blocked
3. Tap the Blocked project → Unblock → confirm InProgress
4. Tap again → Complete → confirm Completed

**C. Dashboard reflects both (Leadership):**
1. Log in as Leadership → verify Completed project count increased
2. Verify bottleneck % is 0 after unblocking all blocked projects
3. Verify conversion rate reflects approved idea

- [ ] **Step 4: Final commit**

```bash
git add mobile/src/screens/leadership/ mobile/src/screens/operator/ mobile/src/screens/manager/
git commit -m "feat: add leadership dashboard and complete end-to-end mobile verification"
```

---

## Notes for the Implementer

**API base URL:** `API_BASE` in `mobile/src/api/client.ts` defaults to `http://localhost:5153/api/v1`. Change to:
- `http://10.0.2.2:5153/api/v1` for Android emulator
- Your machine's LAN IP for a real device (find with `ipconfig` on Windows → IPv4 Address)

**Starting the API:** The backend must be running locally before testing:
```bash
cd src/Flow.API
dotnet run
```

**Starting the mobile app:**
```bash
cd mobile
npx expo start
```

**Placeholder screens during development:** The scaffold generates placeholder screens that TypeScript won't complain about. The AppNavigator imports all screens at once — create all screen files (even empty exports) in Task 15 if TypeScript complains during verification.
