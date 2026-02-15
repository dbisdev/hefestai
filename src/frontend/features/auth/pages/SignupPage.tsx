/**
 * Signup Page
 * Handles new user registration with role selection
 * Uses homepage panel style for consistency
 */

import React, { useState } from 'react';
import { GridBackground } from '@shared/components/layout';
import { Input, Button } from '@shared/components/ui';
import { ErrorMessage } from '@shared/components/feedback';
import { validateEmail, validatePassword, validateDisplayName, validateInviteCode } from '@core/utils';
import { useAuth } from '@core/context/AuthContext';
import type { UserRole } from '@core/types';

interface SignupPageProps {
  onSignupSuccess: () => void;
  onBack: () => void;
}

export const SignupPage: React.FC<SignupPageProps> = ({ onSignupSuccess, onBack }) => {
  const { register } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [role, setRole] = useState<UserRole>('PLAYER');
  const [inviteCode, setInviteCode] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setValidationErrors({});

    // Client-side validation (OWASP A03 - Input Validation)
    const emailValidation = validateEmail(email);
    const passwordValidation = validatePassword(password);
    const displayNameValidation = validateDisplayName(displayName);

    const errors: Record<string, string> = {};
    if (!emailValidation.isValid) errors.email = emailValidation.errors[0];
    if (!passwordValidation.isValid) errors.password = passwordValidation.errors[0];
    if (!displayNameValidation.isValid) errors.displayName = displayNameValidation.errors[0];

    // Validate invite code for players
    if (role === 'PLAYER' && inviteCode) {
      const inviteValidation = validateInviteCode(inviteCode);
      if (!inviteValidation.isValid) errors.inviteCode = inviteValidation.errors[0];
    }

    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);
      return;
    }

    setLoading(true);
    try {
      // Use AuthContext register to update global state
      await register({
        email,
        password,
        displayName,
        role,
        inviteCode: role === 'PLAYER' && inviteCode ? inviteCode : undefined,
      });
      onSignupSuccess();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Error de registro';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-background-dark relative p-6">
      <GridBackground opacity={0.1} size={40} />
      
      <div className="relative z-10 w-full max-w-[450px]">
        {/* Panel with homepage style */}
        <div className="bg-surface-dark/40 border border-primary/10 p-8 flex flex-col gap-6 group hover:border-primary/40 transition-all clip-tech-br backdrop-blur-sm shadow-[0_0_30px_rgba(37,244,106,0.1)] font-mono">
          
          {/* Header with icon */}
          <div className="flex items-center gap-4">
            <div className="w-14 h-14 border border-primary/20 flex items-center justify-center bg-primary/5 group-hover:bg-primary/20 transition-all shrink-0">
              <span className="material-icons text-3xl text-primary leading-none">person_add</span>
            </div>
            <div>
              <h1 className="text-2xl font-display font-bold uppercase tracking-widest text-glow text-primary">
                Nuevo Registro
              </h1>
              <p className="text-primary/50 text-[10px] tracking-widest uppercase">
                Omega_Central // Onboarding
              </p>
            </div>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit} className="space-y-5">

            {/* Role Selection */}
            <div>
              <label className="block text-primary/70 text-[10px] uppercase mb-2 tracking-wider">
                Clasificación de Perfil
              </label>
              <div className="grid grid-cols-2 gap-2">
                <button 
                  type="button"
                  onClick={() => setRole('PLAYER')}
                  className={`py-3 text-[10px] border uppercase font-bold transition-all ${
                    role === 'PLAYER' 
                      ? 'bg-primary text-black border-primary' 
                      : 'text-primary/60 border-primary/20 hover:border-primary hover:bg-primary/5'
                  }`}
                >
                  Jugador (Player)
                </button>
                <button 
                  type="button"
                  onClick={() => setRole('MASTER')}
                  className={`py-3 text-[10px] border uppercase font-bold transition-all ${
                    role === 'MASTER' 
                      ? 'bg-primary text-black border-primary' 
                      : 'text-primary/60 border-primary/20 hover:border-primary hover:bg-primary/5'
                  }`}
                >
                  Maestro (Master)
                </button>
              </div>
            </div>

            <Input
              label="Correo Electrónico"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="operative@omega.sys"
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
              autoComplete="new-password"
            />

            <Input
              label="Nombre de Operativo"
              type="text"
              required
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              placeholder="NEW_OPERATIVE"
              error={validationErrors.displayName}
              className="uppercase"
              autoComplete="username"
            />

            {/* Invite Code (only for Players) */}
            {role === 'PLAYER' && (
              <div className="space-y-3 animate-glitch-in">
                <Input
                  label="Código de Invitación del Maestro"
                  type="text"
                  required
                  value={inviteCode}
                  onChange={(e) => setInviteCode(e.target.value)}
                  placeholder="INV-CODE-XXX"
                  error={validationErrors.inviteCode}
                  className="uppercase font-bold"
                />
                <p className="text-primary/40 text-[8px] uppercase leading-relaxed">
                  Solicita el código a tu Game Master para unirte a su partida
                </p>
              </div>
            )}

            {error && <ErrorMessage message={error} />}

            <Button
              type="submit"
              variant="primary"
              fullWidth
              size="lg"
              isLoading={loading}
              icon="rocket_launch"
            >
              INICIALIZAR_PERFIL
            </Button>
          </form>

          {/* Back Button */}
          <div className="pt-4 border-t border-primary/10 text-center">
            <button 
              onClick={onBack}
              className="text-primary/50 text-[10px] uppercase hover:text-primary transition-colors font-bold flex items-center gap-1 justify-center w-full"
            >
              <span className="material-icons text-sm">arrow_back</span>
              VOLVER_AL_ACCESO
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

export default SignupPage;
