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
