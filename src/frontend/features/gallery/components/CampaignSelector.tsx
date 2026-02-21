/**
 * Campaign Selector Component
 * Single Responsibility: Render campaign dropdown selector
 */

import React, { useState, useRef, useEffect } from 'react';
import type { Campaign } from '@core/types';

interface CampaignSelectorProps {
  campaigns: Campaign[];
  activeCampaignId: string | undefined;
  onSelect: (campaignId: string) => void;
  isMaster: boolean;
  isLoading?: boolean;
}

export const CampaignSelector: React.FC<CampaignSelectorProps> = ({
  campaigns,
  activeCampaignId,
  onSelect,
  isMaster,
  isLoading = false,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const activeCampaign = campaigns.find((c) => c.id === activeCampaignId);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      setIsOpen(false);
    }
  };

  if (campaigns.length === 0) {
    return (
      <div className="px-3 py-2 text-xs text-primary/50 uppercase tracking-wider">
        Sin campaña activa
      </div>
    );
  }

  return (
    <div ref={dropdownRef} className="relative">
      <button
        onClick={() => setIsOpen(!isOpen)}
        onKeyDown={handleKeyDown}
        className="w-full flex items-center gap-2 px-3 py-2 text-left text-sm
          bg-primary/5 hover:bg-primary/10 border border-primary/20 rounded
          transition-colors"
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        disabled={isLoading}
      >
        <span className="material-icons text-primary/70 text-sm">campaign</span>
        <span className="flex-1 truncate text-primary/70">
          {activeCampaign?.name || 'Seleccionar campaña'}
        </span>
        <span className={`material-icons text-xs transition-transform ${isOpen ? 'rotate-180' : ''}`}>
          expand_more
        </span>
      </button>

      {isOpen && (
        <div
          className="absolute top-full left-0 right-0 mt-1 bg-surface-dark border border-primary/20 
            rounded shadow-lg z-20 max-h-64 overflow-y-auto"
          role="listbox"
        >
          {campaigns.map((campaign) => (
            <button
              key={campaign.id}
              onClick={() => {
                onSelect(campaign.id);
                setIsOpen(false);
              }}
              className={`w-full flex items-center gap-2 px-3 py-2 text-left text-sm
                hover:bg-primary/10 transition-colors ${
                  campaign.id === activeCampaignId
                    ? 'bg-primary/10 text-primary'
                    : 'text-primary/70'
                }`}
              role="option"
              aria-selected={campaign.id === activeCampaignId}
            >
              <span className="material-icons text-xs">
                {campaign.id === activeCampaignId ? 'check' : ' '}
              </span>
              <span className="flex-1 truncate">{campaign.name}</span>
              {!campaign.isActive && (
                <span className="text-[10px] text-primary/40 uppercase">Inactiva</span>
              )}
            </button>
          ))}
        </div>
      )}
    </div>
  );
};
