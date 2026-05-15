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
