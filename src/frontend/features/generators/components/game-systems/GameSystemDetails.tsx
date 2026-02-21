/**
 * Game System Details Component
 * Single Responsibility: Render selected system details panel
 */

import React from 'react';
import { Button } from '@shared/components/ui';
import type { GameSystem, EntityTemplateSummary, Campaign } from '@core/types';

interface GameSystemDetailsProps {
  system: GameSystem;
  isOwned: boolean;
  isLoadingData: boolean;
  confirmedTemplates: EntityTemplateSummary[];
  campaignsUsingSystem: Campaign[];
  onEdit: () => void;
  onUploadManual: () => void;
  onExtractEntities: () => void;
}

export const GameSystemDetails: React.FC<GameSystemDetailsProps> = ({
  system,
  isOwned,
  isLoadingData,
  confirmedTemplates,
  campaignsUsingSystem,
  onEdit,
  onUploadManual,
  onExtractEntities,
}) => {
  return (
    <div className="flex-1 border-t border-primary/30 p-4">
      <div className="space-y-2 text-xs">
        <div className="flex justify-between">
          <span className="text-primary/40">ID:</span>
          <span className="text-primary font-mono">{system.id.substring(0, 8)}...</span>
        </div>
        <div className="flex justify-between">
          <span className="text-primary/40">Código:</span>
          <span className="text-cyan-400 font-mono">{system.code}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-primary/40">Nombre:</span>
          <span className="text-primary">{system.name}</span>
        </div>
        {system.publisher && (
          <div className="flex justify-between">
            <span className="text-primary/40">Editorial:</span>
            <span className="text-primary/70">{system.publisher}</span>
          </div>
        )}
        {system.version && (
          <div className="flex justify-between">
            <span className="text-primary/40">Versión:</span>
            <span className="text-primary/70">{system.version}</span>
          </div>
        )}
      </div>

      <div className="mt-4 space-y-3">
        <div>
          <div className="text-xs text-primary/40 uppercase tracking-wider mb-1">
            Entidades Confirmadas ({confirmedTemplates.length})
          </div>
          {isLoadingData ? (
            <div className="text-xs text-primary/40">Cargando...</div>
          ) : confirmedTemplates.length > 0 ? (
            <ul className="text-xs space-y-1 max-h-24 overflow-y-auto">
              {confirmedTemplates.map((t) => (
                <li key={t.id} className="text-primary/70 flex items-center gap-1">
                  <span className="material-icons text-[10px]">check_circle</span>
                  {t.displayName}
                </li>
              ))}
            </ul>
          ) : (
            <div className="text-xs text-primary/40">Sin entidades confirmadas</div>
          )}
        </div>

        <div>
          <div className="text-xs text-primary/40 uppercase tracking-wider mb-1">
            Campañas ({campaignsUsingSystem.length})
          </div>
          {isLoadingData ? (
            <div className="text-xs text-primary/40">Cargando...</div>
          ) : campaignsUsingSystem.length > 0 ? (
            <ul className="text-xs space-y-1 max-h-24 overflow-y-auto">
              {campaignsUsingSystem.map((c) => (
                <li key={c.id} className="text-cyan-400/70 flex items-center gap-1">
                  <span className="material-icons text-[10px]">campaign</span>
                  {c.name}
                </li>
              ))}
            </ul>
          ) : (
            <div className="text-xs text-primary/40">Sin campañas activas</div>
          )}
        </div>

        {isOwned && (
          <div className="space-y-2 pt-2 border-t border-primary/20">
            <Button onClick={onEdit} variant="secondary" size="sm" fullWidth icon="edit">
              Editar
            </Button>
            <Button onClick={onUploadManual} variant="primary" size="sm" fullWidth icon="upload_file">
              Cargar Manual RAG
            </Button>
            <Button onClick={onExtractEntities} variant="secondary" size="sm" fullWidth icon="auto_awesome">
              Extraer Entidades
            </Button>
          </div>
        )}
      </div>
    </div>
  );
};
