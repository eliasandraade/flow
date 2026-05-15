import React from 'react';
import { View, Text, StyleSheet } from 'react-native';

export function PlaceholderOperatorScreen() {
  return (
    <View style={styles.container}>
      <Text style={styles.text}>Operator Home (Task 16)</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: '#F9FAFB' },
  text: { fontSize: 18, color: '#374151' },
});
