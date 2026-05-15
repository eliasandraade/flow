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
    color: theme.colors.status.rejected.text,
    marginTop: 48,
    padding: theme.spacing.lg,
  },
});
