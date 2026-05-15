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
            style={
              item.status === 'UnderReview'
                ? [styles.card, styles.cardHighlight]
                : styles.card
            }
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
