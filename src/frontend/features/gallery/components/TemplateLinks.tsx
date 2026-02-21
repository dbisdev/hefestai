/**
 * Template Links Component
 * Single Responsibility: Render links to dynamic generators based on templates
 */

import React from 'react';
import { useNavigate } from 'react-router-dom';
import type { EntityTemplateSummary } from '@core/types';
import { getRouteForTemplate, getIconForEntityType } from '../constants/categories';

interface TemplateLinksProps {
  templates: EntityTemplateSummary[];
  isLoading?: boolean;
  isVisible?: boolean;
}

export const TemplateLinks: React.FC<TemplateLinksProps> = ({
  templates,
  isLoading = false,
  isVisible = true,
}) => {
  const navigate = useNavigate();

  if (!isVisible) return null;

  if (isLoading) {
    return (
      <div className="space-y-1">
        {[1, 2, 3].map((i) => (
          <div
            key={i}
            className="h-8 bg-primary/10 rounded animate-pulse"
          />
        ))}
      </div>
    );
  }

  if (templates.length === 0) {
    return (
      <div className="px-3 py-2 text-[10px] text-primary/40 uppercase tracking-wider">
        Sin generadores configurados
      </div>
    );
  }

  return (
    <div className="space-y-1">
      <div className="text-[10px] text-primary/30 uppercase tracking-widest px-3 py-2 border-t border-primary/10">
        Generadores
      </div>
      {templates.map((template) => {
        const route = getRouteForTemplate(template.entityTypeName);
        if (!route) return null;

        const icon = getIconForEntityType(template.entityTypeName, template.iconHint);

        return (
          <button
            key={template.id}
            onClick={() => navigate(route)}
            className="w-full flex items-center gap-2 px-3 py-2 text-left text-xs uppercase tracking-wider
              text-primary/50 hover:bg-primary/5 hover:text-primary/70 transition-all border-l-2 
              border-transparent hover:border-primary/30"
          >
            <span className="material-icons text-sm">{icon}</span>
            <span className="flex-1 truncate">{template.displayName}</span>
            <span className="material-icons text-xs text-primary/30">arrow_forward</span>
          </button>
        );
      })}
    </div>
  );
};
