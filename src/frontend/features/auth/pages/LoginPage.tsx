/**
 * Login Page
 * Handles user authentication with cyberpunk terminal aesthetics
 */

import React, { useState } from 'react';
import { GridBackground } from '@shared/components/layout';
import { Input, Button, Card } from '@shared/components/ui';
import { ErrorMessage } from '@shared/components/feedback';
import { validateEmail, validatePassword } from '@core/utils';
import { useAuth } from '@core/context/AuthContext';

interface LoginPageProps {
  onLoginSuccess: () => void;
  onGoSignup: () => void;
}

export const LoginPage: React.FC<LoginPageProps> = ({ onLoginSuccess, onGoSignup }) => {
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setValidationErrors({});

    // Client-side validation (OWASP A03 - Input Validation)
    const emailValidation = validateEmail(email);
    const passwordValidation = validatePassword(password);

    const errors: Record<string, string> = {};
    if (!emailValidation.isValid) {
      errors.email = emailValidation.errors[0];
    }
    if (!passwordValidation.isValid) {
      errors.password = passwordValidation.errors[0];
    }

    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);
      return;
    }

    setLoading(true);
    try {
      // Use AuthContext login to update global state
      await login({ email, password });
      onLoginSuccess();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Error de autenticación';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-background-dark relative p-6">
      <GridBackground opacity={0.1} size={40} />
      
      <div className="relative z-10 w-full max-w-[450px]">
        <Card className="p-8 shadow-[0_0_20px_rgba(37,244,106,0.15)] font-mono">
          {/* Header */}
          <div className="text-center mb-8">
            <h1 className="text-primary text-4xl font-display font-black text-glow uppercase mb-2">
              HefestAI
            </h1>
            <p className="text-primary/60 text-xs tracking-widest uppercase">
              Kernel v3.0 // Authentication
            </p>
          </div>

          {/* Form */}
          <form onSubmit={handleLogin} className="space-y-6">
            <Input
              label="Identificador de Usuario"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="USER_ID@omega.sys"
              error={validationErrors.email}
              autoComplete="email"
            />

            <Input
              label="Código de Acceso"
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              error={validationErrors.password}
              autoComplete="current-password"
            />

            {error && <ErrorMessage message={error} />}

            <Button
              type="submit"
              variant="primary"
              fullWidth
              size="lg"
              isLoading={loading}
              icon="login"
            >
              ACCEDER_AL_NUCLEO
            </Button>
          </form>

          {/* Signup Link */}
          <div className="mt-8 pt-6 border-t border-primary/20 text-center">
            <p className="text-primary/40 text-[10px] uppercase mb-3">
              ¿No tienes credenciales?
            </p>
            <button 
              onClick={onGoSignup}
              className="text-primary text-xs uppercase hover:text-white transition-colors underline underline-offset-4 font-bold"
            >
              CREAR_NUEVO_REGISTRO
            </button>
          </div>
        </Card>
      </div>
    </div>
  );
};

export default LoginPage;
