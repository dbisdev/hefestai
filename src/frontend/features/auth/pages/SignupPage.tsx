/**
 * Signup Page
 * Handles new user registration with role selection
 * Uses homepage panel style for consistency
 */

import React, { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { GridBackground } from '@shared/components/layout';
import { Input, Button } from '@shared/components/ui';
import { ErrorMessage } from '@shared/components/feedback';
import { validateEmail, validatePassword, validateDisplayName, validateInviteCode } from '@core/utils';
import { useAuth } from '@core/context/AuthContext';
import type { UserRole } from '@core/types';

export const SignupPage: React.FC = () => {
  const navigate = useNavigate();
  const { register, clearError } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [displayName, setDisplayName] = useState('');
  const [role, setRole] = useState<UserRole>('PLAYER');
  const [inviteCode, setInviteCode] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  const handleSubmit = useCallback(async (e: React.FormEvent) => {
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

    // Validate password match
    if (password !== confirmPassword) {
      errors.confirmPassword = 'Las contraseñas no coinciden';
    }

    // Validate invite code for players (optional - only if provided)
    if (role === 'PLAYER' && inviteCode && inviteCode.trim()) {
      const inviteValidation = validateInviteCode(inviteCode);
      if (!inviteValidation.isValid) errors.inviteCode = inviteValidation.errors[0];
    }

    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);
      return;
    }

    setLoading(true);
    try {
      await register({
        email,
        password,
        displayName,
        role,
        inviteCode: role === 'PLAYER' && inviteCode ? inviteCode : undefined,
      });
      // Navigation is handled by AnimatedRoutes based on auth state
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Error de registro';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, [register, email, password, confirmPassword, displayName, role, inviteCode]);

  const handleBack = useCallback(() => {
    navigate('/');
  }, [navigate]);

  const handleGoLogin = useCallback(() => {
    setError('');
    setValidationErrors({});
    navigate('/login');
  }, [navigate]);

  return (
    <div className="flex items-center justify-center min-h-screen bg-background-dark relative p-6 overflow-y-auto">
      <GridBackground opacity={0.1} size={40} />
      
      <div className="relative z-10 w-full max-w-[450px]">
        {/* Panel with homepage style */}
        <div className="bg-surface-dark/40 border border-primary/10 p-8 flex flex-col gap-6 group hover:border-primary/40 transition-all clip-tech-br backdrop-blur-sm shadow-[0_0_30px_rgba(37,244,106,0.1)] font-mono">
          
          {/* Back to Home Button */}
          <button 
            onClick={handleBack}
            className="flex items-center gap-1 text-primary/50 hover:text-primary text-[10px] uppercase transition-colors self-start"
          >
            <span className="material-icons text-sm">arrow_back</span>
            VOLVER_AL_INICIO
          </button>

          {/* Header with icon */}
          <div className="flex items-center gap-4">
            <div className="w-14 h-14 border border-primary/20 flex items-center justify-center bg-primary/5 group-hover:bg-primary/20 transition-all shrink-0">
              <span className="material-icons text-3xl text-primary leading-none">person_add</span>
            </div>
            <div>
              <h1 className="text-2xl font-display font-bold uppercase tracking-widest text-glow text-primary">
                HefestAI
              </h1>
              <p className="text-primary/50 text-[10px] tracking-widest uppercase">
                Kernel v3.0 // Onboarding
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

            <div className="relative">
              <Input
                label="Contraseña"
                type={showPassword ? 'text' : 'password'}
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                error={validationErrors.password}
                autoComplete="new-password"
                className="pr-10"
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3 top-[26px] text-primary/50 hover:text-primary transition-colors p-0.5"
                aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
              >
                <span className="material-icons text-sm">
                  {showPassword ? 'visibility_off' : 'visibility'}
                </span>
              </button>
            </div>

            <div className="relative">
              <Input
                label="Confirmar Contraseña"
                type={showConfirmPassword ? 'text' : 'password'}
                required
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="••••••••"
                error={validationErrors.confirmPassword}
                autoComplete="new-password"
                className="pr-10"
              />
              <button
                type="button"
                onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                className="absolute right-3 top-[26px] text-primary/50 hover:text-primary transition-colors p-0.5"
                aria-label={showConfirmPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
              >
                <span className="material-icons text-sm">
                  {showConfirmPassword ? 'visibility_off' : 'visibility'}
                </span>
              </button>
            </div>

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

            {/* Invite Code (optional - for joining a campaign during registration) */}
            {role === 'PLAYER' && (
              <div className="space-y-3 animate-glitch-in">
                <Input
                  label="Código de Campaña (Opcional)"
                  type="text"
                  value={inviteCode}
                  onChange={(e) => setInviteCode(e.target.value)}
                  placeholder="CÓDIGO-CAMPAÑA"
                  error={validationErrors.inviteCode}
                  className="uppercase font-bold"
                />
                <p className="text-primary/40 text-[8px] uppercase leading-relaxed">
                  Ingresa el código de campaña para unirte directamente, o déjalo vacío para unirte más tarde
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
              onClick={handleGoLogin}
              className="text-primary/50 text-[10px] uppercase hover:text-primary transition-colors font-bold flex items-center gap-1 justify-center w-full"
            >
              <span className="material-icons text-sm">arrow_back</span>
              VOLVER_AL_ACCESO
            </button>
          </div>
          
        </div>
      </div>
    </div>
  );
};

export default SignupPage;
