/**
 * CampaignListPage Tests
 * Tests for the Campaign List page component including:
 * - Rendering campaigns list
 * - Filter functionality (all, master, player)
 * - Campaign selection/activation
 * - Navigation actions
 * - Error handling
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, within, waitFor, fireEvent } from '@testing-library/react';
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
  Button: ({ children, onClick, icon, variant, size, ...props }: {
    children: React.ReactNode;
    onClick?: () => void;
    icon?: string;
    variant?: string;
    size?: string;
  }) => (
    <button onClick={onClick} data-icon={icon} data-variant={variant} data-size={size} {...props}>
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
    joinCode: 'ABC123',
    userRole: CampaignRole.Master,
    gameSystemId: 'system-1',
    isActive: true,
    ownerId: 'user-123',
    memberCount: 4,
    createdAt: '2024-01-15T10:00:00Z',
    updatedAt: '2024-01-15T10:00:00Z',
  },
  {
    id: 'campaign-2',
    name: 'Night City Stories',
    description: 'Cyberpunk noir',
    joinCode: 'XYZ789',
    userRole: CampaignRole.Player,
    gameSystemId: 'system-2',
    isActive: false,
    ownerId: 'user-456',
    memberCount: 6,
    createdAt: '2024-02-20T14:30:00Z',
    updatedAt: '2024-02-20T14:30:00Z',
  },
  {
    id: 'campaign-3',
    name: 'Lost Mines',
    description: 'Classic dungeon crawl',
    joinCode: 'LMN456',
    userRole: CampaignRole.Master,
    gameSystemId: 'system-1',
    isActive: true,
    ownerId: 'user-123',
    memberCount: 3,
    createdAt: '2024-03-10T08:00:00Z',
    updatedAt: '2024-03-10T08:00:00Z',
  },
];

describe('CampaignListPage', () => {
  const mockOnNavigate = vi.fn();
  const mockOnLogout = vi.fn();
  const mockSelectCampaign = vi.fn();
  const mockLeaveCampaign = vi.fn();
  const mockFetchCampaigns = vi.fn();
  const mockClearError = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();

    // Default mock setup
    mockUseCampaign.mockReturnValue({
      campaigns: mockCampaigns,
      activeCampaign: mockCampaigns[0],
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
      updateCampaign: vi.fn(),
      updateCampaignStatus: vi.fn(),
      regenerateJoinCode: vi.fn(),
      clearError: mockClearError,
    });

    mockGameSystemService.getAll.mockResolvedValue(mockGameSystems);
    mockSelectCampaign.mockResolvedValue(undefined);
    mockLeaveCampaign.mockResolvedValue(undefined);
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
    });

    it('renders filter tabs with correct counts', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Check filter tabs exist (using getAllByText since MASTER and JUGADOR appear multiple times)
      expect(screen.getByText('TODAS')).toBeInTheDocument();
      expect(screen.getAllByText('MASTER').length).toBeGreaterThan(0);
      expect(screen.getAllByText('JUGADOR').length).toBeGreaterThan(0);

      // Check counts (3 total, 2 master, 1 player)
      expect(screen.getByText('(3)')).toBeInTheDocument();
      expect(screen.getByText('(2)')).toBeInTheDocument();
      expect(screen.getByText('(1)')).toBeInTheDocument();
    });

    it('renders campaign cards with details', async () => {
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

    it('shows active campaign indicator', async () => {
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
        expect(screen.getAllByText('Dungeons & Dragons 5e').length).toBeGreaterThan(0);
        expect(screen.getByText('Cyberpunk Red')).toBeInTheDocument();
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
      expect(screen.getByText('CREAR CAMPAÑA')).toBeInTheDocument();
    });

    it('displays quick stats sidebar', async () => {
      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      expect(screen.getByText('Estadísticas')).toBeInTheDocument();
      expect(screen.getByText('Total')).toBeInTheDocument();
      expect(screen.getByText('Master')).toBeInTheDocument();
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

      // Click master filter tab (find by the tab that contains both MASTER and count)
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

      // Click player filter tab (find by the tab that contains both JUGADOR and count)
      const filterTabs = screen.getAllByText('JUGADOR');
      const playerTab = filterTabs.find(el => el.closest('button')?.textContent?.includes('(1)'));
      await user.click(playerTab!);

      // Only player campaigns should be visible
      expect(screen.queryByText('Dragon Hunt')).not.toBeInTheDocument();
      expect(screen.getByText('Night City Stories')).toBeInTheDocument();
      expect(screen.queryByText('Lost Mines')).not.toBeInTheDocument();
    });

    it('shows "all" filter results when selected', async () => {
      const user = userEvent.setup();

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Click master filter first (find tab with count)
      const masterTabs = screen.getAllByText('MASTER');
      const masterTab = masterTabs.find(el => el.closest('button')?.textContent?.includes('(2)'));
      await user.click(masterTab!);

      // Then click all filter
      await user.click(screen.getByText('TODAS'));

      // All campaigns should be visible
      expect(screen.getByText('Dragon Hunt')).toBeInTheDocument();
      expect(screen.getByText('Night City Stories')).toBeInTheDocument();
      expect(screen.getByText('Lost Mines')).toBeInTheDocument();
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

      // Click player filter tab (find by the tab that contains both JUGADOR and count)
      const filterTabs = screen.getAllByText('JUGADOR');
      const playerTab = filterTabs.find(el => el.closest('button')?.textContent?.includes('(0)'));
      await user.click(playerTab!);

      expect(screen.getByText(/no hay campañas como jugador/i)).toBeInTheDocument();
    });
  });

  describe('Campaign Actions', () => {
    it('navigates to campaign generator when "Nueva Campaña" is clicked', async () => {
      const user = userEvent.setup();

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      const newButton = screen.getByText('NUEVA CAMPAÑA');
      await user.click(newButton);

      expect(mockOnNavigate).toHaveBeenCalledWith(Screen.CAMPAIGN_GEN);
    });

    it('selects campaign and navigates to gallery when "Activar" is clicked', async () => {
      const user = userEvent.setup();

      // Set campaign-2 as not active
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

      // Find ACTIVAR buttons (all campaigns should have them now)
      const activateButtons = screen.getAllByText('ACTIVAR');
      await user.click(activateButtons[0]);

      await waitFor(() => {
        expect(mockSelectCampaign).toHaveBeenCalledWith('campaign-1');
      });

      await waitFor(() => {
        expect(mockOnNavigate).toHaveBeenCalledWith(Screen.GALLERY);
      });
    });

    it('navigates to campaign settings when "Configurar" is clicked (Master only)', async () => {
      const user = userEvent.setup();

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Find configurar buttons (only for master campaigns)
      const configButtons = screen.getAllByText('CONFIGURAR');
      expect(configButtons.length).toBe(2); // 2 master campaigns

      await user.click(configButtons[0]);

      await waitFor(() => {
        expect(mockSelectCampaign).toHaveBeenCalled();
      });

      await waitFor(() => {
        expect(mockOnNavigate).toHaveBeenCalledWith(Screen.CAMPAIGN_SETTINGS);
      });
    });

    it('does not show "Configurar" button for player campaigns', async () => {
      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        campaigns: [mockCampaigns[1]], // Only player campaign
        activeCampaign: null,
      });

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      expect(screen.queryByText('CONFIGURAR')).not.toBeInTheDocument();
    });

    it('calls leaveCampaign when leave button is clicked and confirmed', async () => {
      const user = userEvent.setup();

      // Mock window.confirm
      const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);

      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        campaigns: [mockCampaigns[1]], // Player campaign only
        activeCampaign: null,
      });

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      // Find leave button (logout icon for player)
      const leaveButton = screen.getByTitle('Abandonar');
      await user.click(leaveButton);

      expect(confirmSpy).toHaveBeenCalled();

      await waitFor(() => {
        expect(mockLeaveCampaign).toHaveBeenCalledWith('campaign-2');
      });

      confirmSpy.mockRestore();
    });

    it('does not call leaveCampaign when confirmation is cancelled', async () => {
      const user = userEvent.setup();

      // Mock window.confirm to return false
      const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);

      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        campaigns: [mockCampaigns[1]], // Player campaign only
        activeCampaign: null,
      });

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      const leaveButton = screen.getByTitle('Abandonar');
      await user.click(leaveButton);

      expect(mockLeaveCampaign).not.toHaveBeenCalled();

      confirmSpy.mockRestore();
    });

    it('shows delete button for master campaigns', async () => {
      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        campaigns: [mockCampaigns[0]], // Master campaign only
        activeCampaign: mockCampaigns[0],
      });

      render(
        <CampaignListPage
          onNavigate={mockOnNavigate}
          onLogout={mockOnLogout}
        />
      );

      expect(screen.getByTitle('Eliminar')).toBeInTheDocument();
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

      // Should show MASTER for master campaigns and JUGADOR for player campaigns
      const masterLabels = screen.getAllByText('MASTER');
      const playerLabels = screen.getAllByText('JUGADOR');

      // 2 master campaigns + the filter tab = 3
      expect(masterLabels.length).toBe(3);
      // 1 player campaign + the filter tab = 2
      expect(playerLabels.length).toBe(2);
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

      // Initially shows loading placeholder
      expect(screen.getAllByText('...').length).toBeGreaterThan(0);

      // Wait for game systems to load
      await waitFor(() => {
        expect(screen.getAllByText('Dungeons & Dragons 5e').length).toBeGreaterThan(0);
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
        expect(screen.getByText('Sistema desconocido')).toBeInTheDocument();
      });
    });
  });
});
