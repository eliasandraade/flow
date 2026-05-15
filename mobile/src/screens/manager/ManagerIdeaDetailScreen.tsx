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
