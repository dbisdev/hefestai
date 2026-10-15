/**
 * AdminTerminalPanel Component
 * DRY: Replaces the identical "System Log" panel block copy-pasted in
 * AdminUsersPage and AdminCampaignsPage.
 *
 * Renders a styled terminal header + TerminalLog inside a fixed-height panel.
 */

import React from 'react';
import { TerminalLog } from '@shared/components/ui';
import { TERMINAL_LOG_DEFAULT } from '@core/config/constants';

interface AdminTerminalPanelProps {
  /** Log entries from useTerminalLog (string array) */
  logs: string[];
  /** Maximum entries to show (defaults to TERMINAL_LOG_DEFAULT) */
  maxLogs?: number;
  /**
   * When true the panel stretches to fill its flex parent (flex-1) instead of
   * having a fixed height of h-48. Use this in the SystemPage right column.
   * Default: false (h-48 fixed height used in Users and Campaigns pages).
   */
  fill?: boolean;
}

/**
 * Renders the "System Log" terminal panel with a styled header and scrollable log area.
 * Fixed height (h-48) by default; pass `fill` to make it stretch instead.
 */
export const AdminTerminalPanel: React.FC<AdminTerminalPanelProps> = ({
  logs,
  maxLogs = TERMINAL_LOG_DEFAULT,
  fill = false,
}) => (
  <div className={`border border-primary/30 bg-black/80 flex flex-col ${fill ? 'flex-1' : 'h-48'}`}>
    <div className="bg-primary/20 p-2 text-xs text-primary uppercase tracking-widest flex items-center gap-2">
      <span className="material-icons text-sm">terminal</span>
      System Log
    </div>
    <TerminalLog logs={logs} maxLogs={maxLogs} className="flex-1" />
  </div>
);
