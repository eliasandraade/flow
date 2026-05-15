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
    borderColor: theme.colors.status.rejected.text,
  },
  error: {
    ...theme.typography.caption,
    color: theme.colors.status.rejected.text,
    marginTop: 4,
  },
});
