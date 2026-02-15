/**
 * Login Page
 * Handles user authentication with cyberpunk terminal aesthetics
 * Uses homepage panel style for consistency
 */

import React, { useState } from 'react';
import { GridBackground } from '@shared/components/layout';
import { Input, Button } from '@shared/components/ui';
import { ErrorMessage } from '@shared/components/feedback';
import { validateEmail, validatePassword } from '@core/utils';
import { useAuth } from '@core/context/AuthContext';

interface LoginPageProps {
  onLoginSuccess: () => void;
  onGoSignup: () => void;
  /** Optional callback to navigate back to home page */
  onBack?: () => void;
}

export const LoginPage: React.FC<LoginPageProps> = ({ onLoginSuccess, onGoSignup, onBack }) => {
  const { login, error: authError, isLoading: authLoading, clearError } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    clearError();
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

    try {
      // Use AuthContext login to update global state
      await login({ email, password });
      onLoginSuccess();
    } catch {
      // Error is handled by AuthContext and will be displayed via authError
    }
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-background-dark relative p-6">
      <GridBackground opacity={0.1} size={40} />
      
      <div className="relative z-10 w-full max-w-[450px]">
        {/* Panel with homepage style */}
        <div className="bg-surface-dark/40 border border-primary/10 p-8 flex flex-col gap-6 group hover:border-primary/40 transition-all clip-tech-br backdrop-blur-sm shadow-[0_0_30px_rgba(37,244,106,0.1)] font-mono">
          
          {/* Back to Home Button */}
          {onBack && (
            <button 
              onClick={onBack}
              className="flex items-center gap-1 text-primary/50 hover:text-primary text-[10px] uppercase transition-colors self-start"
            >
              <span className="material-icons text-sm">arrow_back</span>
              VOLVER_AL_INICIO
            </button>
          )}
          
          {/* Header with icon */}
          <div className="flex items-center gap-4">
            <div className="w-14 h-14 border border-primary/20 flex items-center justify-center bg-primary/5 group-hover:bg-primary/20 transition-all shrink-0">
              <span className="material-icons text-3xl text-primary leading-none">login</span>
            </div>
            <div>
              <h1 className="text-2xl font-display font-bold uppercase tracking-widest text-glow text-primary">
                HefestAI
              </h1>
              <p className="text-primary/50 text-[10px] tracking-widest uppercase">
                Kernel v3.0 // Authentication
              </p>
            </div>
          </div>

          {/* Form */}
          <form onSubmit={handleLogin} className="space-y-5">
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

            {authError && <ErrorMessage message={authError} />}

            <Button
              type="submit"
              variant="primary"
              fullWidth
              size="lg"
              isLoading={authLoading}
              icon="terminal"
            >
              ACCEDER_AL_NUCLEO
            </Button>
          </form>

          {/* Signup Link */}
          <div className="pt-4 border-t border-primary/10 text-center">
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

          {/* Decorative dots */}
          <div className="flex gap-1 justify-center pt-2">
            {[...Array(5)].map((_, i) => (
              <div 
                key={i} 
                className="w-1.5 h-1.5 bg-primary/30 group-hover:bg-primary/50 transition-all" 
                style={{ transitionDelay: `${i * 50}ms` }}
              />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
