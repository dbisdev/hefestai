/**
 * Admin Campaigns Management Page
 * Allows Admins to view, edit, and delete campaigns.
 * Supports ownership transfer and status management.
 * Cyberpunk terminal aesthetics with full CRUD functionality.
 */

import React, { useState, useEffect, useCallback } from 'react';
import { AdminLayout } from '@shared/components/layout';
import { Button } from '@shared/components/ui';
import { useTerminalLog } from '@core/hooks/useTerminalLog';
import { adminCampaignService, adminUserService } from '@core/services/api';
import { extractErrorMessage } from '@core/utils/error';
import { formatAdminDate } from '@core/utils/date';
import { TERMINAL_LOG_DEFAULT } from '@core/config/constants';
import type { AdminCampaign, AdminUpdateCampaignInput, AdminUser } from '@core/types';
import {
  AdminGuard,
  AdminTerminalPanel,
  AdminStatusBadge,
  AdminStatusBadgeSm,
  AdminStatCard,
  AdminSearchBar,
  AdminListHeader,
  AdminEmptyState,
  AdminFormActions,
} from '../components';

/**
 * Admin Campaigns Page Component
 * Provides UI for managing campaigns (Admin only)
 * - View all campaigns with filtering
 * - Edit campaign details (name, description, status)
 * - Transfer campaign ownership
 * - Delete (soft delete) campaigns
 */
export const AdminCampaignsPage: React.FC = () => {
  const { logs, addLog } = useTerminalLog({
    maxLogs: TERMINAL_LOG_DEFAULT,
    initialLogs: [
      '> Campaign management system online...',
      '> [SUCCESS] Admin protocols established.',
      '> Awaiting commands...'
    ]
  });
 
  // Data state
  const [campaigns, setCampaigns] = useState<AdminCampaign[]>([]);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [selectedCampaign, setSelectedCampaign] = useState<AdminCampaign | null>(null);
  const [includeInactive, setIncludeInactive] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  
  // UI state
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingUsers, setIsLoadingUsers] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [showEditForm, setShowEditForm] = useState(false);
  const [showTransferForm, setShowTransferForm] = useState(false);
  
  // Form state for editing campaign
  const [editForm, setEditForm] = useState<AdminUpdateCampaignInput>({});
  
  // Form state for ownership transfer
  const [transferOwnerId, setTransferOwnerId] = useState<string>('');

  /**
   * Fetches all campaigns from the API.
   */
  const fetchCampaigns = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = await adminCampaignService.getAll(includeInactive);
      setCampaigns(data);
      addLog(`[SUCCESS] ${data.length} campañas cargadas`);
    } catch (error: unknown) {
      const message = extractErrorMessage(error, 'Error al cargar campañas');
      addLog(`ERROR: ${message}`);
    } finally {
      setIsLoading(false);
    }
  }, [includeInactive, addLog]);

  /**
   * Fetches all users for ownership transfer dropdown.
   */
  const fetchUsers = useCallback(async () => {
    setIsLoadingUsers(true);
    try {
      const data = await adminUserService.getAll(false); // Only active users
      setUsers(data);
    } catch (error: unknown) {
      const message = extractErrorMessage(error, 'Error al cargar usuarios');
      addLog(`ERROR: ${message}`);
    } finally {
      setIsLoadingUsers(false);
    }
  }, [addLog]);

  // Load campaigns on mount and when filter changes
  useEffect(() => {
    fetchCampaigns();
  }, [fetchCampaigns]);

  // Load users when transfer form is opened
  useEffect(() => {
    if (showTransferForm && users.length === 0) {
      fetchUsers();
    }
  }, [showTransferForm, users.length, fetchUsers]);

  /**
   * Filters campaigns based on search term.
   */
  const filteredCampaigns = campaigns.filter(c => {
    if (!searchTerm.trim()) return true;
    const term = searchTerm.toLowerCase();
    return (
      c.name.toLowerCase().includes(term) ||
      c.ownerName.toLowerCase().includes(term) ||
      c.gameSystemName.toLowerCase().includes(term) ||
      c.joinCode.toLowerCase().includes(term)
    );
  });

  /**
   * Opens the edit form for a campaign.
   */
  const openEditForm = (campaign: AdminCampaign) => {
    setSelectedCampaign(campaign);
    setEditForm({
      name: campaign.name,
      description: campaign.description || '',
      isActive: campaign.isActive,
    });
    setShowEditForm(true);
    setShowTransferForm(false);
  };

  /**
   * Opens the ownership transfer form.
   */
  const openTransferForm = (campaign: AdminCampaign) => {
    setSelectedCampaign(campaign);
    setTransferOwnerId('');
    setShowTransferForm(true);
    setShowEditForm(false);
  };

  /**
   * Handles updating a campaign.
   */
  const handleUpdate = async () => {
    if (!selectedCampaign) return;

    setIsUpdating(true);
    addLog(`ACTUALIZANDO ${selectedCampaign.name.toUpperCase()}...`);

    try {
      const request: AdminUpdateCampaignInput = {};
      
      // Only include changed fields
      if (editForm.name && editForm.name !== selectedCampaign.name) {
        request.name = editForm.name;
      }
      if (editForm.description !== undefined && editForm.description !== selectedCampaign.description) {
        request.description = editForm.description || undefined;
      }
      if (editForm.isActive !== undefined && editForm.isActive !== selectedCampaign.isActive) {
        request.isActive = editForm.isActive;
      }

      const updatedCampaign = await adminCampaignService.update(selectedCampaign.id, request);
      
      setCampaigns(prev => prev.map(c => c.id === selectedCampaign.id ? updatedCampaign : c));
      setSelectedCampaign(updatedCampaign);
      addLog(`[SUCCESS] Campaña actualizada: ${updatedCampaign.name.toUpperCase()}`);
      
      setShowEditForm(false);
      setEditForm({});
    } catch (error: unknown) {
      const message = extractErrorMessage(error, 'ERROR AL ACTUALIZAR');
      addLog(`ERROR: ${message}`);
    } finally {
      setIsUpdating(false);
    }
  };

  /**
   * Handles ownership transfer.
   */
  const handleTransfer = async () => {
    if (!selectedCampaign || !transferOwnerId) return;

    const newOwner = users.find(u => u.id === transferOwnerId);
    if (!newOwner) return;

    if (!confirm(`¿Transferir la campaña "${selectedCampaign.name}" a ${newOwner.displayName || newOwner.email}?`)) {
      return;
    }

    setIsUpdating(true);
    addLog(`TRANSFIRIENDO PROPIEDAD A ${(newOwner.displayName || newOwner.email).toUpperCase()}...`);

    try {
      const updatedCampaign = await adminCampaignService.update(selectedCampaign.id, {
        ownerId: transferOwnerId,
      });
      
      setCampaigns(prev => prev.map(c => c.id === selectedCampaign.id ? updatedCampaign : c));
      setSelectedCampaign(updatedCampaign);
      addLog(`[SUCCESS] Propiedad transferida a ${updatedCampaign.ownerName.toUpperCase()}`);
      
      setShowTransferForm(false);
      setTransferOwnerId('');
    } catch (error: unknown) {
      const message = extractErrorMessage(error, 'ERROR AL TRANSFERIR');
      addLog(`ERROR: ${message}`);
    } finally {
      setIsUpdating(false);
    }
  };

  /**
   * Handles deleting (soft delete) a campaign.
   */
  const handleDelete = async (campaign: AdminCampaign) => {
    if (!confirm(`¿Confirmar eliminación de la campaña "${campaign.name}"? Esta acción desactivará la campaña.`)) {
      return;
    }

    setIsDeleting(true);
    addLog(`ELIMINANDO ${campaign.name.toUpperCase()}...`);

    try {
      await adminCampaignService.delete(campaign.id);
      
      // Remove from list or mark as inactive
      if (includeInactive) {
        await fetchCampaigns();
      } else {
        setCampaigns(prev => prev.filter(c => c.id !== campaign.id));
      }
      
      if (selectedCampaign?.id === campaign.id) {
        setSelectedCampaign(null);
        setShowEditForm(false);
        setShowTransferForm(false);
      }
      
      addLog(`[SUCCESS] Campaña eliminada: ${campaign.name.toUpperCase()}`);
    } catch (error: unknown) {
      const message = extractErrorMessage(error, 'ERROR AL ELIMINAR');
      addLog(`ERROR: ${message}`);
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <AdminGuard>
      <AdminLayout>
        <div className="flex flex-col lg:flex-row h-full gap-6">
          {/* Left Column - Campaigns List */}
          <div className="w-full lg:w-1/2 flex flex-col gap-4 overflow-hidden">
            {/* Header & Actions */}
            <div className="border border-primary/30 bg-black/60 p-4">
              <div className="flex items-center justify-between mb-4">
                <div>
                  <h1 className="text-xl font-display text-primary uppercase tracking-widest">
                    Campañas
                  </h1>
                  <p className="text-primary/40 text-xs mt-1">
                    {filteredCampaigns.length} de {campaigns.length} campañas
                  </p>
                </div>
              </div>
              
              <AdminSearchBar
                searchTerm={searchTerm}
                onSearch={setSearchTerm}
                includeInactive={includeInactive}
                onToggleInactive={setIncludeInactive}
                placeholder="Buscar por nombre, dueño, sistema o código..."
                inactiveLabel="Inactivas"
              />
            </div>

            {/* Campaigns List */}
            <div className="border border-primary/30 bg-black/60 flex-1 flex flex-col overflow-hidden">
              <AdminListHeader
                icon="public"
                title="Lista de Campañas"
                onRefresh={fetchCampaigns}
                isLoading={isLoading}
              />
              
              <div className="flex-1 overflow-y-auto p-2">
                {isLoading ? (
                  <div className="flex items-center justify-center h-full text-primary/40">
                    <span className="animate-pulse">CARGANDO CAMPAÑAS...</span>
                  </div>
                ) : filteredCampaigns.length === 0 ? (
                  <div className="flex flex-col items-center justify-center h-full text-primary/40">
                    <span className="material-icons text-4xl mb-2">public_off</span>
                    <p className="text-sm uppercase">No se encontraron campañas</p>
                  </div>
                ) : (
                  <div className="space-y-2">
                    {filteredCampaigns.map((campaign) => (
                      <div
                        key={campaign.id}
                        onClick={() => {
                          setSelectedCampaign(campaign);
                          setShowEditForm(false);
                          setShowTransferForm(false);
                        }}
                        className={`border p-3 cursor-pointer transition-all ${
                          selectedCampaign?.id === campaign.id
                            ? 'border-cyan-500 bg-cyan-500/10'
                            : 'border-primary/20 bg-black/40 hover:border-primary/40'
                        }`}
                      >
                        <div className="flex items-start justify-between">
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center gap-2 flex-wrap">
                              <span className="text-primary font-bold text-sm truncate">
                                {campaign.name}
                              </span>
                              {!campaign.isActive && (
                                <AdminStatusBadgeSm isActive={false} inactiveLabel="INACTIVA" />
                              )}
                            </div>
                            <div className="flex items-center gap-3 mt-1 text-xs text-primary/40">
                              <span className="flex items-center gap-1">
                                <span className="material-icons text-[10px]">person</span>
                                {campaign.ownerName}
                              </span>
                              <span className="flex items-center gap-1">
                                <span className="material-icons text-[10px]">sports_esports</span>
                                {campaign.gameSystemName}
                              </span>
                            </div>
                            <div className="flex items-center gap-3 mt-1 text-[10px] text-primary/30">
                              <span>{campaign.memberCount} miembros</span>
                              <span>{campaign.entityCount} entidades</span>
                            </div>
                          </div>
                          
                          <div className="flex items-center gap-1 ml-2">
                            <button
                              onClick={(e) => {
                                e.stopPropagation();
                                openEditForm(campaign);
                              }}
                              className="material-icons text-sm text-primary/40 hover:text-cyan-500 transition-colors p-1"
                              title="Editar"
                            >
                              edit
                            </button>
                            <button
                              onClick={(e) => {
                                e.stopPropagation();
                                openTransferForm(campaign);
                              }}
                              className="material-icons text-sm text-primary/40 hover:text-yellow-500 transition-colors p-1"
                              title="Transferir propiedad"
                            >
                              swap_horiz
                            </button>
                            <button
                              onClick={(e) => {
                                e.stopPropagation();
                                handleDelete(campaign);
                              }}
                              disabled={isDeleting}
                              className="material-icons text-sm text-primary/40 hover:text-danger transition-colors p-1 disabled:opacity-30"
                              title="Eliminar"
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

          {/* Right Column - Campaign Details / Edit Form / Transfer Form */}
          <div className="flex-1 flex flex-col gap-4 overflow-hidden">
            {showEditForm && selectedCampaign ? (
              /* Edit Form */
              <div className="border border-yellow-500/30 bg-black/60 p-6 flex-1">
                <h2 className="text-sm text-yellow-500/60 uppercase tracking-widest mb-4 flex items-center gap-2">
                  <span className="material-icons text-xs">edit</span>
                  Editar Campaña
                </h2>
                
                <div className="space-y-4">
                  {/* Name */}
                  <div>
                    <label className="block text-xs text-primary/40 uppercase mb-1">Nombre</label>
                    <input
                      type="text"
                      value={editForm.name || ''}
                      onChange={(e) => setEditForm(prev => ({ ...prev, name: e.target.value }))}
                      className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-yellow-500 focus:outline-none"
                      disabled={isUpdating}
                    />
                  </div>

                  {/* Description */}
                  <div>
                    <label className="block text-xs text-primary/40 uppercase mb-1">Descripción</label>
                    <textarea
                      value={editForm.description || ''}
                      onChange={(e) => setEditForm(prev => ({ ...prev, description: e.target.value }))}
                      rows={3}
                      className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-yellow-500 focus:outline-none resize-none"
                      disabled={isUpdating}
                    />
                  </div>

                  {/* Active */}
                  <label className="flex items-center gap-2 text-sm text-primary/60 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={editForm.isActive ?? selectedCampaign.isActive}
                      onChange={(e) => setEditForm(prev => ({ ...prev, isActive: e.target.checked }))}
                      className="accent-primary w-4 h-4"
                      disabled={isUpdating}
                    />
                    Campaña Activa
                  </label>
                </div>

                <AdminFormActions
                  onCancel={() => setShowEditForm(false)}
                  onSave={handleUpdate}
                  isSaving={isUpdating}
                />
              </div>
            ) : showTransferForm && selectedCampaign ? (
              /* Transfer Ownership Form */
              <div className="border border-purple-500/30 bg-black/60 p-6 flex-1">
                <h2 className="text-sm text-purple-500/60 uppercase tracking-widest mb-4 flex items-center gap-2">
                  <span className="material-icons text-xs">swap_horiz</span>
                  Transferir Propiedad
                </h2>
                
                <div className="space-y-4">
                  {/* Current Owner */}
                  <div className="bg-black/40 border border-primary/20 p-3">
                    <p className="text-[10px] text-primary/40 uppercase mb-1">Propietario Actual</p>
                    <p className="text-sm text-primary">{selectedCampaign.ownerName}</p>
                  </div>

                  {/* New Owner Selection */}
                  <div>
                    <label className="block text-xs text-primary/40 uppercase mb-1">Nuevo Propietario</label>
                    {isLoadingUsers ? (
                      <div className="text-primary/40 text-sm animate-pulse p-3">Cargando usuarios...</div>
                    ) : (
                      <select
                        value={transferOwnerId}
                        onChange={(e) => setTransferOwnerId(e.target.value)}
                        className="w-full bg-black/40 border border-primary/30 text-primary p-3 text-sm focus:border-purple-500 focus:outline-none"
                        disabled={isUpdating}
                      >
                        <option value="">Seleccionar usuario...</option>
                        {users
                          .filter(u => u.id !== selectedCampaign.ownerId)
                          .map(user => (
                            <option key={user.id} value={user.id}>
                              {user.displayName || user.email} ({user.email})
                            </option>
                          ))
                        }
                      </select>
                    )}
                  </div>

                  {/* Warning */}
                  <div className="bg-yellow-500/10 border border-yellow-500/30 p-3 text-xs text-yellow-400">
                    <span className="material-icons text-sm align-middle mr-1">warning</span>
                    Esta acción transferirá todos los permisos de propietario al nuevo usuario.
                  </div>
                </div>

                <div className="mt-6 flex gap-2">
                  <Button
                    onClick={() => setShowTransferForm(false)}
                    variant="secondary"
                    className="flex-1"
                  >
                    CANCELAR
                  </Button>
                  <Button
                    onClick={handleTransfer}
                    disabled={isUpdating || !transferOwnerId}
                    className="flex-1 bg-purple-500/20 border-purple-500 text-purple-400 hover:bg-purple-500 hover:text-black"
                  >
                    {isUpdating ? 'TRANSFIRIENDO...' : 'TRANSFERIR'}
                  </Button>
                </div>
              </div>
            ) : selectedCampaign ? (
              /* Campaign Details */
              <div className="border border-primary/30 bg-black/60 flex-1 flex flex-col overflow-hidden">
                <div className="bg-primary/10 p-4 text-xs text-primary uppercase tracking-widest flex items-center justify-between">
                  <span className="flex items-center gap-2">
                    <span className="material-icons text-sm">public</span>
                    Detalles de la Campaña
                  </span>
                  <div className="flex gap-1">
                    <button 
                      onClick={() => openEditForm(selectedCampaign)}
                      className="material-icons text-sm text-primary/60 hover:text-cyan-500 transition-colors"
                      title="Editar"
                    >
                      edit
                    </button>
                    <button 
                      onClick={() => openTransferForm(selectedCampaign)}
                      className="material-icons text-sm text-primary/60 hover:text-purple-500 transition-colors"
                      title="Transferir"
                    >
                      swap_horiz
                    </button>
                  </div>
                </div>
                
                <div className="flex-1 p-6 overflow-y-auto space-y-4">
                  {/* Name & Status */}
                  <div className="pb-4 border-b border-primary/20">
                    <div className="flex items-center gap-3">
                      <h3 className="text-2xl text-primary font-bold">{selectedCampaign.name}</h3>
                      <AdminStatusBadge
                        isActive={selectedCampaign.isActive}
                        activeLabel="ACTIVA"
                        inactiveLabel="INACTIVA"
                      />
                    </div>
                    {selectedCampaign.description && (
                      <p className="text-sm text-primary/60 mt-2">{selectedCampaign.description}</p>
                    )}
                  </div>

                  {/* Join Code */}
                  <div className="bg-black/40 border border-cyan-500/20 p-3">
                    <p className="text-[10px] text-cyan-500/60 uppercase mb-1">Código de Invitación</p>
                    <p className="text-xl text-cyan-400 font-mono tracking-[0.3em]">{selectedCampaign.joinCode}</p>
                  </div>

                  {/* Stats Grid */}
                  <div className="grid grid-cols-2 gap-3">
                    <AdminStatCard label="Miembros" value={selectedCampaign.memberCount} />
                    <AdminStatCard label="Entidades" value={selectedCampaign.entityCount} />
                  </div>

                  {/* Owner & System */}
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-primary/40">Propietario:</span>
                      <span className="text-primary">{selectedCampaign.ownerName}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-primary/40">Sistema:</span>
                      <span className="text-primary">{selectedCampaign.gameSystemName}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-primary/40">Creada:</span>
                      <span className="text-primary">{formatAdminDate(selectedCampaign.createdAt)}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-primary/40">Actualizada:</span>
                      <span className="text-primary">{formatAdminDate(selectedCampaign.updatedAt)}</span>
                    </div>
                  </div>

                  {/* IDs */}
                  <div className="pt-4 border-t border-primary/20 space-y-2">
                    <div>
                      <p className="text-[10px] text-primary/30 uppercase">Campaign ID</p>
                      <p className="text-xs text-primary/60 font-mono break-all">{selectedCampaign.id}</p>
                    </div>
                    <div>
                      <p className="text-[10px] text-primary/30 uppercase">Owner ID</p>
                      <p className="text-xs text-primary/60 font-mono break-all">{selectedCampaign.ownerId}</p>
                    </div>
                    <div>
                      <p className="text-[10px] text-primary/30 uppercase">Game System ID</p>
                      <p className="text-xs text-primary/60 font-mono break-all">{selectedCampaign.gameSystemId}</p>
                    </div>
                  </div>
                </div>
              </div>
            ) : (
              /* No Selection */
              <AdminEmptyState icon="public" message="Selecciona una campaña" />
            )}

            <AdminTerminalPanel logs={logs} />
          </div>
        </div>
      </AdminLayout>
    </AdminGuard>
  );
};
