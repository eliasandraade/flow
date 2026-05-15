# Flow Mobile — UI/UX Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply a centralized design system and 5 shared components across all 9 mobile screens for visual consistency and professional quality.

**Architecture:** Foundation-first in two phases. Phase 1 creates `theme.ts`, `normalizeStatus.ts`, and 5 shared components. Phase 2 rewrites each screen to consume them. No logic, no libraries, no architecture changes — visual polish only.

**Tech Stack:** React Native (Expo), TypeScript, `react-native-safe-area-context` (already installed), `@tanstack/react-query`, Zustand.

**Spec:** `docs/superpowers/specs/2026-05-14-flow-mobile-ui-polish-design.md`

---

## File Map

### Phase 1 — New files

| File | Responsibility |
|---|---|
| `mobile/src/theme.ts` | Colors, typography, spacing, radius constants |
| `mobile/src/utils/normalizeStatus.ts` | Backend PascalCase → UI camelCase status mapping |
| `mobile/src/components/ScreenContainer.tsx` | SafeAreaView wrapper with consistent padding |
| `mobile/src/components/Button.tsx` | 4 variants, 3 sizes, loading + double-tap guard |
| `mobile/src/components/StatusBadge.tsx` | Status pill with theme color mapping |
| `mobile/src/components/Card.tsx` | Card with shadow, optional onPress, 64px min touch |
| `mobile/src/components/FormInput.tsx` | Label + TextInput + error state |

### Phase 2 — Modified files

| File | Key changes |
|---|---|
| `mobile/src/screens/auth/LoginScreen.tsx` | ScreenContainer, FormInput ×2, Button |
| `mobile/src/screens/operator/MyIdeasScreen.tsx` | ScreenContainer, Card, StatusBadge, Button |
| `mobile/src/screens/operator/SubmitIdeaScreen.tsx` | ScreenContainer scrollable, FormInput ×3, Button |
| `mobile/src/screens/operator/IdeaDetailScreen.tsx` | ScreenContainer scrollable, StatusBadge, Card, Button |
| `mobile/src/screens/manager/IdeaQueueScreen.tsx` | ScreenContainer, Card, StatusBadge |
| `mobile/src/screens/manager/ManagerIdeaDetailScreen.tsx` | ScreenContainer scrollable, StatusBadge, FormInput, Button ×2 |
| `mobile/src/screens/manager/ProjectListScreen.tsx` | ScreenContainer, Card, StatusBadge, Button |
| `mobile/src/screens/manager/ProjectDetailScreen.tsx` | ScreenContainer scrollable, StatusBadge, FormInput, Button |
| `mobile/src/screens/leadership/DashboardScreen.tsx` | ScreenContainer scrollable, Card, StatusBadge, theme |

**Verification command for every task:**
```bash
cd mobile && npx tsc --noEmit
```
Expected: no output (success). Fix any errors before committing.

---

## Phase 1: Design System Foundation

### Task 1: Design System (`src/theme.ts`)

**Files:**
- Create: `mobile/src/theme.ts`

- [ ] **Step 1: Create the theme file**

Create `mobile/src/theme.ts` with this exact content:

```typescript
export const theme = {
  colors: {
    primary: '#2563EB',
    primarySurface: '#EEF2FF',
    text: {
      primary: '#111827',
      secondary: '#6B7280',
      muted: '#9CA3AF',
      inverse: '#FFFFFF',
    },
    surface: {
      background: '#F9FAFB',
      card: '#FFFFFF',
      border: '#E5E7EB',
      inputBorder: '#D1D5DB',
    },
    status: {
      draft:       { bg: '#F3F4F6', text: '#6B7280' },
      underReview: { bg: '#FFFBEB', text: '#D97706' },
      approved:    { bg: '#ECFDF5', text: '#059669' },
      rejected:    { bg: '#FEF2F2', text: '#DC2626' },
      inProgress:  { bg: '#EEF2FF', text: '#2563EB' },
      blocked:     { bg: '#FFF7ED', text: '#C2410C' },
      completed:   { bg: '#ECFDF5', text: '#059669' },
      cancelled:   { bg: '#F3F4F6', text: '#6B7280' },
    },
  },
  typography: {
    display: { fontSize: 36, fontWeight: '700' as const },
    heading: { fontSize: 22, fontWeight: '700' as const },
    title:   { fontSize: 18, fontWeight: '600' as const },
    body:    { fontSize: 15, fontWeight: '400' as const },
    label:   { fontSize: 13, fontWeight: '500' as const },
    caption: { fontSize: 12, fontWeight: '400' as const },
    kpi:     { fontSize: 32, fontWeight: '700' as const },
  },
  spacing: {
    xs:  4,
    sm:  8,
    md:  12,
    lg:  16,
    xl:  24,
    xxl: 32,
  },
  radius: {
    sm:   6,
    md:   8,
    lg:   12,
    full: 999,
  },
} as const;
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/theme.ts
git commit -m "feat: add centralized theme design system"
```

---

### Task 2: Status Normalization (`src/utils/normalizeStatus.ts`)

**Files:**
- Create: `mobile/src/utils/normalizeStatus.ts`

- [ ] **Step 1: Create the utils directory and file**

Create `mobile/src/utils/normalizeStatus.ts`:

```typescript
export type StatusKey =
  | 'draft'
  | 'underReview'
  | 'approved'
  | 'rejected'
  | 'inProgress'
  | 'blocked'
  | 'completed'
  | 'cancelled';

const STATUS_MAP: Record<string, StatusKey> = {
  Draft:       'draft',
  UnderReview: 'underReview',
  Approved:    'approved',
  Rejected:    'rejected',
  InProgress:  'inProgress',
  Blocked:     'blocked',
  Completed:   'completed',
  Cancelled:   'cancelled',
};

export function normalizeStatus(raw: string): StatusKey {
  const key = STATUS_MAP[raw];
  if (!key) {
    if (__DEV__) {
      console.warn(`[normalizeStatus] Unknown status: "${raw}". Falling back to 'draft'.`);
    }
    return 'draft';
  }
  return key;
}
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/utils/normalizeStatus.ts
git commit -m "feat: add status normalization utility (PascalCase to camelCase)"
```

---

### Task 3: ScreenContainer (`src/components/ScreenContainer.tsx`)

**Files:**
- Create: `mobile/src/components/ScreenContainer.tsx`

- [ ] **Step 1: Create the components directory and file**

Create `mobile/src/components/ScreenContainer.tsx`:

```tsx
import React from 'react';
import { ScrollView, StyleSheet, ViewStyle } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { theme } from '../theme';

interface Props {
  children: React.ReactNode;
  style?: ViewStyle;
  scrollable?: boolean;
}

export function ScreenContainer({ children, style, scrollable = false }: Props) {
  if (scrollable) {
    return (
      <SafeAreaView style={[styles.root, style]}>
        <ScrollView
          style={styles.scroll}
          contentContainerStyle={styles.scrollContent}
          showsVerticalScrollIndicator={false}
        >
          {children}
        </ScrollView>
      </SafeAreaView>
    );
  }
  return (
    <SafeAreaView style={[styles.root, style]}>
      {children}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    backgroundColor: theme.colors.surface.background,
    paddingHorizontal: theme.spacing.xl,
  },
  scroll: { flex: 1 },
  scrollContent: {
    paddingTop: theme.spacing.lg,
    paddingBottom: theme.spacing.xxl,
  },
});
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/components/ScreenContainer.tsx
git commit -m "feat: add ScreenContainer with SafeAreaView and consistent padding"
```

---

### Task 4: Button (`src/components/Button.tsx`)

**Files:**
- Create: `mobile/src/components/Button.tsx`

- [ ] **Step 1: Create Button component**

Create `mobile/src/components/Button.tsx`:

```tsx
import React from 'react';
import {
  ActivityIndicator,
  StyleSheet,
  Text,
  TouchableOpacity,
  ViewStyle,
} from 'react-native';
import { theme } from '../theme';

type Variant = 'primary' | 'success' | 'danger' | 'secondary';
type Size = 'sm' | 'md' | 'lg';

interface Props {
  variant: Variant;
  label: string;
  onPress: () => void;
  size?: Size;
  loading?: boolean;
  disabled?: boolean;
  style?: ViewStyle;
}

const VARIANT_STYLES: Record<Variant, { bg: string; text: string; border?: string }> = {
  primary:   { bg: theme.colors.primary,      text: theme.colors.text.inverse },
  success:   { bg: '#059669',                 text: theme.colors.text.inverse },
  danger:    { bg: '#DC2626',                 text: theme.colors.text.inverse },
  secondary: { bg: theme.colors.surface.card, text: theme.colors.text.primary, border: theme.colors.surface.border },
};

const SIZE_STYLES: Record<Size, { paddingVertical: number; paddingHorizontal: number }> = {
  sm: { paddingVertical: 8,  paddingHorizontal: 12 },
  md: { paddingVertical: 12, paddingHorizontal: 20 },
  lg: { paddingVertical: 14, paddingHorizontal: 24 },
};

export function Button({
  variant,
  label,
  onPress,
  size = 'md',
  loading = false,
  disabled = false,
  style,
}: Props) {
  const v = VARIANT_STYLES[variant];
  const s = SIZE_STYLES[size];

  function handlePress() {
    if (loading) return;
    onPress();
  }

  return (
    <TouchableOpacity
      onPress={handlePress}
      disabled={disabled}
      activeOpacity={0.8}
      style={[
        styles.base,
        { backgroundColor: v.bg, ...s },
        v.border ? { borderWidth: 1, borderColor: v.border } : null,
        disabled && styles.disabled,
        style,
      ]}
    >
      <Text style={[styles.label, { color: v.text, opacity: loading ? 0 : 1 }]}>
        {label}
      </Text>
      {loading && (
        <ActivityIndicator style={StyleSheet.absoluteFill} color={v.text} size="small" />
      )}
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  base: {
    borderRadius: theme.radius.md,
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
  },
  label: {
    fontSize: theme.typography.body.fontSize,
    fontWeight: '600',
  },
  disabled: { opacity: 0.5 },
});
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/components/Button.tsx
git commit -m "feat: add Button component with 4 variants, loading guard, and disabled state"
```

---

### Task 5: StatusBadge (`src/components/StatusBadge.tsx`)

**Files:**
- Create: `mobile/src/components/StatusBadge.tsx`

- [ ] **Step 1: Create StatusBadge component**

Create `mobile/src/components/StatusBadge.tsx`:

```tsx
import React from 'react';
import { StyleSheet, Text } from 'react-native';
import { theme } from '../theme';
import { StatusKey } from '../utils/normalizeStatus';

const DISPLAY_LABELS: Record<StatusKey, string> = {
  draft:       'Draft',
  underReview: 'Under Review',
  approved:    'Approved',
  rejected:    'Rejected',
  inProgress:  'In Progress',
  blocked:     'Blocked',
  completed:   'Completed',
  cancelled:   'Cancelled',
};

interface Props {
  status: StatusKey;
}

export function StatusBadge({ status }: Props) {
  const colors = theme.colors.status[status] ?? theme.colors.status.draft;
  const label  = DISPLAY_LABELS[status] ?? 'Unknown';

  return (
    <Text style={[styles.badge, { backgroundColor: colors.bg, color: colors.text }]}>
      {label}
    </Text>
  );
}

const styles = StyleSheet.create({
  badge: {
    ...theme.typography.label,
    paddingVertical: 4,
    paddingHorizontal: 10,
    borderRadius: theme.radius.full,
    alignSelf: 'flex-start',
    overflow: 'hidden',
  },
});
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/components/StatusBadge.tsx
git commit -m "feat: add StatusBadge component with theme color mapping and display labels"
```

---

### Task 6: Card (`src/components/Card.tsx`)

**Files:**
- Create: `mobile/src/components/Card.tsx`

- [ ] **Step 1: Create Card component**

Create `mobile/src/components/Card.tsx`:

```tsx
import React from 'react';
import { StyleSheet, TouchableOpacity, View, ViewStyle } from 'react-native';
import { theme } from '../theme';

interface Props {
  children: React.ReactNode;
  onPress?: () => void;
  style?: ViewStyle;
  padding?: number;
}

export function Card({ children, onPress, style, padding = theme.spacing.lg }: Props) {
  const cardStyle = [styles.card, { padding }, style];

  if (onPress) {
    return (
      <TouchableOpacity
        onPress={onPress}
        activeOpacity={0.7}
        style={[cardStyle, styles.touchable]}
      >
        {children}
      </TouchableOpacity>
    );
  }

  return <View style={cardStyle}>{children}</View>;
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: theme.colors.surface.card,
    borderWidth: 1,
    borderColor: theme.colors.surface.border,
    borderRadius: theme.radius.md,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.06,
    shadowRadius: 3,
    elevation: 2,
  },
  touchable: {
    minHeight: 64,
  },
});
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/components/Card.tsx
git commit -m "feat: add Card component with shadow, optional press handler, and 64px min touch height"
```

---

### Task 7: FormInput (`src/components/FormInput.tsx`)

**Files:**
- Create: `mobile/src/components/FormInput.tsx`

- [ ] **Step 1: Create FormInput component**

Create `mobile/src/components/FormInput.tsx`:

```tsx
import React from 'react';
import { StyleSheet, Text, TextInput, TextInputProps, TextStyle, View } from 'react-native';
import { theme } from '../theme';

interface Props extends Omit<TextInputProps, 'style'> {
  label?: string;
  error?: string;
  inputStyle?: TextStyle;
}

export function FormInput({ label, error, inputStyle, ...rest }: Props) {
  return (
    <View style={styles.wrapper}>
      {label ? <Text style={styles.label}>{label}</Text> : null}
      <TextInput
        style={[styles.input, error ? styles.inputError : null, inputStyle]}
        placeholderTextColor={theme.colors.text.muted}
        {...rest}
      />
      {error ? <Text style={styles.error}>{error}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  wrapper: { marginBottom: theme.spacing.md },
  label: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
    marginBottom: 6,
  },
  input: {
    minHeight: 48,
    borderWidth: 1,
    borderColor: theme.colors.surface.inputBorder,
    borderRadius: theme.radius.md,
    paddingHorizontal: theme.spacing.md,
    paddingVertical: theme.spacing.md,
    ...theme.typography.body,
    color: theme.colors.text.primary,
    backgroundColor: theme.colors.surface.card,
  },
  inputError: {
    borderColor: '#DC2626',
  },
  error: {
    ...theme.typography.caption,
    color: '#DC2626',
    marginTop: 4,
  },
});
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/components/FormInput.tsx
git commit -m "feat: add FormInput component with label, error state, and TextInput prop forwarding"
```

---

## Phase 2: Screen Polish

> **Note:** In each screen task, the complete file content is provided. Replace the entire file — do not attempt to patch line-by-line. The logic (API calls, navigation, query keys, handlers) is preserved exactly; only imports, JSX structure, and StyleSheet are replaced.

---

### Task 8: LoginScreen

**Files:**
- Modify: `mobile/src/screens/auth/LoginScreen.tsx`

- [ ] **Step 1: Replace LoginScreen**

Replace the entire file content with:

```tsx
import React, { useState } from 'react';
import { Alert, StyleSheet, Text, View } from 'react-native';
import * as SecureStore from 'expo-secure-store';
import { API_BASE } from '../../api/client';
import { useAuthStore } from '../../store/authStore';
import { AuthResult } from '../../types/api';
import { theme } from '../../theme';
import { Button } from '../../components/Button';
import { FormInput } from '../../components/FormInput';
import { ScreenContainer } from '../../components/ScreenContainer';

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
    <ScreenContainer>
      <View style={styles.inner}>
        <Text style={styles.title}>Flow</Text>
        <Text style={styles.subtitle}>Innovation Management</Text>
        <View style={styles.form}>
          <FormInput
            label="Email"
            value={email}
            onChangeText={setEmail}
            placeholder="you@example.com"
            autoCapitalize="none"
            keyboardType="email-address"
          />
          <FormInput
            label="Password"
            value={password}
            onChangeText={setPassword}
            placeholder="••••••••"
            secureTextEntry
          />
          <Button
            variant="primary"
            size="lg"
            label="Sign In"
            onPress={handleLogin}
            loading={loading}
            style={styles.button}
          />
        </View>
      </View>
    </ScreenContainer>
  );
}

const styles = StyleSheet.create({
  inner: {
    flex: 1,
    justifyContent: 'center',
  },
  title: {
    ...theme.typography.display,
    color: theme.colors.text.primary,
    textAlign: 'center',
    marginBottom: theme.spacing.xs,
  },
  subtitle: {
    ...theme.typography.body,
    color: theme.colors.text.secondary,
    textAlign: 'center',
    marginBottom: theme.spacing.xxl,
  },
  form: { gap: theme.spacing.xs },
  button: { marginTop: theme.spacing.sm },
});
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/screens/auth/LoginScreen.tsx
git commit -m "polish: apply design system to LoginScreen"
```

---

### Task 9: MyIdeasScreen

**Files:**
- Modify: `mobile/src/screens/operator/MyIdeasScreen.tsx`

- [ ] **Step 1: Replace MyIdeasScreen**

```tsx
import React from 'react';
import { ActivityIndicator, FlatList, StyleSheet, Text, View } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { apiFetch, logout } from '../../api/client';
import { useAuthStore } from '../../store/authStore';
import { IdeaSummary } from '../../types/api';
import { theme } from '../../theme';
import { normalizeStatus } from '../../utils/normalizeStatus';
import { Button } from '../../components/Button';
import { Card } from '../../components/Card';
import { ScreenContainer } from '../../components/ScreenContainer';
import { StatusBadge } from '../../components/StatusBadge';

export function MyIdeasScreen({ navigation }: any) {
  const session = useAuthStore((s) => s.session);

  const { data: ideas, isLoading, isFetching, error, refetch } = useQuery<IdeaSummary[]>({
    queryKey: ['ideas'],
    queryFn: () => apiFetch<IdeaSummary[]>('/ideas'),
  });

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color={theme.colors.primary} />;
  }
  if (error) {
    return <Text style={styles.errorText}>{(error as Error).message}</Text>;
  }

  return (
    <ScreenContainer>
      <Button
        variant="primary"
        label="+ New Idea"
        onPress={() => navigation.navigate('SubmitIdea')}
        style={styles.newButton}
      />
      <FlatList
        data={ideas ?? []}
        keyExtractor={(item) => item.id}
        onRefresh={() => { refetch(); }}
        refreshing={isFetching}
        renderItem={({ item }) => (
          <Card
            onPress={() => navigation.navigate('IdeaDetail', { id: item.id })}
            style={styles.card}
          >
            <Text style={styles.cardTitle}>{item.title}</Text>
            <Text style={styles.meta} numberOfLines={2}>{item.problem}</Text>
            <View style={styles.cardFooter}>
              <Text style={styles.priority}>{item.priority}</Text>
              <StatusBadge status={normalizeStatus(item.status)} />
            </View>
          </Card>
        )}
        ListEmptyComponent={
          <Text style={styles.emptyText}>No ideas yet. Tap "+ New Idea" to get started.</Text>
        }
        contentContainerStyle={{ paddingBottom: theme.spacing.xl }}
        showsVerticalScrollIndicator={false}
      />
      <Button
        variant="secondary"
        label="Sign Out"
        onPress={() => logout(session?.refreshToken ?? '')}
        style={styles.logoutBtn}
      />
    </ScreenContainer>
  );
}

const styles = StyleSheet.create({
  newButton: { marginTop: theme.spacing.lg, marginBottom: theme.spacing.lg },
  card: { marginBottom: theme.spacing.md },
  cardTitle: {
    ...theme.typography.title,
    color: theme.colors.text.primary,
    marginBottom: theme.spacing.xs,
  },
  meta: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
    marginBottom: theme.spacing.sm,
  },
  cardFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  priority: {
    ...theme.typography.caption,
    color: theme.colors.text.muted,
  },
  emptyText: {
    ...theme.typography.body,
    color: theme.colors.text.muted,
    textAlign: 'center',
    marginTop: 48,
    lineHeight: 22,
  },
  errorText: {
    ...theme.typography.body,
    textAlign: 'center',
    color: '#EF4444',
    marginTop: 48,
    padding: theme.spacing.lg,
  },
  logoutBtn: { marginTop: theme.spacing.sm, marginBottom: theme.spacing.lg },
});
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/screens/operator/MyIdeasScreen.tsx
git commit -m "polish: apply design system to MyIdeasScreen"
```

---

### Task 10: SubmitIdeaScreen

**Files:**
- Modify: `mobile/src/screens/operator/SubmitIdeaScreen.tsx`

- [ ] **Step 1: Replace SubmitIdeaScreen**

```tsx
import React, { useState } from 'react';
import { Alert } from 'react-native';
import { useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../../api/client';
import { IdeaSummary } from '../../types/api';
import { Button } from '../../components/Button';
import { FormInput } from '../../components/FormInput';
import { ScreenContainer } from '../../components/ScreenContainer';

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
    <ScreenContainer scrollable>
      <FormInput
        label="Title *"
        value={title}
        onChangeText={setTitle}
        placeholder="Brief, descriptive title"
      />
      <FormInput
        label="Problem *"
        value={problem}
        onChangeText={setProblem}
        placeholder="What problem does this idea solve?"
        multiline
        numberOfLines={3}
        textAlignVertical="top"
        inputStyle={{ minHeight: 80 }}
      />
      <FormInput
        label="Description *"
        value={description}
        onChangeText={setDescription}
        placeholder="Describe your idea in detail"
        multiline
        numberOfLines={5}
        textAlignVertical="top"
        inputStyle={{ minHeight: 120 }}
      />
      <Button
        variant="primary"
        size="lg"
        label="Create Idea"
        onPress={handleCreate}
        loading={loading}
      />
    </ScreenContainer>
  );
}
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/screens/operator/SubmitIdeaScreen.tsx
git commit -m "polish: apply design system to SubmitIdeaScreen"
```

---

### Task 11: IdeaDetailScreen

**Files:**
- Modify: `mobile/src/screens/operator/IdeaDetailScreen.tsx`

- [ ] **Step 1: Replace IdeaDetailScreen**

```tsx
import React from 'react';
import { ActivityIndicator, Alert, StyleSheet, Text, View } from 'react-native';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../../api/client';
import { IdeaDetail } from '../../types/api';
import { theme } from '../../theme';
import { normalizeStatus } from '../../utils/normalizeStatus';
import { Button } from '../../components/Button';
import { Card } from '../../components/Card';
import { ScreenContainer } from '../../components/ScreenContainer';
import { StatusBadge } from '../../components/StatusBadge';

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
    return <ActivityIndicator style={{ flex: 1 }} size="large" color={theme.colors.primary} />;
  }
  if (error || !idea) {
    return <Text style={styles.errorText}>{(error as Error)?.message ?? 'Idea not found'}</Text>;
  }

  return (
    <ScreenContainer scrollable>
      <Text style={styles.title}>{idea.title}</Text>
      <View style={styles.metaRow}>
        <StatusBadge status={normalizeStatus(idea.status)} />
        <Text style={styles.priority}>{idea.priority}</Text>
      </View>

      <Text style={styles.sectionLabel}>Problem</Text>
      <Text style={styles.body}>{idea.problem}</Text>

      <Text style={styles.sectionLabel}>Description</Text>
      <Text style={styles.body}>{idea.description}</Text>

      {idea.managerComment ? (
        <View style={styles.commentSection}>
          <Text style={styles.sectionLabel}>Manager Comment</Text>
          <Card padding={theme.spacing.md}>
            <Text style={styles.commentText}>{idea.managerComment}</Text>
          </Card>
        </View>
      ) : null}

      <Text style={styles.timestamp}>
        Created: {new Date(idea.createdAt).toLocaleDateString()}
      </Text>

      {idea.status === 'Draft' && (
        <Button
          variant="primary"
          size="lg"
          label="Submit for Review"
          onPress={handleSubmitForReview}
          style={styles.actionBtn}
        />
      )}
    </ScreenContainer>
  );
}

const styles = StyleSheet.create({
  title: {
    ...theme.typography.title,
    color: theme.colors.text.primary,
    marginBottom: theme.spacing.sm,
  },
  metaRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: theme.spacing.sm,
    marginBottom: theme.spacing.xl,
  },
  priority: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
  },
  sectionLabel: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
    marginTop: theme.spacing.xl,
    marginBottom: theme.spacing.xs,
  },
  body: {
    ...theme.typography.body,
    color: theme.colors.text.primary,
    lineHeight: 22,
  },
  commentSection: { marginTop: theme.spacing.xl },
  commentText: {
    ...theme.typography.body,
    color: theme.colors.text.primary,
    lineHeight: 20,
  },
  timestamp: {
    ...theme.typography.caption,
    color: theme.colors.text.muted,
    marginTop: theme.spacing.xl,
  },
  actionBtn: { marginTop: theme.spacing.xxl },
  errorText: {
    textAlign: 'center',
    color: '#EF4444',
    marginTop: 48,
    padding: theme.spacing.lg,
  },
});
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/screens/operator/IdeaDetailScreen.tsx
git commit -m "polish: apply design system to IdeaDetailScreen"
```

---

### Task 12: IdeaQueueScreen

**Files:**
- Modify: `mobile/src/screens/manager/IdeaQueueScreen.tsx`

- [ ] **Step 1: Replace IdeaQueueScreen**

```tsx
import React from 'react';
import { ActivityIndicator, FlatList, StyleSheet, Text, View } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { apiFetch } from '../../api/client';
import { IdeaSummary } from '../../types/api';
import { theme } from '../../theme';
import { normalizeStatus } from '../../utils/normalizeStatus';
import { Card } from '../../components/Card';
import { ScreenContainer } from '../../components/ScreenContainer';
import { StatusBadge } from '../../components/StatusBadge';

export function IdeaQueueScreen({ navigation }: any) {
  const { data: ideas, isLoading, isFetching, error, refetch } = useQuery<IdeaSummary[]>({
    queryKey: ['ideas', 'all'],
    queryFn: () => apiFetch<IdeaSummary[]>('/ideas'),
  });

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color={theme.colors.primary} />;
  }
  if (error) {
    return <Text style={styles.errorText}>{(error as Error).message}</Text>;
  }

  const pending = (ideas ?? []).filter((i) => i.status === 'UnderReview');
  const rest    = (ideas ?? []).filter((i) => i.status !== 'UnderReview');
  const sorted  = [...pending, ...rest];

  return (
    <ScreenContainer>
      <FlatList
        data={sorted}
        keyExtractor={(item) => item.id}
        onRefresh={() => { refetch(); }}
        refreshing={isFetching}
        renderItem={({ item }) => (
          <Card
            onPress={() => navigation.navigate('ManagerIdeaDetail', { id: item.id })}
            style={[
              styles.card,
              item.status === 'UnderReview' && styles.cardHighlight,
            ]}
          >
            <Text style={styles.cardTitle}>{item.title}</Text>
            <Text style={styles.meta} numberOfLines={2}>{item.problem}</Text>
            <View style={styles.cardFooter}>
              <Text style={styles.priority}>{item.priority}</Text>
              <StatusBadge status={normalizeStatus(item.status)} />
            </View>
          </Card>
        )}
        ListEmptyComponent={
          <Text style={styles.emptyText}>No ideas submitted yet.</Text>
        }
        contentContainerStyle={{ paddingTop: theme.spacing.lg, paddingBottom: theme.spacing.xl }}
        showsVerticalScrollIndicator={false}
      />
    </ScreenContainer>
  );
}

const styles = StyleSheet.create({
  card: { marginBottom: theme.spacing.md },
  cardHighlight: {
    borderColor: '#FCD34D',
    backgroundColor: theme.colors.status.underReview.bg,
  },
  cardTitle: {
    ...theme.typography.title,
    color: theme.colors.text.primary,
    marginBottom: theme.spacing.xs,
  },
  meta: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
    marginBottom: theme.spacing.sm,
  },
  cardFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  priority: {
    ...theme.typography.caption,
    color: theme.colors.text.muted,
  },
  emptyText: {
    ...theme.typography.body,
    color: theme.colors.text.muted,
    textAlign: 'center',
    marginTop: 48,
  },
  errorText: {
    ...theme.typography.body,
    textAlign: 'center',
    color: '#EF4444',
    marginTop: 48,
    padding: theme.spacing.lg,
  },
});
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/screens/manager/IdeaQueueScreen.tsx
git commit -m "polish: apply design system to IdeaQueueScreen"
```

---

### Task 13: ManagerIdeaDetailScreen

**Files:**
- Modify: `mobile/src/screens/manager/ManagerIdeaDetailScreen.tsx`

- [ ] **Step 1: Replace ManagerIdeaDetailScreen**

```tsx
import React, { useState } from 'react';
import { ActivityIndicator, Alert, StyleSheet, Text, View } from 'react-native';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../../api/client';
import { IdeaDetail } from '../../types/api';
import { theme } from '../../theme';
import { normalizeStatus } from '../../utils/normalizeStatus';
import { Button } from '../../components/Button';
import { FormInput } from '../../components/FormInput';
import { ScreenContainer } from '../../components/ScreenContainer';
import { StatusBadge } from '../../components/StatusBadge';

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
    return <ActivityIndicator style={{ flex: 1 }} size="large" color={theme.colors.primary} />;
  }
  if (error || !idea) {
    return <Text style={styles.errorText}>{(error as Error)?.message ?? 'Not found'}</Text>;
  }

  const canAct = idea.status === 'UnderReview';

  return (
    <ScreenContainer scrollable>
      <Text style={styles.title}>{idea.title}</Text>
      <View style={styles.metaRow}>
        <StatusBadge status={normalizeStatus(idea.status)} />
        <Text style={styles.priority}>{idea.priority}</Text>
      </View>

      <Text style={styles.sectionLabel}>Problem</Text>
      <Text style={styles.body}>{idea.problem}</Text>

      <Text style={styles.sectionLabel}>Description</Text>
      <Text style={styles.body}>{idea.description}</Text>

      {idea.managerComment ? (
        <View style={styles.section}>
          <Text style={styles.sectionLabel}>Manager Comment</Text>
          <Text style={styles.body}>{idea.managerComment}</Text>
        </View>
      ) : null}

      {canAct && (
        <View style={styles.actionsSection}>
          <FormInput
            label="Comment (required for rejection)"
            value={comment}
            onChangeText={setComment}
            placeholder="Add a comment..."
            multiline
            numberOfLines={3}
            textAlignVertical="top"
            inputStyle={{ minHeight: 72 }}
          />
          <View style={styles.actionsRow}>
            <Button
              variant="success"
              label="Approve"
              onPress={handleApprove}
              loading={actionLoading}
              style={styles.actionBtn}
            />
            <Button
              variant="danger"
              label="Reject"
              onPress={handleReject}
              loading={actionLoading}
              style={styles.actionBtn}
            />
          </View>
        </View>
      )}

      {!canAct && (
        <Text style={styles.resolvedNote}>
          This idea has already been {idea.status.toLowerCase()}.
        </Text>
      )}
    </ScreenContainer>
  );
}

const styles = StyleSheet.create({
  title: {
    ...theme.typography.title,
    color: theme.colors.text.primary,
    marginBottom: theme.spacing.sm,
  },
  metaRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: theme.spacing.sm,
    marginBottom: theme.spacing.xl,
  },
  priority: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
  },
  sectionLabel: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
    marginTop: theme.spacing.xl,
    marginBottom: theme.spacing.xs,
  },
  body: {
    ...theme.typography.body,
    color: theme.colors.text.primary,
    lineHeight: 22,
  },
  section: { marginTop: theme.spacing.xl },
  actionsSection: { marginTop: theme.spacing.xxl },
  actionsRow: {
    flexDirection: 'row',
    gap: theme.spacing.md,
    marginTop: theme.spacing.md,
  },
  actionBtn: { flex: 1 },
  resolvedNote: {
    ...theme.typography.body,
    color: theme.colors.text.secondary,
    textAlign: 'center',
    marginTop: theme.spacing.xxl,
  },
  errorText: {
    textAlign: 'center',
    color: '#EF4444',
    marginTop: 48,
    padding: theme.spacing.lg,
  },
});
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/screens/manager/ManagerIdeaDetailScreen.tsx
git commit -m "polish: apply design system to ManagerIdeaDetailScreen"
```

---

### Task 14: ProjectListScreen

**Files:**
- Modify: `mobile/src/screens/manager/ProjectListScreen.tsx`

- [ ] **Step 1: Replace ProjectListScreen**

```tsx
import React from 'react';
import { ActivityIndicator, FlatList, StyleSheet, Text, View } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { apiFetch, logout } from '../../api/client';
import { useAuthStore } from '../../store/authStore';
import { ProjectSummary } from '../../types/api';
import { theme } from '../../theme';
import { normalizeStatus } from '../../utils/normalizeStatus';
import { Button } from '../../components/Button';
import { Card } from '../../components/Card';
import { ScreenContainer } from '../../components/ScreenContainer';
import { StatusBadge } from '../../components/StatusBadge';

export function ProjectListScreen({ navigation }: any) {
  const session = useAuthStore((s) => s.session);

  const { data: projects, isLoading, isFetching, error, refetch } = useQuery<ProjectSummary[]>({
    queryKey: ['projects'],
    queryFn: () => apiFetch<ProjectSummary[]>('/projects'),
  });

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color={theme.colors.primary} />;
  }
  if (error) {
    return <Text style={styles.errorText}>{(error as Error).message}</Text>;
  }

  return (
    <ScreenContainer>
      <FlatList
        data={projects ?? []}
        keyExtractor={(item) => item.id}
        onRefresh={() => { refetch(); }}
        refreshing={isFetching}
        renderItem={({ item }) => (
          <Card
            onPress={() => navigation.navigate('ProjectDetail', { id: item.id })}
            style={[
              styles.card,
              item.status === 'Blocked' && styles.cardBlocked,
            ]}
          >
            <Text style={styles.cardTitle}>{item.title}</Text>
            {item.blockedReason ? (
              <Text style={styles.blockedReason} numberOfLines={1}>
                {item.blockedReason}
              </Text>
            ) : null}
            <View style={styles.cardFooter}>
              <Text style={styles.priority}>{item.priority}</Text>
              <StatusBadge status={normalizeStatus(item.status)} />
            </View>
          </Card>
        )}
        ListEmptyComponent={
          <Text style={styles.emptyText}>No projects yet.</Text>
        }
        contentContainerStyle={{ paddingTop: theme.spacing.lg, paddingBottom: theme.spacing.xl }}
        showsVerticalScrollIndicator={false}
      />
      <Button
        variant="secondary"
        label="Sign Out"
        onPress={() => logout(session?.refreshToken ?? '')}
        style={styles.logoutBtn}
      />
    </ScreenContainer>
  );
}

const styles = StyleSheet.create({
  card: { marginBottom: theme.spacing.md },
  cardBlocked: {
    borderColor: '#FED7AA',
    backgroundColor: theme.colors.status.blocked.bg,
  },
  cardTitle: {
    ...theme.typography.title,
    color: theme.colors.text.primary,
    marginBottom: theme.spacing.xs,
  },
  blockedReason: {
    ...theme.typography.label,
    color: theme.colors.status.blocked.text,
    marginBottom: theme.spacing.sm,
  },
  cardFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  priority: {
    ...theme.typography.caption,
    color: theme.colors.text.muted,
  },
  emptyText: {
    ...theme.typography.body,
    color: theme.colors.text.muted,
    textAlign: 'center',
    marginTop: 48,
  },
  errorText: {
    ...theme.typography.body,
    textAlign: 'center',
    color: '#EF4444',
    marginTop: 48,
    padding: theme.spacing.lg,
  },
  logoutBtn: { marginTop: theme.spacing.sm, marginBottom: theme.spacing.lg },
});
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/screens/manager/ProjectListScreen.tsx
git commit -m "polish: apply design system to ProjectListScreen"
```

---

### Task 15: ProjectDetailScreen

**Files:**
- Modify: `mobile/src/screens/manager/ProjectDetailScreen.tsx`

- [ ] **Step 1: Replace ProjectDetailScreen**

```tsx
import React, { useState } from 'react';
import { ActivityIndicator, Alert, StyleSheet, Text, View } from 'react-native';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../../api/client';
import { ProjectDetail } from '../../types/api';
import { theme } from '../../theme';
import { normalizeStatus } from '../../utils/normalizeStatus';
import { Button } from '../../components/Button';
import { FormInput } from '../../components/FormInput';
import { ScreenContainer } from '../../components/ScreenContainer';
import { StatusBadge } from '../../components/StatusBadge';

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
    return <ActivityIndicator style={{ flex: 1 }} size="large" color={theme.colors.primary} />;
  }
  if (error || !project) {
    return <Text style={styles.errorText}>{(error as Error)?.message ?? 'Not found'}</Text>;
  }

  return (
    <ScreenContainer scrollable>
      <Text style={styles.title}>{project.title}</Text>
      <View style={styles.metaRow}>
        <StatusBadge status={normalizeStatus(project.status)} />
        <Text style={styles.priority}>{project.priority}</Text>
      </View>

      <Text style={styles.sectionLabel}>Description</Text>
      <Text style={styles.body}>{project.description}</Text>

      {project.blockedReason ? (
        <View style={styles.section}>
          <Text style={styles.sectionLabel}>Blocked Reason</Text>
          <View style={styles.blockedBox}>
            <Text style={styles.blockedText}>{project.blockedReason}</Text>
          </View>
        </View>
      ) : null}

      <View style={styles.section}>
        <Text style={styles.sectionLabel}>Details</Text>
        {project.estimatedCost != null && (
          <Text style={styles.meta}>Estimated Cost: ${project.estimatedCost.toLocaleString()}</Text>
        )}
        {project.deadline && (
          <Text style={styles.meta}>Deadline: {new Date(project.deadline).toLocaleDateString()}</Text>
        )}
        {project.startDate && (
          <Text style={styles.meta}>Started: {new Date(project.startDate).toLocaleDateString()}</Text>
        )}
        {project.completedAt && (
          <Text style={styles.meta}>Completed: {new Date(project.completedAt).toLocaleDateString()}</Text>
        )}
      </View>

      <View style={styles.actionsSection}>
        {project.status === 'Planning' && (
          <Button
            variant="primary"
            size="lg"
            label="Start Project"
            onPress={() => callAction(`/projects/${id}/start`)}
            loading={actionLoading}
          />
        )}

        {project.status === 'InProgress' && (
          <View style={styles.inProgressActions}>
            <Button
              variant="success"
              size="lg"
              label="Mark Complete"
              onPress={() => callAction(`/projects/${id}/complete`)}
              loading={actionLoading}
            />
            <View style={styles.blockSection}>
              <FormInput
                label="Block Reason *"
                value={blockReason}
                onChangeText={setBlockReason}
                placeholder="Why is this project blocked?"
                multiline
                numberOfLines={2}
                textAlignVertical="top"
                inputStyle={{ minHeight: 60 }}
              />
              <Button
                variant="danger"
                label="Block Project"
                onPress={() => {
                  if (!blockReason.trim()) {
                    Alert.alert('Validation', 'A block reason is required.');
                    return;
                  }
                  callAction(`/projects/${id}/block`, { reason: blockReason });
                }}
                loading={actionLoading}
              />
            </View>
          </View>
        )}

        {project.status === 'Blocked' && (
          <Button
            variant="success"
            size="lg"
            label="Unblock Project"
            onPress={() => callAction(`/projects/${id}/unblock`)}
            loading={actionLoading}
          />
        )}
      </View>
    </ScreenContainer>
  );
}

const styles = StyleSheet.create({
  title: {
    ...theme.typography.title,
    color: theme.colors.text.primary,
    marginBottom: theme.spacing.sm,
  },
  metaRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: theme.spacing.sm,
    marginBottom: theme.spacing.xl,
  },
  priority: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
  },
  sectionLabel: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
    marginBottom: theme.spacing.xs,
  },
  body: {
    ...theme.typography.body,
    color: theme.colors.text.primary,
    lineHeight: 22,
  },
  section: { marginTop: theme.spacing.xxl },
  blockedBox: {
    backgroundColor: theme.colors.status.blocked.bg,
    borderRadius: theme.radius.md,
    padding: theme.spacing.md,
  },
  blockedText: {
    ...theme.typography.body,
    color: theme.colors.status.blocked.text,
  },
  meta: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
    marginTop: theme.spacing.sm,
  },
  actionsSection: { marginTop: theme.spacing.xxl },
  inProgressActions: { gap: theme.spacing.lg },
  blockSection: { marginTop: theme.spacing.lg, gap: theme.spacing.sm },
  errorText: {
    textAlign: 'center',
    color: '#EF4444',
    marginTop: 48,
    padding: theme.spacing.lg,
  },
});
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/screens/manager/ProjectDetailScreen.tsx
git commit -m "polish: apply design system to ProjectDetailScreen"
```

---

### Task 16: DashboardScreen

**Files:**
- Modify: `mobile/src/screens/leadership/DashboardScreen.tsx`

- [ ] **Step 1: Replace DashboardScreen**

```tsx
import React from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { apiFetch, logout } from '../../api/client';
import { useAuthStore } from '../../store/authStore';
import { BlockedProject, DashboardSummary } from '../../types/api';
import { theme } from '../../theme';
import { normalizeStatus } from '../../utils/normalizeStatus';
import { Button } from '../../components/Button';
import { Card } from '../../components/Card';
import { ScreenContainer } from '../../components/ScreenContainer';
import { StatusBadge } from '../../components/StatusBadge';

export function DashboardScreen() {
  const session = useAuthStore((s) => s.session);

  const { data, isLoading, error } = useQuery<DashboardSummary>({
    queryKey: ['dashboard'],
    queryFn: () => apiFetch<DashboardSummary>('/dashboard/summary'),
    refetchInterval: 60_000,
  });

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color={theme.colors.primary} />;
  }
  if (error || !data) {
    return (
      <Text style={styles.errorText}>
        {(error as Error)?.message ?? 'Failed to load dashboard'}
      </Text>
    );
  }

  const sortedBlocked = [...data.blockedProjectList].sort(
    (a, b) => b.daysBlocked - a.daysBlocked
  );

  return (
    <ScreenContainer scrollable>
      <Text style={styles.heading}>Innovation Overview</Text>

      <Text style={styles.sectionTitle}>Ideas</Text>
      <View style={styles.row}>
        <KpiCard label="Total"         value={data.totalIdeas} />
        <KpiCard label="Approved"      value={data.approvedIdeas}  color="#059669" />
        <KpiCard label="Rejected"      value={data.rejectedIdeas}  color="#DC2626" />
      </View>
      <View style={styles.row}>
        <KpiCard label="Under Review"  value={data.pendingIdeas}   color="#D97706" />
        <KpiCard label="Conversion"    value={`${data.conversionRate.toFixed(1)}%`} color={theme.colors.primary} />
      </View>

      <Text style={styles.sectionTitle}>Projects</Text>
      <View style={styles.row}>
        <KpiCard label="Active"        value={data.activeProjects}    color={theme.colors.primary} />
        <KpiCard label="Blocked"       value={data.blockedProjects}   color="#C2410C" />
        <KpiCard label="Completed"     value={data.completedProjects} color="#059669" />
      </View>
      <View style={styles.row}>
        <KpiCard label="Avg Completion" value={`${data.averageCompletionDays}d`} />
        <KpiCard
          label="Bottleneck"
          value={`${data.bottleneckIndex.toFixed(1)}%`}
          color={data.bottleneckIndex > 30 ? '#DC2626' : '#059669'}
        />
        <KpiCard label="Total ROI"     value={`${data.totalRoi.toFixed(0)}%`} color={theme.colors.primary} />
      </View>

      {sortedBlocked.length > 0 && (
        <View style={styles.blockedSection}>
          <Text style={styles.sectionTitle}>Blocked Projects</Text>
          {sortedBlocked.map((p: BlockedProject) => (
            <Card key={p.id} style={styles.blockedCard} padding={theme.spacing.lg}>
              <View style={styles.blockedHeader}>
                <Text style={styles.blockedTitle} numberOfLines={1}>{p.title}</Text>
                <View style={styles.blockedMeta}>
                  <Text style={styles.blockedDays}>{p.daysBlocked}d</Text>
                  <StatusBadge status={normalizeStatus('Blocked')} />
                </View>
              </View>
              <Text style={styles.blockedReason}>{p.blockedReason}</Text>
            </Card>
          ))}
        </View>
      )}

      <Button
        variant="secondary"
        label="Sign Out"
        onPress={() => logout(session?.refreshToken ?? '')}
        style={styles.logoutBtn}
      />
    </ScreenContainer>
  );
}

function KpiCard({
  label,
  value,
  color = theme.colors.text.primary,
}: {
  label: string;
  value: string | number;
  color?: string;
}) {
  return (
    <Card style={styles.kpiCard} padding={theme.spacing.xl}>
      <Text style={[styles.kpiValue, { color }]}>{value}</Text>
      <Text style={styles.kpiLabel}>{label}</Text>
    </Card>
  );
}

const styles = StyleSheet.create({
  heading: {
    ...theme.typography.heading,
    color: theme.colors.text.primary,
    marginBottom: theme.spacing.xs,
  },
  sectionTitle: {
    ...theme.typography.title,
    color: theme.colors.text.primary,
    marginTop: theme.spacing.xxl,
    marginBottom: theme.spacing.md,
  },
  row: {
    flexDirection: 'row',
    gap: theme.spacing.sm,
    marginBottom: theme.spacing.sm,
    alignItems: 'stretch',
  },
  kpiCard: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  kpiValue: {
    ...theme.typography.kpi,
    marginBottom: theme.spacing.xs,
    textAlign: 'center',
  },
  kpiLabel: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
    textAlign: 'center',
  },
  blockedSection: { marginTop: theme.spacing.xxl },
  blockedCard: {
    marginBottom: theme.spacing.md,
    backgroundColor: theme.colors.status.blocked.bg,
    borderColor: '#FED7AA',
  },
  blockedHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: theme.spacing.xs,
  },
  blockedTitle: {
    ...theme.typography.title,
    color: theme.colors.text.primary,
    flex: 1,
    marginRight: theme.spacing.sm,
  },
  blockedMeta: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: theme.spacing.sm,
  },
  blockedDays: {
    ...theme.typography.label,
    color: theme.colors.status.blocked.text,
    fontWeight: '700',
  },
  blockedReason: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
    lineHeight: 18,
  },
  logoutBtn: { marginTop: theme.spacing.xxl },
  errorText: {
    textAlign: 'center',
    color: '#EF4444',
    marginTop: 48,
    padding: theme.spacing.lg,
  },
});
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd mobile && npx tsc --noEmit
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/screens/leadership/DashboardScreen.tsx
git commit -m "polish: apply design system to DashboardScreen"
```

---

## Self-Review Checklist

After all 16 tasks are complete, verify:

- [ ] `npx tsc --noEmit` passes with zero errors from `mobile/`
- [ ] Every screen imports from `../../theme`, `../../utils/normalizeStatus`, and `../../components/*`
- [ ] No screen has a hardcoded hex color, hardcoded font size, or hardcoded font weight in its `StyleSheet`
- [ ] Every screen's outermost element is `ScreenContainer`
- [ ] Every list item card uses `Card` with `onPress` (and therefore has `minHeight: 64`)
- [ ] Every status display uses `StatusBadge` with `normalizeStatus(item.status)`
- [ ] No screen has more than one `Button variant="primary"`
- [ ] Dashboard KPI rows have `alignItems: 'stretch'`; `KpiCard` has `justifyContent: 'center'`
