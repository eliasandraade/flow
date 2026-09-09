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
              ...(item.status === 'Blocked' ? [styles.cardBlocked] : []),
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
    borderColor: theme.colors.status.blocked.border,
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
    marginTop: theme.spacing.xxxl,
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
