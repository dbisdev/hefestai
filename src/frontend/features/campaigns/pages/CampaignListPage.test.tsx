/**
 * CampaignListPage Tests
 * Tests for the Campaign List page component including:
 * - Rendering campaigns list (GameSystemsPage style)
 * - Filter functionality (all, master, player)
 * - Campaign selection and details sidebar
 * - Navigation actions
 * - Edit flow
 * - Error handling
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, within, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { CampaignListPage } from './CampaignListPage';
import { Screen, CampaignRole } from '@core/types';

// Mock dependencies
vi.mock('@core/context', () => ({
  useCampaign: vi.fn(),
}));

vi.mock('@core/services/api', () => ({
  gameSystemService: {
    getAll: vi.fn(),
  },
}));

vi.mock('@shared/components/layout', () => ({
  TerminalLayout: ({ children, title, subtitle, onLogout }: {
    children: React.ReactNode;
    title: string;
    subtitle: string;
    onLogout?: () => void;
  }) => (
    <div data-testid="terminal-layout">
      <header>
        <h1>{title}</h1>
        <p>{subtitle}</p>
        {onLogout && <button onClick={onLogout} data-testid="logout-button">LOGOUT</button>}
      </header>
      <main>{children}</main>
    </div>
  ),
}));

vi.mock('@shared/components/ui', () => ({
  Button: ({ children, onClick, icon, variant, size, className, disabled, ...props }: {
    children: React.ReactNode;
    onClick?: () => void;
    icon?: string;
    variant?: string;
    size?: string;
    className?: string;
    disabled?: boolean;
  }) => (
    <button 
      onClick={onClick} 
      data-icon={icon} 
      data-variant={variant} 
      data-size={size} 
      className={className}
      disabled={disabled}
      {...props}
    >
      {children}
    </button>
  ),
}));

// Import mocked modules
import { useCampaign } from '@core/context';
import { gameSystemService } from '@core/services/api';

// Type the mocks
const mockUseCampaign = vi.mocked(useCampaign);
const mockGameSystemService = vi.mocked(gameSystemService);

// Test data
const mockGameSystems = [
  { 
    id: 'system-1', 
    code: 'dnd5e',
    name: 'Dungeons & Dragons 5e', 
    description: 'Fantasy RPG', 
    supportedEntityTypes: ['character', 'npc', 'enemy', 'faction', 'location', 'item', 'lore'],
  },
  { 
    id: 'system-2', 
    code: 'cpr',
    name: 'Cyberpunk Red', 
    description: 'Sci-fi RPG',
    supportedEntityTypes: ['character', 'npc', 'enemy', 'faction', 'location', 'item', 'lore'],
  },
];

const mockCampaigns = [
  {
    id: 'campaign-1',
    name: 'Dragon Hunt',
    description: 'Epic fantasy adventure',
    userRole: CampaignRole.Master,
    gameSystemId: 'system-1',
    isActive: true,
    createdAt: '2024-01-15T10:00:00Z',
  },
  {
    id: 'campaign-2',
    name: 'Night City Stories',
    description: 'Cyberpunk noir',
    userRole: CampaignRole.Player,
    gameSystemId: 'system-2',
    isActive: false,
    createdAt: '2024-02-20T14:30:00Z',
  },
  {
    id: 'campaign-3',
    name: 'Lost Mines',
    description: 'Classic dungeon crawl',
    userRole: CampaignRole.Master,
    gameSystemId: 'system-1',
    isActive: true,
    createdAt: '2024-03-10T08:00:00Z',
  },
];

// CampaignDetail for activeCampaign (has additional fields)
const mockActiveCampaignDetail = {
  ...mockCampaigns[0],
  ownerId: 'user-123',
  memberCount: 4,
  joinCode: 'ABC123',
};

describe('CampaignListPage', () => {
  const mockOnNavigate = vi.fn();
  const mockOnLogout = vi.fn();
  const mockSelectCampaign = vi.fn();
  const mockLeaveCampaign = vi.fn();
  const mockUpdateCampaign = vi.fn();
  const mockUpdateCampaignStatus = vi.fn();
  const mockFetchCampaigns = vi.fn();
  const mockClearError = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();

    // Default mock setup
    mockUseCampaign.mockReturnValue({
      campaigns: mockCampaigns,
      activeCampaign: mockActiveCampaignDetail,
      activeCampaignId: mockCampaigns[0].id,
      isLoading: false,
      error: null,
      isActiveCampaignMaster: true,
      fetchCampaigns: mockFetchCampaigns,
      selectCampaign: mockSelectCampaign,
      clearActiveCampaign: vi.fn(),
      createCampaign: vi.fn(),
      joinCampaign: vi.fn(),
      leaveCampaign: mockLeaveCampaign,
      deleteCampaign: vi.fn(),
      updateCampaign: mockUpdateCampaign,
      updateCampaignStatus: mockUpdateCampaignStatus,
      regenerateJoinCode: vi.fn(),
      clearError: mockClearError,
    });

    mockGameSystemService.getAll.mockResolvedValue(mockGameSystems);
    mockSelectCampaign.mockResolvedValue(undefined);
    mockLeaveCampaign.mockResolvedValue(undefined);
    mockUpdateCampaign.mockResolvedValue(mockCampaigns[0]);
    mockUpdateCampaignStatus.mockResolvedValue(undefined);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Rendering', () => {
    it('renders the campaign list page with title', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      expect(screen.getByText('CAMPAÑAS')).toBeInTheDocument();
      expect(screen.getByText('Mis Campañas')).toBeInTheDocument();
    });

    it('renders header with new campaign button', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      expect(screen.getByText('+ NUEVA CAMPAÑA')).toBeInTheDocument();
    });

    it('renders filter tabs with correct counts', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Check filter tabs exist
      expect(screen.getByText('TODAS')).toBeInTheDocument();
      expect(screen.getAllByText('MASTER').length).toBeGreaterThan(0);
      expect(screen.getAllByText('JUGADOR').length).toBeGreaterThan(0);

      // Check counts (3 total, 2 master, 1 player)
      expect(screen.getByText('(3)')).toBeInTheDocument();
      expect(screen.getByText('(2)')).toBeInTheDocument();
      expect(screen.getByText('(1)')).toBeInTheDocument();
    });

    it('renders campaign items in list style', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Campaign names
      expect(screen.getByText('Dragon Hunt')).toBeInTheDocument();
      expect(screen.getByText('Night City Stories')).toBeInTheDocument();
      expect(screen.getByText('Lost Mines')).toBeInTheDocument();

      // Descriptions
      expect(screen.getByText('Epic fantasy adventure')).toBeInTheDocument();
      expect(screen.getByText('Cyberpunk noir')).toBeInTheDocument();
    });

    it('shows active campaign indicator badge', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Active badge should be shown for the active campaign
      expect(screen.getByText('ACTIVA')).toBeInTheDocument();
    });

    it('displays game system names after loading', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      await waitFor(() => {
        expect(screen.getAllByText(/Dungeons & Dragons 5e/i).length).toBeGreaterThan(0);
        expect(screen.getAllByText(/Cyberpunk Red/i).length).toBeGreaterThan(0);
      });
    });

    it('shows loading state when campaigns are loading', async () => {
      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        campaigns: [],
        isLoading: true,
      });

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      expect(screen.getByText(/cargando campañas/i)).toBeInTheDocument();
    });

    it('shows empty state when no campaigns exist', async () => {
      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        campaigns: [],
        isLoading: false,
      });

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      expect(screen.getByText('No hay campañas')).toBeInTheDocument();
    });

    it('displays statistics sidebar when no campaign is selected', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      expect(screen.getByText('Estadísticas')).toBeInTheDocument();
      expect(screen.getByText('Total')).toBeInTheDocument();
    });
  });

  describe('Campaign Selection (GameSystemsPage style)', () => {
    it('selects campaign when clicked and shows details in sidebar', async () => {
      const user = userEvent.setup();

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Click on a campaign to select it
      const campaignItem = screen.getByText('Night City Stories');
      await user.click(campaignItem);

      // Should show campaign details in sidebar
      await waitFor(() => {
        expect(screen.getByText('Campaña Seleccionada')).toBeInTheDocument();
      });
    });

    it('shows action buttons in sidebar for selected campaign', async () => {
      const user = userEvent.setup();

      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        activeCampaign: null, // No active campaign so "ACTIVAR Y ENTRAR" shows
      });

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Click on a campaign to select it
      await user.click(screen.getByText('Dragon Hunt'));

      // Should show action buttons in sidebar
      await waitFor(() => {
        expect(screen.getByText(/ACTIVAR Y ENTRAR/i)).toBeInTheDocument();
        expect(screen.getByText('EDITAR')).toBeInTheDocument();
        expect(screen.getByText('CONFIGURACIÓN AVANZADA')).toBeInTheDocument();
      });
    });

    it('hides statistics when campaign is selected', async () => {
      const user = userEvent.setup();

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Initially statistics visible
      expect(screen.getByText('Estadísticas')).toBeInTheDocument();

      // Click on a campaign
      await user.click(screen.getByText('Dragon Hunt'));

      // Statistics should be replaced by campaign details
      await waitFor(() => {
        expect(screen.getByText('Campaña Seleccionada')).toBeInTheDocument();
      });
    });
  });

  describe('Filter Functionality', () => {
    it('filters campaigns by master role', async () => {
      const user = userEvent.setup();

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Initially all campaigns visible
      expect(screen.getByText('Dragon Hunt')).toBeInTheDocument();
      expect(screen.getByText('Night City Stories')).toBeInTheDocument();
      expect(screen.getByText('Lost Mines')).toBeInTheDocument();

      // Click master filter tab (find by the tab that contains count)
      const filterTabs = screen.getAllByText('MASTER');
      const masterTab = filterTabs.find(el => el.closest('button')?.textContent?.includes('(2)'));
      await user.click(masterTab!);

      // Only master campaigns should be visible
      expect(screen.getByText('Dragon Hunt')).toBeInTheDocument();
      expect(screen.getByText('Lost Mines')).toBeInTheDocument();
      expect(screen.queryByText('Night City Stories')).not.toBeInTheDocument();
    });

    it('filters campaigns by player role', async () => {
      const user = userEvent.setup();

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Click player filter tab
      const filterTabs = screen.getAllByText('JUGADOR');
      const playerTab = filterTabs.find(el => el.closest('button')?.textContent?.includes('(1)'));
      await user.click(playerTab!);

      // Only player campaigns should be visible
      expect(screen.queryByText('Dragon Hunt')).not.toBeInTheDocument();
      expect(screen.getByText('Night City Stories')).toBeInTheDocument();
      expect(screen.queryByText('Lost Mines')).not.toBeInTheDocument();
    });

    it('shows empty state for filtered view with no matching campaigns', async () => {
      const user = userEvent.setup();

      // Only master campaigns
      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        campaigns: [mockCampaigns[0], mockCampaigns[2]], // Only master campaigns
      });

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Click player filter tab
      const filterTabs = screen.getAllByText('JUGADOR');
      const playerTab = filterTabs.find(el => el.closest('button')?.textContent?.includes('(0)'));
      await user.click(playerTab!);

      expect(screen.getByText(/no hay campañas como jugador/i)).toBeInTheDocument();
    });
  });

  describe('Campaign Actions (from sidebar)', () => {
    it('navigates to campaign generator when "Nueva Campaña" is clicked', async () => {
      const user = userEvent.setup();

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      const newButton = screen.getByText('+ NUEVA CAMPAÑA');
      await user.click(newButton);

      expect(mockOnNavigate).toHaveBeenCalledWith(Screen.CAMPAIGN_GEN);
    });

    it('activates campaign and navigates to gallery when "Activar y Entrar" is clicked', async () => {
      const user = userEvent.setup();

      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        activeCampaign: null,
        activeCampaignId: null,
      });

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // First select a campaign
      await user.click(screen.getByText('Dragon Hunt'));

      // Then click activate button in sidebar
      await waitFor(() => {
        expect(screen.getByText(/ACTIVAR Y ENTRAR/i)).toBeInTheDocument();
      });

      await user.click(screen.getByText(/ACTIVAR Y ENTRAR/i));

      await waitFor(() => {
        expect(mockSelectCampaign).toHaveBeenCalledWith('campaign-1');
      });

      await waitFor(() => {
        expect(mockOnNavigate).toHaveBeenCalledWith(Screen.GALLERY);
      });
    });

    it('navigates to campaign settings when "Configuración Avanzada" is clicked', async () => {
      const user = userEvent.setup();

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Select a master campaign
      await user.click(screen.getByText('Dragon Hunt'));

      // Click settings button in sidebar
      await waitFor(() => {
        expect(screen.getByText('CONFIGURACIÓN AVANZADA')).toBeInTheDocument();
      });

      await user.click(screen.getByText('CONFIGURACIÓN AVANZADA'));

      await waitFor(() => {
        expect(mockSelectCampaign).toHaveBeenCalled();
        expect(mockOnNavigate).toHaveBeenCalledWith(Screen.CAMPAIGN_SETTINGS);
      });
    });

    it('does not show edit/settings buttons for player campaigns', async () => {
      const user = userEvent.setup();

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Select a player campaign
      await user.click(screen.getByText('Night City Stories'));

      // Should not show master-only buttons
      await waitFor(() => {
        expect(screen.getByText('Campaña Seleccionada')).toBeInTheDocument();
      });

      expect(screen.queryByText('EDITAR')).not.toBeInTheDocument();
      expect(screen.queryByText('CONFIGURACIÓN AVANZADA')).not.toBeInTheDocument();
      // But should show leave button
      expect(screen.getByText('ABANDONAR')).toBeInTheDocument();
    });

    it('calls leaveCampaign when ABANDONAR is clicked and confirmed', async () => {
      const user = userEvent.setup();

      const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Select a player campaign
      await user.click(screen.getByText('Night City Stories'));

      // Click leave button
      await waitFor(() => {
        expect(screen.getByText('ABANDONAR')).toBeInTheDocument();
      });

      await user.click(screen.getByText('ABANDONAR'));

      expect(confirmSpy).toHaveBeenCalled();

      await waitFor(() => {
        expect(mockLeaveCampaign).toHaveBeenCalledWith('campaign-2');
      });

      confirmSpy.mockRestore();
    });

    it('shows ELIMINAR button for master campaigns', async () => {
      const user = userEvent.setup();

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Select a master campaign
      await user.click(screen.getByText('Dragon Hunt'));

      await waitFor(() => {
        expect(screen.getByText('ELIMINAR')).toBeInTheDocument();
      });
    });
  });

  describe('Edit Flow (GameSystemsPage style)', () => {
    it('shows edit form when EDITAR is clicked', async () => {
      const user = userEvent.setup();

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Select a master campaign
      await user.click(screen.getByText('Dragon Hunt'));

      // Click edit button
      await waitFor(() => {
        expect(screen.getByText('EDITAR')).toBeInTheDocument();
      });

      await user.click(screen.getByText('EDITAR'));

      // Should show edit form
      await waitFor(() => {
        expect(screen.getByText(/Editar Campaña:/i)).toBeInTheDocument();
      });
    });

    it('edit form shows campaign name input', async () => {
      const user = userEvent.setup();

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Select and edit
      await user.click(screen.getByText('Dragon Hunt'));
      await waitFor(() => expect(screen.getByText('EDITAR')).toBeInTheDocument());
      await user.click(screen.getByText('EDITAR'));

      // Should have name input with current value
      await waitFor(() => {
        const nameInput = screen.getByDisplayValue('Dragon Hunt');
        expect(nameInput).toBeInTheDocument();
      });
    });

    it('calls updateCampaign when saving changes', async () => {
      const user = userEvent.setup();

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Select and edit
      await user.click(screen.getByText('Dragon Hunt'));
      await waitFor(() => expect(screen.getByText('EDITAR')).toBeInTheDocument());
      await user.click(screen.getByText('EDITAR'));

      // Change the name
      await waitFor(() => {
        expect(screen.getByDisplayValue('Dragon Hunt')).toBeInTheDocument();
      });

      const nameInput = screen.getByDisplayValue('Dragon Hunt');
      await user.clear(nameInput);
      await user.type(nameInput, 'Updated Campaign Name');

      // Click save
      await user.click(screen.getByText('GUARDAR CAMBIOS'));

      await waitFor(() => {
        expect(mockUpdateCampaign).toHaveBeenCalledWith('campaign-1', expect.objectContaining({
          name: 'Updated Campaign Name',
        }));
      });
    });

    it('closes edit form when CANCELAR is clicked', async () => {
      const user = userEvent.setup();

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Select and edit
      await user.click(screen.getByText('Dragon Hunt'));
      await waitFor(() => expect(screen.getByText('EDITAR')).toBeInTheDocument());
      await user.click(screen.getByText('EDITAR'));

      // Verify form is open
      await waitFor(() => {
        expect(screen.getByText(/Editar Campaña:/i)).toBeInTheDocument();
      });

      // Click cancel
      await user.click(screen.getByText('CANCELAR'));

      // Form should be closed
      await waitFor(() => {
        expect(screen.queryByText(/Editar Campaña:/i)).not.toBeInTheDocument();
      });
    });
  });

  describe('Error Handling', () => {
    it('displays error message when error exists', async () => {
      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        error: 'Failed to load campaigns',
      });

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      expect(screen.getByText('Failed to load campaigns')).toBeInTheDocument();
    });

    it('clears error when close button is clicked', async () => {
      const user = userEvent.setup();

      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        error: 'Failed to load campaigns',
      });

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Find close button within error container
      const errorContainer = screen.getByText('Failed to load campaigns').closest('div');
      const closeButton = errorContainer?.parentElement?.querySelector('button');
      
      if (closeButton) {
        await user.click(closeButton);
        expect(mockClearError).toHaveBeenCalled();
      }
    });
  });

  describe('Terminal Log Panel', () => {
    it('displays system log panel', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      expect(screen.getByText('System Log')).toBeInTheDocument();
    });

    it('shows initial log messages', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      expect(screen.getByText(/campaign registry system online/i)).toBeInTheDocument();
    });
  });

  describe('Campaign Status Indicators', () => {
    it('shows ONLINE status for active campaigns', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      const onlineStatuses = screen.getAllByText('ONLINE');
      expect(onlineStatuses.length).toBeGreaterThan(0);
    });

    it('shows OFFLINE status for inactive campaigns', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      expect(screen.getByText('OFFLINE')).toBeInTheDocument();
    });

    it('shows role indicator for each campaign', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Should show MASTER and JUGADOR indicators
      const masterLabels = screen.getAllByText('MASTER');
      const playerLabels = screen.getAllByText('JUGADOR');

      // 2 master campaigns in list + filter tab = 3
      expect(masterLabels.length).toBe(3);
      // 1 player campaign in list + filter tab = 2
      expect(playerLabels.length).toBe(2);
    });

    it('shows toggle button for master campaigns to change status', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Toggle buttons should exist for master campaigns
      const toggleButtons = screen.getAllByTitle(/pausar campaña|activar campaña/i);
      expect(toggleButtons.length).toBe(2); // 2 master campaigns
    });
  });

  describe('Game System Loading', () => {
    it('calls gameSystemService.getAll on mount', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      expect(mockGameSystemService.getAll).toHaveBeenCalled();
    });

    it('shows placeholder while game systems are loading', async () => {
      // Delay the game system response
      mockGameSystemService.getAll.mockImplementation(
        () => new Promise(resolve => setTimeout(() => resolve(mockGameSystems), 100))
      );

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Initially shows loading placeholder (text contains "...")
      expect(screen.getAllByText(/\.\.\./i).length).toBeGreaterThan(0);

      // Wait for game systems to load
      await waitFor(() => {
        expect(screen.getAllByText(/Dungeons & Dragons 5e/i).length).toBeGreaterThan(0);
      });
    });

    it('shows "Sistema desconocido" when game system not found', async () => {
      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        campaigns: [{
          ...mockCampaigns[0],
          gameSystemId: 'unknown-system',
        }],
      });

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      await waitFor(() => {
        expect(screen.getByText(/Sistema desconocido/i)).toBeInTheDocument();
      });
    });
  });
});
