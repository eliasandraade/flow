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
    marginTop: theme.spacing.xxxl,
    lineHeight: 22,
  },
  errorText: {
    ...theme.typography.body,
    textAlign: 'center',
    color: theme.colors.status.rejected.text,
    marginTop: theme.spacing.xxxl,
    padding: theme.spacing.lg,
  },
  logoutBtn: { marginTop: theme.spacing.sm, marginBottom: theme.spacing.lg },
});
