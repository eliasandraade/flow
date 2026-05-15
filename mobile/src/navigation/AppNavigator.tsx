import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useAuthStore } from '../store/authStore';
import { LoginScreen } from '../screens/auth/LoginScreen';
import { PlaceholderOperatorScreen } from '../screens/operator/PlaceholderOperatorScreen';
import { PlaceholderManagerScreen } from '../screens/manager/PlaceholderManagerScreen';
import { PlaceholderLeadershipScreen } from '../screens/leadership/PlaceholderLeadershipScreen';

const AuthStack = createNativeStackNavigator();
const AppStack = createNativeStackNavigator();

function AuthNavigator() {
  return (
    <AuthStack.Navigator screenOptions={{ headerShown: false }}>
      <AuthStack.Screen name="Login" component={LoginScreen} />
    </AuthStack.Navigator>
  );
}

function OperatorNavigator() {
  return (
    <AppStack.Navigator>
      <AppStack.Screen
        name="OperatorHome"
        component={PlaceholderOperatorScreen}
        options={{ title: 'My Ideas' }}
      />
    </AppStack.Navigator>
  );
}

function ManagerNavigator() {
  return (
    <AppStack.Navigator>
      <AppStack.Screen
        name="ManagerHome"
        component={PlaceholderManagerScreen}
        options={{ title: 'Manager' }}
      />
    </AppStack.Navigator>
  );
}

function LeadershipNavigator() {
  return (
    <AppStack.Navigator>
      <AppStack.Screen
        name="LeadershipHome"
        component={PlaceholderLeadershipScreen}
        options={{ title: 'Dashboard' }}
      />
    </AppStack.Navigator>
  );
}

export function AppNavigator() {
  const session = useAuthStore((s) => s.session);

  return (
    <NavigationContainer>
      {!session ? (
        <AuthNavigator />
      ) : session.role === 'Operator' ? (
        <OperatorNavigator />
      ) : session.role === 'Manager' ? (
        <ManagerNavigator />
      ) : (
        <LeadershipNavigator />
      )}
    </NavigationContainer>
  );
}
