/**
 * Admin Users Management Page
 * Allows Admins to view, create, edit, and delete users.
 * Cyberpunk terminal aesthetics with full CRUD functionality.
 */

import React, { useState, useEffect, useCallback } from 'react';
import { AdminLayout } from '@shared/components/layout';
import { Button } from '@shared/components/ui';
import { useAuth } from '@core/context';
import { adminUserService } from '@core/services/api';
import type { AdminUser, CreateUserRequest, UpdateUserRequest } from '@core/types';
import { AdminUserRole, AdminUserRoleLabels, AdminUserRoleColors, Screen } from '@core/types';

interface AdminUsersPageProps {
  /** Handler for navigating to other screens */
  onNavigate: (screen: Screen) => void;
  /** Handler for returning to gallery */
  onBack: () => void;
}

/**
 * Admin Users Page Component
 * Provides UI for managing users (Admin only)
 * - View all users with filtering
 * - Create new users
 * - Edit existing users (role, status, password)
 * - Delete (soft delete) users
 */
export const AdminUsersPage: React.FC<AdminUsersPageProps> = ({ onNavigate, onBack }) => {
  const { user: currentUser } = useAuth();
  
  // Data state
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [selectedUser, setSelectedUser] = useState<AdminUser | null>(null);
  const [includeInactive, setIncludeInactive] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  
  // UI state
  const [isLoading, setIsLoading] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [showEditForm, setShowEditForm] = useState(false);
  
  // Form state for creating user
  const [createForm, setCreateForm] = useState<CreateUserRequest>({
    email: '',
    password: '',
    displayName: '',
    role: AdminUserRole.Player,
    isActive: true,
  });
  
  // Form state for editing user
  const [editForm, setEditForm] = useState<UpdateUserRequest>({});
  
  // Terminal logs
  const [logs, setLogs] = useState([
    '> User management system online...',
    '> [SUCCESS] Admin protocols established.',
    '> Awaiting commands...'
  ]);

  /**
   * Adds a log entry to the terminal display.
   */
  const addLog = useCallback((msg: string) => {
    setLogs(prev => [...prev, `> ${msg}`].slice(-12));
  }, []);

  /**
   * Fetches all users from the API.
   */
  const fetchUsers = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = await adminUserService.getAll(includeInactive);
      setUsers(data);
      addLog(`[SUCCESS] ${data.length} usuarios cargados`);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Error al cargar usuarios';
      addLog(`ERROR: ${message}`);
    } finally {
      setIsLoading(false);
    }
  }, [includeInactive, addLog]);

  // Load users on mount and when filter changes
  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  /**
   * Filters users based on search term.
   */
  const filteredUsers = users.filter(u => {
    if (!searchTerm.trim()) return true;
    const term = searchTerm.toLowerCase();
    return (
      u.email.toLowerCase().includes(term) ||
      (u.displayName?.toLowerCase().includes(term) ?? false)
    );
  });

  /**
   * Validates the create form.
   */
  const validateCreateForm = (): boolean => {
    if (!createForm.email.trim()) {
      addLog('ERROR: EMAIL REQUERIDO');
      return false;
    }
    
    // Basic email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(createForm.email.trim())) {
      addLog('ERROR: EMAIL INVALIDO');
      return false;
    }
    
    if (!createForm.password || createForm.password.length < 6) {
      addLog('ERROR: PASSWORD DEBE TENER AL MENOS 6 CARACTERES');
      return false;
    }
    
    return true;
  };

  /**
   * Handles creating a new user.
   */
  const handleCreate = async () => {
    if (!validateCreateForm()) return;

    setIsCreating(true);
    addLog('CREANDO USUARIO...');

    try {
      const request: CreateUserRequest = {
        email: createForm.email.trim(),
        password: createForm.password!,
        displayName: createForm.displayName?.trim() || undefined,
        role: createForm.role,
        isActive: createForm.isActive,
      };

      const newUser = await adminUserService.create(request);
      
      setUsers(prev => [...prev, newUser]);
      addLog(`[SUCCESS] Usuario creado: ${newUser.email.toUpperCase()}`);
      
      // Reset form
      setCreateForm({
        email: '',
        password: '',
        displayName: '',
role: AdminUserRole.Player,
        isActive: true,
      });
      setShowCreateForm(false);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'ERROR AL CREAR USUARIO';
      addLog(`ERROR_CRITICO: ${message}`);
    } finally {
      setIsCreating(false);
    }
  };

  /**
   * Opens the edit form for a user.
   */
  const openEditForm = (user: AdminUser) => {
    setSelectedUser(user);
    setEditForm({
      email: user.email,
      displayName: user.displayName || '',
      role: user.role,
      isActive: user.isActive,
    });
    setShowEditForm(true);
  };

  /**
   * Handles updating a user.
   */
  const handleUpdate = async () => {
    if (!selectedUser) return;

    setIsUpdating(true);
    addLog(`ACTUALIZANDO ${selectedUser.email.toUpperCase()}...`);

    try {
      const request: UpdateUserRequest = {};
      
      // Only include changed fields
      if (editForm.email && editForm.email !== selectedUser.email) {
        request.email = editForm.email;
      }
      if (editForm.password) {
        request.password = editForm.password;
      }
      if (editForm.displayName !== undefined && editForm.displayName !== selectedUser.displayName) {
        request.displayName = editForm.displayName || undefined;
      }
      if (editForm.role !== undefined && editForm.role !== selectedUser.role) {
        request.role = editForm.role;
      }
      if (editForm.isActive !== undefined && editForm.isActive !== selectedUser.isActive) {
        request.isActive = editForm.isActive;
      }

      const updatedUser = await adminUserService.update(selectedUser.id, request);
      
      setUsers(prev => prev.map(u => u.id === selectedUser.id ? updatedUser : u));
      setSelectedUser(updatedUser);
      addLog(`[SUCCESS] Usuario actualizado: ${updatedUser.email.toUpperCase()}`);
      
      setShowEditForm(false);
      setEditForm({});
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'ERROR AL ACTUALIZAR';
      addLog(`ERROR: ${message}`);
    } finally {
      setIsUpdating(false);
    }
  };

  /**
   * Handles deleting (soft delete) a user.
   */
  const handleDelete = async (user: AdminUser) => {
    // Prevent deleting yourself
    if (user.id === currentUser?.id) {
      addLog('ERROR: NO PUEDES ELIMINAR TU PROPIO USUARIO');
      return;
    }

    if (!confirm(`¿Confirmar eliminación del usuario ${user.email}? Esta acción desactivará la cuenta.`)) {
      return;
    }

    setIsDeleting(true);
    addLog(`ELIMINANDO ${user.email.toUpperCase()}...`);

    try {
      await adminUserService.delete(user.id);
      
      // Remove from list or mark as inactive
      if (includeInactive) {
        // Refresh to get updated status
        await fetchUsers();
      } else {
        setUsers(prev => prev.filter(u => u.id !== user.id));
      }
      
      if (selectedUser?.id === user.id) {
        setSelectedUser(null);
      }
      
      addLog(`[SUCCESS] Usuario eliminado: ${user.email.toUpperCase()}`);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'ERROR AL ELIMINAR';
      addLog(`ERROR: ${message}`);
    } finally {
      setIsDeleting(false);
    }
  };

  /**
   * Formats a date string for display.
   */
  const formatDate = (dateStr: string | null): string => {
    if (!dateStr) return 'Nunca';
    return new Date(dateStr).toLocaleDateString('es-ES', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  // Check if user is Admin
  const isAdmin = currentUser?.role === 'ADMIN';
  
  if (!isAdmin) {
    return (
      <AdminLayout 
        activeScreen={Screen.ADMIN_USERS} 
        onNavigate={onNavigate} 
        onBack={onBack}
      >
        <div className="flex flex-col items-center justify-center h-full text-danger/60">
          <span className="material-icons text-6xl mb-4">lock</span>
          <p className="text-sm uppercase tracking-widest">Acceso restringido a Administradores</p>
          <Button onClick={onBack} className="mt-4">VOLVER</Button>
        </div>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout 
      activeScreen={Screen.ADMIN_USERS} 
      onNavigate={onNavigate} 
      onBack={onBack}
    >
      <div className="flex flex-col lg:flex-row h-full gap-6">
        {/* Left Column - Users List */}
        <div className="w-full lg:w-1/2 flex flex-col gap-4 overflow-hidden">
          {/* Header & Actions */}
          <div className="border border-primary/30 bg-black/60 p-4">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h1 className="text-xl font-display text-primary uppercase tracking-widest">
                  Usuarios
                </h1>
                <p className="text-primary/40 text-xs mt-1">
                  {filteredUsers.length} de {users.length} usuarios
                </p>
              </div>
              <div className="flex items-center gap-2">
                <Button 
                  onClick={() => {
                    setShowCreateForm(!showCreateForm);
                    setShowEditForm(false);
                  }}
                  variant={showCreateForm ? 'secondary' : 'primary'}
                  size="sm"
                >
                  {showCreateForm ? 'CANCELAR' : '+ NUEVO'}
                </Button>
                <button 
                  onClick={onBack}
                  className="material-icons text-primary/60 hover:text-primary transition-colors"
                  aria-label="Volver"
                >
                  arrow_back
                </button>
              </div>
            </div>
            
            {/* Search & Filter */}
            <div className="flex gap-2">
              <div className="relative flex-1">
                <span className="absolute left-3 top-1/2 -translate-y-1/2 material-icons text-primary/40 text-sm">search</span>
                <input
                  type="text"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  placeholder="Buscar por email o nombre..."
                  className="w-full bg-black/40 border border-primary/30 pl-10 pr-3 py-2 text-sm text-primary placeholder-primary/30 focus:border-primary focus:outline-none"
                />
              </div>
              <label className="flex items-center gap-2 text-xs text-primary/60 whitespace-nowrap cursor-pointer">
                <input
                  type="checkbox"
                  checked={includeInactive}
                  onChange={(e) => setIncludeInactive(e.target.checked)}
                  className="accent-primary"
                />
                Inactivos
              </label>
            </div>
          </div>

          {/* Create Form */}
          {showCreateForm && (
            <div className="border border-cyan-500/30 bg-black/60 p-4">
              <h2 className="text-xs text-cyan-500/60 uppercase tracking-widest mb-4 flex items-center gap-2">
                <span className="material-icons text-xs">person_add</span>
                Crear Nuevo Usuario
              </h2>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {/* Email */}
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">Email *</label>
                  <input
                    type="email"
                    value={createForm.email}
                    onChange={(e) => setCreateForm(prev => ({ ...prev, email: e.target.value }))}
                    placeholder="usuario@ejemplo.com"
                    className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none placeholder:text-primary/20"
                    disabled={isCreating}
                  />
                </div>

                {/* Password */}
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">Password *</label>
                  <input
                    type="password"
                    value={createForm.password}
                    onChange={(e) => setCreateForm(prev => ({ ...prev, password: e.target.value }))}
                    placeholder="••••••••"
                    className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-cyan-500 focus:outline-none placeholder:text-primary/20"
                    disabled={isCreating}
                  />
                </div>

                {/* Display Name */}
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">Nombre</label>
                  <input
                    type="text"
                    value={createForm.displayName || ''}
                    onChange={(e) => setCreateForm(prev => ({ ...prev, displayName: e.target.value }))}
                    placeholder="Nombre de usuario"
                    className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-primary focus:outline-none placeholder:text-primary/20"
                    disabled={isCreating}
                  />
                </div>

                {/* Role */}
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">Rol</label>
                  <select
                    value={createForm.role}
                    onChange={(e) => setCreateForm(prev => ({ ...prev, role: Number(e.target.value) as AdminUserRole }))}
                    className="w-full bg-black/40 border border-primary/30 text-primary p-2 text-sm focus:border-primary focus:outline-none"
                    disabled={isCreating}
                  >
                    <option value={AdminUserRole.Player}>{AdminUserRoleLabels[AdminUserRole.Player]}</option>
                    <option value={AdminUserRole.Master}>{AdminUserRoleLabels[AdminUserRole.Master]}</option>
                    <option value={AdminUserRole.Admin}>{AdminUserRoleLabels[AdminUserRole.Admin]}</option>
                  </select>
                </div>
              </div>

              {/* Active checkbox & Create button */}
              <div className="mt-4 flex items-center justify-between">
                <label className="flex items-center gap-2 text-xs text-primary/60 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={createForm.isActive}
                    onChange={(e) => setCreateForm(prev => ({ ...prev, isActive: e.target.checked }))}
                    className="accent-primary"
                    disabled={isCreating}
                  />
                  Usuario Activo
                </label>
                <Button onClick={handleCreate} disabled={isCreating} size="sm">
                  {isCreating ? 'CREANDO...' : 'CREAR USUARIO'}
                </Button>
              </div>
            </div>
          )}

          {/* Users List */}
          <div className="border border-primary/30 bg-black/60 flex-1 flex flex-col overflow-hidden">
            <div className="bg-primary/10 p-3 text-xs text-primary uppercase tracking-widest flex items-center justify-between">
              <span className="flex items-center gap-2">
                <span className="material-icons text-sm">people</span>
                Lista de Usuarios
              </span>
              <button 
                onClick={fetchUsers}
                disabled={isLoading}
                className="material-icons text-sm text-primary/60 hover:text-primary transition-colors disabled:opacity-50"
                title="Recargar"
              >
                refresh
              </button>
            </div>
            
            <div className="flex-1 overflow-y-auto p-2">
              {isLoading ? (
                <div className="flex items-center justify-center h-full text-primary/40">
                  <span className="animate-pulse">CARGANDO USUARIOS...</span>
                </div>
              ) : filteredUsers.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-full text-primary/40">
                  <span className="material-icons text-4xl mb-2">person_off</span>
                  <p className="text-sm uppercase">No se encontraron usuarios</p>
                </div>
              ) : (
                <div className="space-y-2">
                  {filteredUsers.map((user) => (
                    <div
                      key={user.id}
                      onClick={() => {
                        setSelectedUser(user);
                        setShowEditForm(false);
                      }}
                      className={`border p-3 cursor-pointer transition-all ${
                        selectedUser?.id === user.id
                          ? 'border-cyan-500 bg-cyan-500/10'
                          : 'border-primary/20 bg-black/40 hover:border-primary/40'
                      }`}
                    >
                      <div className="flex items-center justify-between">
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2 flex-wrap">
                            <span className="text-primary font-bold text-sm truncate">
                              {user.displayName || user.email}
                            </span>
                            <span className={`text-[10px] px-1.5 py-0.5 border ${AdminUserRoleColors[user.role]}`}>
                              {AdminUserRoleLabels[user.role]}
                            </span>
                            {!user.isActive && (
                              <span className="text-[10px] px-1.5 py-0.5 border border-gray-500/40 text-gray-400 bg-gray-500/10">
                                INACTIVO
                              </span>
                            )}
                          </div>
                          <p className="text-xs text-primary/40 mt-1 truncate">{user.email}</p>
                        </div>
                        
                        <div className="flex items-center gap-1 ml-2">
                          <button
                            onClick={(e) => {
                              e.stopPropagation();
                              openEditForm(user);
                            }}
                            className="material-icons text-sm text-primary/40 hover:text-cyan-500 transition-colors p-1"
                            title="Editar"
                          >
                            edit
                          </button>
                          <button
                            onClick={(e) => {
                              e.stopPropagation();
                              handleDelete(user);
                            }}
                            disabled={isDeleting || user.id === currentUser?.id}
                            className="material-icons text-sm text-primary/40 hover:text-danger transition-colors p-1 disabled:opacity-30 disabled:cursor-not-allowed"
                            title={user.id === currentUser?.id ? 'No puedes eliminarte' : 'Eliminar'}
                          >
                            delete
                          </button>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Right Column - User Details / Edit Form */}
        <div className="flex-1 flex flex-col gap-4 overflow-hidden">
          {showEditForm && selectedUser ? (
            /* Edit Form */
            <div className="border border-yellow-500/30 bg-black/60 p-6 flex-1">
              <h2 className="text-sm text-yellow-500/60 uppercase tracking-widest mb-4 flex items-center gap-2">
                <span className="material-icons text-xs">edit</span>
                Editar Usuario
              </h2>
              
              <div className="space-y-4">
                {/* Email */}
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">Email</label>
                  <input
                    type="email"
                    value={editForm.email || ''}
                    onChange={(e) => setEditForm(prev => ({ ...prev, email: e.target.value }))}
                    className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-yellow-500 focus:outline-none"
                    disabled={isUpdating}
                  />
                </div>

                {/* Display Name */}
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">Nombre</label>
                  <input
                    type="text"
                    value={editForm.displayName || ''}
                    onChange={(e) => setEditForm(prev => ({ ...prev, displayName: e.target.value }))}
                    className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-yellow-500 focus:outline-none"
                    disabled={isUpdating}
                  />
                </div>

                {/* New Password */}
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">
                    Nueva Contraseña <span className="text-primary/20">(dejar vacío para mantener)</span>
                  </label>
                  <input
                    type="password"
                    value={editForm.password || ''}
                    onChange={(e) => setEditForm(prev => ({ ...prev, password: e.target.value }))}
                    placeholder="••••••••"
                    className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-yellow-500 focus:outline-none placeholder:text-primary/20"
                    disabled={isUpdating}
                  />
                </div>

                {/* Role */}
                <div>
                  <label className="block text-xs text-primary/40 uppercase mb-1">Rol</label>
                  <select
                    value={editForm.role ?? selectedUser.role}
                    onChange={(e) => setEditForm(prev => ({ ...prev, role: Number(e.target.value) as AdminUserRole }))}
                    className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-yellow-500 focus:outline-none"
                    disabled={isUpdating || selectedUser.id === currentUser?.id}
                  >
                    <option value={AdminUserRole.Player}>{AdminUserRoleLabels[AdminUserRole.Player]}</option>
                    <option value={AdminUserRole.Master}>{AdminUserRoleLabels[AdminUserRole.Master]}</option>
                    <option value={AdminUserRole.Admin}>{AdminUserRoleLabels[AdminUserRole.Admin]}</option>
                  </select>
                  {selectedUser.id === currentUser?.id && (
                    <p className="text-xs text-yellow-500/60 mt-1">No puedes cambiar tu propio rol</p>
                  )}
                </div>

                {/* Active */}
                <label className="flex items-center gap-2 text-sm text-primary/60 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={editForm.isActive ?? selectedUser.isActive}
                    onChange={(e) => setEditForm(prev => ({ ...prev, isActive: e.target.checked }))}
                    className="accent-primary w-4 h-4"
                    disabled={isUpdating || selectedUser.id === currentUser?.id}
                  />
                  Usuario Activo
                  {selectedUser.id === currentUser?.id && (
                    <span className="text-xs text-yellow-500/60">(no puedes desactivarte)</span>
                  )}
                </label>
              </div>

              {/* Action Buttons */}
              <div className="mt-6 flex gap-2">
                <Button 
                  onClick={() => setShowEditForm(false)} 
                  variant="secondary"
                  className="flex-1"
                >
                  CANCELAR
                </Button>
                <Button 
                  onClick={handleUpdate} 
                  disabled={isUpdating}
                  className="flex-1"
                >
                  {isUpdating ? 'GUARDANDO...' : 'GUARDAR CAMBIOS'}
                </Button>
              </div>
            </div>
          ) : selectedUser ? (
            /* User Details */
            <div className="border border-primary/30 bg-black/60 flex-1 flex flex-col overflow-hidden">
              <div className="bg-primary/10 p-4 text-xs text-primary uppercase tracking-widest flex items-center justify-between">
                <span className="flex items-center gap-2">
                  <span className="material-icons text-sm">person</span>
                  Detalles del Usuario
                </span>
                <button 
                  onClick={() => openEditForm(selectedUser)}
                  className="material-icons text-sm text-primary/60 hover:text-cyan-500 transition-colors"
                  title="Editar"
                >
                  edit
                </button>
              </div>
              
              <div className="flex-1 p-6 overflow-y-auto space-y-4">
                {/* Avatar & Name */}
                <div className="flex items-center gap-4 pb-4 border-b border-primary/20">
                  <div className="w-16 h-16 rounded-full bg-primary/10 border border-primary/30 flex items-center justify-center">
                    {selectedUser.avatarUrl ? (
                      <img src={selectedUser.avatarUrl} alt="" className="w-full h-full rounded-full object-cover" />
                    ) : (
                      <span className="material-icons text-3xl text-primary/40">person</span>
                    )}
                  </div>
                  <div>
                    <h3 className="text-xl text-primary font-bold">
                      {selectedUser.displayName || 'Sin nombre'}
                    </h3>
                    <p className="text-sm text-primary/60">{selectedUser.email}</p>
                    <div className="flex gap-2 mt-2">
                      <span className={`text-xs px-2 py-0.5 border ${AdminUserRoleColors[selectedUser.role]}`}>
                        {AdminUserRoleLabels[selectedUser.role]}
                      </span>
                      <span className={`text-xs px-2 py-0.5 border ${
                        selectedUser.isActive 
                          ? 'border-green-500/40 text-green-400 bg-green-500/10' 
                          : 'border-gray-500/40 text-gray-400 bg-gray-500/10'
                      }`}>
                        {selectedUser.isActive ? 'ACTIVO' : 'INACTIVO'}
                      </span>
                    </div>
                  </div>
                </div>

                {/* Stats Grid */}
                <div className="grid grid-cols-2 gap-3">
                  <div className="bg-black/40 border border-primary/20 p-3">
                    <p className="text-[10px] text-primary/40 uppercase">Campañas Propias</p>
                    <p className="text-2xl text-primary font-bold">{selectedUser.ownedCampaignsCount}</p>
                  </div>
                  <div className="bg-black/40 border border-primary/20 p-3">
                    <p className="text-[10px] text-primary/40 uppercase">Membresías</p>
                    <p className="text-2xl text-primary font-bold">{selectedUser.campaignMembershipsCount}</p>
                  </div>
                </div>

                {/* Timestamps */}
                <div className="space-y-2 text-sm">
                  <div className="flex justify-between">
                    <span className="text-primary/40">Último acceso:</span>
                    <span className="text-primary">{formatDate(selectedUser.lastLoginAt)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-primary/40">Creado:</span>
                    <span className="text-primary">{formatDate(selectedUser.createdAt)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-primary/40">Actualizado:</span>
                    <span className="text-primary">{formatDate(selectedUser.updatedAt)}</span>
                  </div>
                </div>

                {/* ID */}
                <div className="pt-4 border-t border-primary/20">
                  <p className="text-[10px] text-primary/30 uppercase">User ID</p>
                  <p className="text-xs text-primary/60 font-mono break-all">{selectedUser.id}</p>
                </div>
              </div>
            </div>
          ) : (
            /* No Selection */
            <div className="border border-primary/30 bg-black/60 flex-1 flex flex-col items-center justify-center text-primary/40">
              <span className="material-icons text-6xl mb-4">person_search</span>
              <p className="text-sm uppercase">Selecciona un usuario</p>
              <p className="text-xs mt-1">para ver sus detalles</p>
            </div>
          )}

          {/* Terminal Log */}
          <div className="border border-primary/30 bg-black/80 h-48 flex flex-col">
            <div className="bg-primary/20 p-2 text-xs text-primary uppercase tracking-widest flex items-center gap-2">
              <span className="material-icons text-sm">terminal</span>
              System Log
            </div>
            <div className="flex-1 p-4 font-mono text-xs text-primary/70 space-y-1 overflow-y-auto">
              {logs.map((log, i) => (
                <p 
                  key={i} 
                  className={`${
                    log.includes('ERROR') ? 'text-danger' : 
                    log.includes('SUCCESS') ? 'text-green-400' : ''
                  }`}
                >
                  {log}
                </p>
              ))}
              <p className="animate-pulse">_</p>
            </div>
          </div>
        </div>
      </div>
    </AdminLayout>
  );
};
