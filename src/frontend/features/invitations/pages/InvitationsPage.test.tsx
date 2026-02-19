/**
 * InvitationsPage Tests
 * Tests for the Invitations page component including:
 * - Rendering the invitations page (two-column layout)
 * - Join campaign functionality
 * - Campaign invitation codes list for Masters
 * - Navigation actions
 * - Error handling
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { InvitationsPage } from './InvitationsPage';
import { CampaignRole } from '@core/types';

// Mock useNavigate
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useParams: () => ({}),
  };
});

// Mock campaign service
const mockGetById = vi.fn();
vi.mock('@core/services/api', () => ({
  campaignService: {
    getById: (...args: unknown[]) => mockGetById(...args),
  },
}));

// Mock dependencies
vi.mock('@core/context', () => ({
  useAuth: vi.fn(),
  useCampaign: vi.fn(),
}));

vi.mock('@core/hooks/useTerminalLog', () => ({
  useTerminalLog: () => ({
    logs: ['> Test log'],
    addLog: vi.fn(),
  }),
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
  Button: ({ children, onClick, disabled, className, title, variant, ...props }: {
    children: React.ReactNode;
    onClick?: () => void;
    disabled?: boolean;
    className?: string;
    title?: string;
    variant?: string;
  }) => (
    <button 
      onClick={onClick} 
      disabled={disabled}
      className={className}
      title={title}
      data-variant={variant}
      {...props}
    >
      {children}
    </button>
  ),
  TerminalLog: ({ logs }: { logs: string[] }) => (
    <div data-testid="terminal-log">
      {logs.map((log, i) => <p key={i}>{log}</p>)}
    </div>
  ),
}));

// Import mocked modules
import { useAuth, useCampaign } from '@core/context';

// Type the mocks
const mockUseAuth = vi.mocked(useAuth);
const mockUseCampaign = vi.mocked(useCampaign);

// Test data with joinCode and gameSystem
const mockCampaigns = [
  {
    id: 'campaign-1',
    name: 'Dragon Hunt',
    description: 'Epic fantasy adventure',
    userRole: CampaignRole.Master,
    gameSystemId: 'system-1',
    gameSystem: { id: 'system-1', name: 'D&D 5e' },
    joinCode: 'DRAG1234',
    isActive: true,
    createdAt: '2024-01-15T10:00:00Z',
  },
  {
    id: 'campaign-2',
    name: 'Night City Stories',
    description: 'Cyberpunk noir',
    userRole: CampaignRole.Player,
    gameSystemId: 'system-2',
    gameSystem: { id: 'system-2', name: 'Cyberpunk RED' },
    joinCode: 'NGHT5678',
    isActive: false,
    createdAt: '2024-02-20T14:30:00Z',
  },
  {
    id: 'campaign-3',
    name: 'Shadow Realm',
    description: 'Dark fantasy',
    userRole: CampaignRole.Master,
    gameSystemId: 'system-1',
    gameSystem: { id: 'system-1', name: 'D&D 5e' },
    joinCode: 'SHAD9012',
    isActive: true,
    createdAt: '2024-03-10T08:00:00Z',
  },
];

const mockMasterUser = {
  id: 'user-123',
  username: 'testmaster',
  email: 'master@test.com',
  role: 'MASTER' as const,
  invitationCode: 'MSTR1234',
};

const mockPlayerUser = {
  id: 'user-456',
  username: 'testplayer',
  email: 'player@test.com',
  role: 'PLAYER' as const,
};

describe('InvitationsPage', () => {
  const mockJoinCampaign = vi.fn();
  const mockClearError = vi.fn();

  const renderComponent = () => {
    return render(
      <BrowserRouter>
        <InvitationsPage />
      </BrowserRouter>
    );
  };

  beforeEach(() => {
    vi.clearAllMocks();

    // Mock campaignService.getById to return detailed campaign data
    mockGetById.mockImplementation((id: string) => {
      const campaign = mockCampaigns.find(c => c.id === id);
      if (campaign) {
        return Promise.resolve({
          ...campaign,
          ownerId: 'user-123',
          memberCount: 4,
        });
      }
      return Promise.reject(new Error('Campaign not found'));
    });

    // Default mock setup - Master user
    mockUseAuth.mockReturnValue({
      user: mockMasterUser,
      isAuthenticated: true,
      isLoading: false,
      error: null,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      clearError: vi.fn(),
      isMaster: true,
      isPlayer: false,
      isAdmin: false,
    });

    mockUseCampaign.mockReturnValue({
      campaigns: mockCampaigns,
      activeCampaign: null,
      activeCampaignId: null,
      isLoading: false,
      error: null,
      isActiveCampaignMaster: false,
      fetchCampaigns: vi.fn(),
      selectCampaign: vi.fn(),
      clearActiveCampaign: vi.fn(),
      createCampaign: vi.fn(),
      joinCampaign: mockJoinCampaign,
      leaveCampaign: vi.fn(),
      deleteCampaign: vi.fn(),
      updateCampaign: vi.fn(),
      updateCampaignStatus: vi.fn(),
      regenerateJoinCode: vi.fn(),
      clearError: mockClearError,
    });

    mockJoinCampaign.mockResolvedValue({ id: 'new-campaign', name: 'New Campaign' });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Rendering', () => {
    it('renders the invitations page with title', async () => {
      renderComponent();

      expect(screen.getByText('INVITACIONES')).toBeInTheDocument();
      expect(screen.getByText('Centro de Invitaciones')).toBeInTheDocument();
    });

    it('renders join campaign section', async () => {
      renderComponent();

      expect(screen.getByText('Unirse a una Campaña')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('________')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'UNIRSE A CAMPAÑA' })).toBeInTheDocument();
    });

    it('renders view campaigns button in header', async () => {
      renderComponent();

      expect(screen.getByRole('button', { name: 'VER MIS CAMPAÑAS' })).toBeInTheDocument();
    });

    it('renders terminal log section', async () => {
      renderComponent();

      expect(screen.getByText('System Log')).toBeInTheDocument();
      expect(screen.getByText('> Sistema de invitaciones activo...')).toBeInTheDocument();
    });
  });

  describe('Campaign Codes List (Master View)', () => {
    it('shows campaign codes section for Master users', async () => {
      renderComponent();

      expect(screen.getByText('Codigos de Mis Campañas')).toBeInTheDocument();
    });

    it('displays list of master campaigns with codes', async () => {
      renderComponent();

      // Wait for async campaign details to load
      await waitFor(() => {
        expect(screen.getByText('Dragon Hunt')).toBeInTheDocument();
      });

      // Should show campaigns where user is Master
      expect(screen.getByText('DRAG1234')).toBeInTheDocument();
      expect(screen.getByText('Shadow Realm')).toBeInTheDocument();
      expect(screen.getByText('SHAD9012')).toBeInTheDocument();
      
      // Should NOT show campaigns where user is Player
      expect(screen.queryByText('NGHT5678')).not.toBeInTheDocument();
    });

    it('shows game system name for each campaign', async () => {
      renderComponent();

      // Wait for async campaign details to load
      await waitFor(() => {
        // Game system should be shown (appears multiple times due to multiple master campaigns)
        const dndElements = screen.getAllByText('D&D 5e');
        expect(dndElements.length).toBeGreaterThan(0);
      });
    });

    it('shows copy button for each campaign code', async () => {
      renderComponent();

      // Wait for async campaign details to load
      await waitFor(() => {
        // Should have copy buttons for each master campaign
        const copyButtons = screen.getAllByTitle('Copiar codigo');
        expect(copyButtons).toHaveLength(2); // 2 master campaigns
      });
    });

    it('copies campaign code to clipboard on button click', async () => {
      const user = userEvent.setup();
      const mockWriteText = vi.fn().mockResolvedValue(undefined);
      
      // Mock the clipboard API
      const originalClipboard = navigator.clipboard;
      Object.defineProperty(navigator, 'clipboard', {
        value: { writeText: mockWriteText },
        writable: true,
        configurable: true,
      });

      renderComponent();

      // Wait for async campaign details to load
      await waitFor(() => {
        expect(screen.getAllByTitle('Copiar codigo')).toHaveLength(2);
      });

      const copyButtons = screen.getAllByTitle('Copiar codigo');
      await user.click(copyButtons[0]);

      expect(mockWriteText).toHaveBeenCalledWith('DRAG1234');
      
      // Restore original clipboard
      Object.defineProperty(navigator, 'clipboard', {
        value: originalClipboard,
        writable: true,
        configurable: true,
      });
    });

    it('does not show campaign codes section for Player users', async () => {
      mockUseAuth.mockReturnValue({
        user: mockPlayerUser,
        isAuthenticated: true,
        isLoading: false,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: true,
        isAdmin: false,
      });

      renderComponent();

      expect(screen.queryByText('Codigos de Mis Campañas')).not.toBeInTheDocument();
    });

    it('shows empty state when master has no campaigns', async () => {
      mockUseCampaign.mockReturnValue({
        campaigns: [],
        activeCampaign: null,
        activeCampaignId: null,
        isLoading: false,
        error: null,
        isActiveCampaignMaster: false,
        fetchCampaigns: vi.fn(),
        selectCampaign: vi.fn(),
        clearActiveCampaign: vi.fn(),
        createCampaign: vi.fn(),
        joinCampaign: mockJoinCampaign,
        leaveCampaign: vi.fn(),
        deleteCampaign: vi.fn(),
        updateCampaign: vi.fn(),
        updateCampaignStatus: vi.fn(),
        regenerateJoinCode: vi.fn(),
        clearError: mockClearError,
      });

      renderComponent();

      expect(screen.getByText('No tienes campañas como Master')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /CREAR CAMPAÑA/i })).toBeInTheDocument();
    });
  });

  describe('Player Stats Panel', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: mockPlayerUser,
        isAuthenticated: true,
        isLoading: false,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: true,
        isAdmin: false,
      });
    });

    it('shows stats panel for Player users', async () => {
      renderComponent();

      expect(screen.getByText('Tus Campañas')).toBeInTheDocument();
      expect(screen.getByText('Total Campañas')).toBeInTheDocument();
      expect(screen.getByText('Como Jugador')).toBeInTheDocument();
    });

    it('shows zero counts when no campaigns (Player view)', async () => {
      mockUseCampaign.mockReturnValue({
        campaigns: [],
        activeCampaign: null,
        activeCampaignId: null,
        isLoading: false,
        error: null,
        isActiveCampaignMaster: false,
        fetchCampaigns: vi.fn(),
        selectCampaign: vi.fn(),
        clearActiveCampaign: vi.fn(),
        createCampaign: vi.fn(),
        joinCampaign: mockJoinCampaign,
        leaveCampaign: vi.fn(),
        deleteCampaign: vi.fn(),
        updateCampaign: vi.fn(),
        updateCampaignStatus: vi.fn(),
        regenerateJoinCode: vi.fn(),
        clearError: mockClearError,
      });

      renderComponent();

      // Stats panel shows for players - should show 0
      const statsSection = screen.getByText('Tus Campañas').closest('div')!.parentElement!;
      expect(statsSection).toHaveTextContent('0');
    });

    it('shows player campaign list', async () => {
      renderComponent();

      // Should show campaign where user is Player
      expect(screen.getByText('Night City Stories')).toBeInTheDocument();
      expect(screen.getByText(/Cyberpunk RED/)).toBeInTheDocument();
    });
  });

  describe('Join Campaign', () => {
    it('allows entering a join code', async () => {
      const user = userEvent.setup();

      renderComponent();

      const input = screen.getByPlaceholderText('________');
      await user.type(input, 'TEST1234');

      expect(input).toHaveValue('TEST1234');
    });

    it('converts join code to uppercase', async () => {
      const user = userEvent.setup();

      renderComponent();

      const input = screen.getByPlaceholderText('________');
      await user.type(input, 'test1234');

      expect(input).toHaveValue('TEST1234');
    });

    it('calls joinCampaign when submit button is clicked', async () => {
      const user = userEvent.setup();

      renderComponent();

      const input = screen.getByPlaceholderText('________');
      await user.type(input, 'TEST1234');

      const submitButton = screen.getByRole('button', { name: 'UNIRSE A CAMPAÑA' });
      await user.click(submitButton);

      await waitFor(() => {
        expect(mockJoinCampaign).toHaveBeenCalledWith('TEST1234');
      });
    });

    it('calls joinCampaign when Enter is pressed', async () => {
      const user = userEvent.setup();

      renderComponent();

      const input = screen.getByPlaceholderText('________');
      await user.type(input, 'TEST1234{enter}');

      await waitFor(() => {
        expect(mockJoinCampaign).toHaveBeenCalledWith('TEST1234');
      });
    });

    it('shows success message after joining', async () => {
      const user = userEvent.setup();
      mockJoinCampaign.mockResolvedValue({ id: 'camp-1', name: 'Epic Adventure' });

      renderComponent();

      const input = screen.getByPlaceholderText('________');
      await user.type(input, 'TEST1234');

      const submitButton = screen.getByRole('button', { name: 'UNIRSE A CAMPAÑA' });
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/Te has unido a la campaña: Epic Adventure/)).toBeInTheDocument();
      });
    });

    it('clears input after successful join', async () => {
      const user = userEvent.setup();
      mockJoinCampaign.mockResolvedValue({ id: 'camp-1', name: 'Epic Adventure' });

      renderComponent();

      const input = screen.getByPlaceholderText('________');
      await user.type(input, 'TEST1234');

      const submitButton = screen.getByRole('button', { name: 'UNIRSE A CAMPAÑA' });
      await user.click(submitButton);

      await waitFor(() => {
        expect(input).toHaveValue('');
      });
    });

    it('disables submit button when input is empty', async () => {
      renderComponent();

      const submitButton = screen.getByRole('button', { name: 'UNIRSE A CAMPAÑA' });
      expect(submitButton).toBeDisabled();
    });

    it('shows error for invalid join code format', async () => {
      const user = userEvent.setup();

      renderComponent();

      const input = screen.getByPlaceholderText('________');
      await user.type(input, 'ABC'); // Too short

      const submitButton = screen.getByRole('button', { name: 'UNIRSE A CAMPAÑA' });
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/El codigo debe tener 8 caracteres alfanumericos/)).toBeInTheDocument();
      });

      expect(mockJoinCampaign).not.toHaveBeenCalled();
    });

    it('shows error when join fails', async () => {
      const user = userEvent.setup();
      mockJoinCampaign.mockRejectedValue(new Error('Campaign not found'));

      renderComponent();

      const input = screen.getByPlaceholderText('________');
      await user.type(input, 'INVALID1');

      const submitButton = screen.getByRole('button', { name: 'UNIRSE A CAMPAÑA' });
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/Campaign not found/)).toBeInTheDocument();
      });
    });

    it('disables submit button while joining', async () => {
      const user = userEvent.setup();
      
      // Make joinCampaign slow
      mockJoinCampaign.mockImplementation(() => new Promise(resolve => setTimeout(resolve, 1000)));

      renderComponent();

      const input = screen.getByPlaceholderText('________');
      await user.type(input, 'TEST1234');

      const submitButton = screen.getByRole('button', { name: 'UNIRSE A CAMPAÑA' });
      await user.click(submitButton);

      // Button should show loading state
      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'PROCESANDO...' })).toBeInTheDocument();
      });
    });
  });

  describe('Navigation', () => {
    it('navigates to campaign list when view campaigns button is clicked', async () => {
      const user = userEvent.setup();

      renderComponent();

      const viewCampaignsButton = screen.getByRole('button', { name: 'VER MIS CAMPAÑAS' });
      await user.click(viewCampaignsButton);

      expect(mockNavigate).toHaveBeenCalledWith('/campaigns');
    });

    it('navigates to campaign list from stats panel (Player view)', async () => {
      mockUseAuth.mockReturnValue({
        user: mockPlayerUser,
        isAuthenticated: true,
        isLoading: false,
        error: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        clearError: vi.fn(),
        isMaster: false,
        isPlayer: true,
        isAdmin: false,
      });

      const user = userEvent.setup();

      renderComponent();

      const viewButton = screen.getByRole('button', { name: /VER CAMPAÑAS/i });
      await user.click(viewButton);

      expect(mockNavigate).toHaveBeenCalledWith('/campaigns');
    });

    it('navigates to campaign generator from empty master state', async () => {
      mockUseCampaign.mockReturnValue({
        campaigns: [],
        activeCampaign: null,
        activeCampaignId: null,
        isLoading: false,
        error: null,
        isActiveCampaignMaster: false,
        fetchCampaigns: vi.fn(),
        selectCampaign: vi.fn(),
        clearActiveCampaign: vi.fn(),
        createCampaign: vi.fn(),
        joinCampaign: mockJoinCampaign,
        leaveCampaign: vi.fn(),
        deleteCampaign: vi.fn(),
        updateCampaign: vi.fn(),
        updateCampaignStatus: vi.fn(),
        regenerateJoinCode: vi.fn(),
        clearError: mockClearError,
      });

      const user = userEvent.setup();

      renderComponent();

      const createButton = screen.getByRole('button', { name: /CREAR CAMPAÑA/i });
      await user.click(createButton);

      expect(mockNavigate).toHaveBeenCalledWith('/campaigns/new');
    });
  });

  describe('Terminal Log', () => {
    it('updates log on successful join', async () => {
      const user = userEvent.setup();
      mockJoinCampaign.mockResolvedValue({ id: 'camp-1', name: 'Epic Adventure' });

      renderComponent();

      const input = screen.getByPlaceholderText('________');
      await user.type(input, 'TEST1234');

      const submitButton = screen.getByRole('button', { name: 'UNIRSE A CAMPAÑA' });
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/UNIDO A: EPIC ADVENTURE/)).toBeInTheDocument();
      });
    });

    it('updates log on join error', async () => {
      const user = userEvent.setup();
      mockJoinCampaign.mockRejectedValue(new Error('Not found'));

      renderComponent();

      const input = screen.getByPlaceholderText('________');
      await user.type(input, 'INVALID1');

      const submitButton = screen.getByRole('button', { name: 'UNIRSE A CAMPAÑA' });
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/ERROR: NOT FOUND/)).toBeInTheDocument();
      });
    });

    it('updates log on copy campaign code', async () => {
      const user = userEvent.setup();
      const mockWriteText = vi.fn().mockResolvedValue(undefined);
      
      Object.defineProperty(navigator, 'clipboard', {
        value: { writeText: mockWriteText },
        writable: true,
        configurable: true,
      });

      renderComponent();

      // Wait for async campaign details to load
      await waitFor(() => {
        expect(screen.getAllByTitle('Copiar codigo')).toHaveLength(2);
      });

      const copyButtons = screen.getAllByTitle('Copiar codigo');
      await user.click(copyButtons[0]);

      await waitFor(() => {
        expect(screen.getByText(/CODIGO DE "DRAGON HUNT" COPIADO/)).toBeInTheDocument();
      });
    });
  });

  describe('Context Error Handling', () => {
    it('displays context error message', async () => {
      mockUseCampaign.mockReturnValue({
        campaigns: mockCampaigns,
        activeCampaign: null,
        activeCampaignId: null,
        isLoading: false,
        error: 'Network error occurred',
        isActiveCampaignMaster: false,
        fetchCampaigns: vi.fn(),
        selectCampaign: vi.fn(),
        clearActiveCampaign: vi.fn(),
        createCampaign: vi.fn(),
        joinCampaign: mockJoinCampaign,
        leaveCampaign: vi.fn(),
        deleteCampaign: vi.fn(),
        updateCampaign: vi.fn(),
        updateCampaignStatus: vi.fn(),
        regenerateJoinCode: vi.fn(),
        clearError: mockClearError,
      });

      renderComponent();

      expect(screen.getByText('Network error occurred')).toBeInTheDocument();
    });

    it('clears error on unmount', async () => {
      const { unmount } = renderComponent();

      unmount();

      expect(mockClearError).toHaveBeenCalled();
    });
  });

  describe('Loading State', () => {
    it('disables join button when context is loading', async () => {
      mockUseCampaign.mockReturnValue({
        campaigns: mockCampaigns,
        activeCampaign: null,
        activeCampaignId: null,
        isLoading: true,
        error: null,
        isActiveCampaignMaster: false,
        fetchCampaigns: vi.fn(),
        selectCampaign: vi.fn(),
        clearActiveCampaign: vi.fn(),
        createCampaign: vi.fn(),
        joinCampaign: mockJoinCampaign,
        leaveCampaign: vi.fn(),
        deleteCampaign: vi.fn(),
        updateCampaign: vi.fn(),
        updateCampaignStatus: vi.fn(),
        regenerateJoinCode: vi.fn(),
        clearError: mockClearError,
      });

      const user = userEvent.setup();

      renderComponent();

      const input = screen.getByPlaceholderText('________');
      await user.type(input, 'TEST1234');

      const submitButton = screen.getByRole('button', { name: 'UNIRSE A CAMPAÑA' });
      expect(submitButton).toBeDisabled();
    });
  });
});
