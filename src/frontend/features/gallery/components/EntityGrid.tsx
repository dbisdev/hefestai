/**
 * Entity Grid Component
 * Single Responsibility: Render entity grid with transitions
 */

import React from 'react';
import type { LoreEntity, EntityCategory } from '@core/types';
import { EntityCard } from './EntityCard';
import { EmptyState } from '@shared/components/ui';
import { getIconForEntityType } from '../constants/categories';

type TransitionStatus = 'idle' | 'out' | 'in';

interface EntityGridProps {
  entities: LoreEntity[];
  category: EntityCategory;
  selectedEntityId: string | null;
  onSelectEntity: (entity: LoreEntity | null) => void;
  transitionStatus: TransitionStatus;
  isLoading?: boolean;
  searchTerm?: string;
}

export const EntityGrid: React.FC<EntityGridProps> = ({
  entities,
  category,
  selectedEntityId,
  onSelectEntity,
  transitionStatus,
  isLoading = false,
  searchTerm = '',
}) => {
  const filteredEntities = searchTerm
    ? entities.filter(
        (e) =>
          e.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
          e.description?.toLowerCase().includes(searchTerm.toLowerCase())
      )
    : entities;

  const transitionClasses = {
    idle: 'opacity-100',
    out: 'opacity-0 transition-opacity duration-300',
    in: 'opacity-0 animate-fadeIn',
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <span className="material-icons text-primary text-4xl animate-pulse">sync</span>
      </div>
    );
  }

  if (filteredEntities.length === 0) {
    return (
      <EmptyState
        icon="inventory_2"
        title={searchTerm ? 'Sin resultados' : 'No hay entidades'}
        description={
          searchTerm
            ? `No se encontraron entidades para "${searchTerm}"`
            : 'Crea una nueva entidad usando los generadores'
        }
      />
    );
  }

  return (
    <div
      className={`grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 ${transitionClasses[transitionStatus]}`}
      role="listbox"
      aria-label={`Lista de ${entities.length} entidades`}
    >
      {filteredEntities.map((entity) => (
        <EntityCard
          key={entity.id}
          entity={entity}
          isSelected={selectedEntityId === entity.id}
          onSelect={onSelectEntity}
          icon={getIconForEntityType(entity.entityType)}
        />
      ))}
    </div>
  );
};
