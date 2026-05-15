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
        Created: {new Date(idea.createdAt).toLocaleDateString()}
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
  sectionLabel: { fontSize: 13, fontWeight: '600', color: '#374151', marginTop: 16, marginBottom: 4 },
  body: { fontSize: 15, color: '#374151', lineHeight: 22 },
  commentBox: { backgroundColor: '#FEF3C7', borderRadius: 6, padding: 10, marginTop: 4 },
  commentText: { fontSize: 14, color: '#92400E', lineHeight: 20 },
  timestamp: { fontSize: 12, color: '#9CA3AF', marginTop: 16 },
  submitBtn: {
    backgroundColor: '#059669', borderRadius: 8,
    padding: 14, alignItems: 'center', marginTop: 24,
  },
  submitBtnText: { color: '#FFF', fontWeight: '600', fontSize: 16 },
  errorText: { textAlign: 'center', color: '#EF4444', marginTop: 48, padding: 16 },
});
