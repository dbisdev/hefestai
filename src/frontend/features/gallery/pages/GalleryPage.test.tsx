/**
 * GalleryPage Tests
 * Tests for the Gallery page component including:
 * - Accessibility features (ARIA, keyboard navigation)
 * - Category filtering
 * - Entity selection and detail panel
 * - Campaign selector
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, within, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { GalleryPage } from '@features/gallery/pages/GalleryPage';
import { Screen } from '@core/types';

// Mock the dependencies
vi.mock('@core/context', () => ({
  useCampaign: vi.fn(),
}));

vi.mock('@core/services/api', () => ({
  entityService: {
    getByCampaign: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('@shared/components/layout', () => ({
  TerminalLayout: ({ children, title, subtitle, onLogout, actions }: {
    children: React.ReactNode;
    title: string;
    subtitle: string;
    onLogout?: () => void;
    actions?: React.ReactNode;
  }) => (
    <div data-testid="terminal-layout">
      <header>
        <h1>{title}</h1>
        <p>{subtitle}</p>
        {actions && <div data-testid="header-actions">{actions}</div>}
        {onLogout && <button onClick={onLogout} data-testid="logout-button">LOGOUT</button>}
      </header>
      <main>{children}</main>
    </div>
  ),
}));

// Import mocked modules to configure them
import { useCampaign } from '@core/context';
import { entityService } from '@core/services/api';

// Type the mocks
const mockUseCampaign = vi.mocked(useCampaign);
const mockEntityService = vi.mocked(entityService);

// Test data
const mockUser = {
  id: 'user-123',
  username: 'testuser',
  email: 'test@example.com',
  role: 'PLAYER' as const,
};

const mockMasterUser = {
  ...mockUser,
  role: 'MASTER' as const,
};

const mockCampaign = {
  id: 'campaign-123',
  name: 'Test Campaign',
  description: 'A test campaign',
  joinCode: 'ABC123',
  userRole: 1, // Master
  gameSystemId: 'system-1',
  status: 1,
  members: [],
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
};

const mockEntities = [
  {
    id: 'char-1',
    name: 'Hero Character',
    description: 'A brave hero',
    entityType: 'character' as const,
    visibility: 2,
    imageUrl: 'https://example.com/hero.jpg',
    attributes: { strength: 10 },
    campaignId: 'campaign-123',
    ownerId: 'user-123',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 'char-2',
    name: 'Wizard Character',
    description: 'A powerful wizard',
    entityType: 'character' as const,
    visibility: 2,
    imageUrl: null,
    attributes: { intelligence: 18 },
    campaignId: 'campaign-123',
    ownerId: 'user-123',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 'npc-1',
    name: 'Village Elder',
    description: 'A wise elder',
    entityType: 'npc' as const,
    visibility: 2,
    imageUrl: 'https://example.com/elder.jpg',
    attributes: {},
    campaignId: 'campaign-123',
    ownerId: 'user-123',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 'enemy-1',
    name: 'Dark Dragon',
    description: 'A fearsome dragon',
    entityType: 'enemy' as const,
    visibility: 2,
    imageUrl: 'https://example.com/dragon.jpg',
    attributes: { health: 500 },
    campaignId: 'campaign-123',
    ownerId: 'user-123',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
];

describe('GalleryPage', () => {
  const mockOnNavigate = vi.fn();
  const mockOnLogout = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    
    // Default mock setup
    mockUseCampaign.mockReturnValue({
      campaigns: [mockCampaign],
      activeCampaign: mockCampaign,
      activeCampaignId: mockCampaign.id,
      isLoading: false,
      error: null,
      isActiveCampaignMaster: true,
      fetchCampaigns: vi.fn(),
      selectCampaign: vi.fn(),
      clearActiveCampaign: vi.fn(),
      createCampaign: vi.fn(),
      joinCampaign: vi.fn(),
      leaveCampaign: vi.fn(),
      deleteCampaign: vi.fn(),
      clearError: vi.fn(),
    });
    
    mockEntityService.getByCampaign.mockResolvedValue(mockEntities);
    mockEntityService.delete.mockResolvedValue(undefined);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Rendering', () => {
    it('renders the gallery page with title', async () => {
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      expect(screen.getByText('Galería de Creaciones')).toBeInTheDocument();
    });

    it('renders category navigation', async () => {
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      // Check category tabs exist
      const tablist = screen.getByRole('tablist', { name: /categorías de entidades/i });
      expect(tablist).toBeInTheDocument();
      
      const tabs = within(tablist).getAllByRole('tab');
      expect(tabs.length).toBe(7); // 7 categories
    });

    it('shows loading state while fetching entities', async () => {
      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        isLoading: true,
      });

      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      expect(screen.getByText(/recuperando registros/i)).toBeInTheDocument();
    });

    it('shows no campaign message when no active campaign', async () => {
      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        activeCampaign: null,
        activeCampaignId: null,
      });

      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      expect(screen.getByText(/sin campaña seleccionada/i)).toBeInTheDocument();
    });
  });

  describe('Accessibility - ARIA Attributes', () => {
    it('has proper tablist and tab roles for category navigation', async () => {
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      const tablist = screen.getByRole('tablist');
      expect(tablist).toHaveAttribute('aria-label', 'Categorías de entidades');

      const tabs = screen.getAllByRole('tab');
      expect(tabs.length).toBeGreaterThan(0);
      
      // First tab (character) should be selected by default
      const characterTab = tabs.find(tab => tab.textContent?.includes('PERSONAJES'));
      expect(characterTab).toHaveAttribute('aria-selected', 'true');
    });

    it('has proper grid role for entity container', async () => {
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      await waitFor(() => {
        const grid = screen.getByRole('grid');
        expect(grid).toBeInTheDocument();
        expect(grid).toHaveAttribute('aria-label');
      });
    });

    it('has gridcell role for entity cards', async () => {
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      await waitFor(() => {
        const gridcells = screen.getAllByRole('gridcell');
        expect(gridcells.length).toBeGreaterThan(0);
      });
    });

    it('has live region for announcements', async () => {
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      const liveRegion = screen.getByRole('status');
      expect(liveRegion).toHaveAttribute('aria-live', 'polite');
      expect(liveRegion).toHaveAttribute('aria-atomic', 'true');
    });

    it('campaign selector button has aria-expanded and aria-haspopup', async () => {
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      const campaignButton = screen.getByRole('button', { name: /seleccionar campaña/i });
      expect(campaignButton).toHaveAttribute('aria-expanded', 'false');
      expect(campaignButton).toHaveAttribute('aria-haspopup', 'dialog');
    });
  });

  describe('Accessibility - Keyboard Navigation', () => {
    it('allows Tab navigation between interactive elements', async () => {
      const user = userEvent.setup();
      
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      // Tab should move focus to campaign selector first (in header actions)
      await user.tab();
      expect(screen.getByRole('button', { name: /seleccionar campaña/i })).toHaveFocus();
    });

    it('allows arrow key navigation in category tabs', async () => {
      const user = userEvent.setup();
      
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      // Focus on the first (selected) tab
      const tabs = screen.getAllByRole('tab');
      const selectedTab = tabs.find(tab => tab.getAttribute('aria-selected') === 'true');
      selectedTab?.focus();
      
      expect(selectedTab).toHaveFocus();

      // Press ArrowDown to move to next tab
      await user.keyboard('{ArrowDown}');
      
      // The next tab should receive focus
      await waitFor(() => {
        const newFocusedTab = document.activeElement;
        expect(newFocusedTab).toHaveAttribute('role', 'tab');
        expect(newFocusedTab).not.toBe(selectedTab);
      });
    });

    it('handles Home key to jump to first category', async () => {
      const user = userEvent.setup();
      
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      const tabs = screen.getAllByRole('tab');
      // Focus on a tab that's not first
      tabs[3]?.focus();

      await user.keyboard('{Home}');

      await waitFor(() => {
        expect(tabs[0]).toHaveFocus();
      });
    });

    it('handles End key to jump to last category', async () => {
      const user = userEvent.setup();
      
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      const tabs = screen.getAllByRole('tab');
      tabs[0]?.focus();

      await user.keyboard('{End}');

      await waitFor(() => {
        expect(tabs[tabs.length - 1]).toHaveFocus();
      });
    });

    it('handles Escape key to close dialogs', async () => {
      const user = userEvent.setup();
      
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      // Open campaign selector
      const campaignButton = screen.getByRole('button', { name: /seleccionar campaña/i });
      await user.click(campaignButton);

      // Dialog should be open
      expect(screen.getByRole('dialog', { name: /selector de campaña/i })).toBeInTheDocument();

      // Press Escape
      await user.keyboard('{Escape}');

      // Dialog should be closed
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: /selector de campaña/i })).not.toBeInTheDocument();
      });
    });

    it('handles Enter/Space on entity cards', async () => {
      const user = userEvent.setup();
      
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      await waitFor(() => {
        const gridcells = screen.getAllByRole('gridcell');
        expect(gridcells.length).toBeGreaterThan(0);
      });

      const gridcells = screen.getAllByRole('gridcell');
      gridcells[0].focus();

      await user.keyboard('{Enter}');

      // Detail panel should appear (check DOM directly since it's hidden on smaller screens)
      await waitFor(() => {
        const detailPanel = document.querySelector('[role="complementary"]');
        expect(detailPanel).toBeInTheDocument();
      });
    });
  });

  describe('Category Filtering', () => {
    it('filters entities by selected category', async () => {
      const user = userEvent.setup();
      
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      // Wait for entities to load
      await waitFor(() => {
        expect(mockEntityService.getByCampaign).toHaveBeenCalled();
      });

      // Default is 'character' - should show 2 character entities
      await waitFor(() => {
        const gridcells = screen.getAllByRole('gridcell');
        // 2 characters + 1 "add new" card (if master)
        expect(gridcells.length).toBeGreaterThanOrEqual(2);
      });

      // Click on 'enemy' category
      const enemyTab = screen.getByRole('tab', { name: /enemigos/i });
      await user.click(enemyTab);

      // Wait for transition
      await waitFor(() => {
        const gridcells = screen.getAllByRole('gridcell');
        // Should show 1 enemy + add card
        expect(gridcells.some(cell => cell.getAttribute('aria-label')?.includes('Dark Dragon'))).toBe(true);
      }, { timeout: 2000 });
    });

    it('announces category change to screen readers', async () => {
      const user = userEvent.setup();
      
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      await waitFor(() => {
        expect(mockEntityService.getByCampaign).toHaveBeenCalled();
      });

      const liveRegion = screen.getByRole('status');
      
      // Click on 'npc' category
      const npcTab = screen.getByRole('tab', { name: /actores/i });
      await user.click(npcTab);

      // Live region should announce the change
      await waitFor(() => {
        expect(liveRegion.textContent).toMatch(/categoría|cambiando/i);
      }, { timeout: 1500 });
    });
  });

  describe('Entity Selection', () => {
    it('shows detail panel when entity is selected', async () => {
      const user = userEvent.setup();
      
      // Mock window width for lg breakpoint (detail panel is hidden lg:flex)
      Object.defineProperty(window, 'innerWidth', { value: 1200, writable: true });
      
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      await waitFor(() => {
        const gridcells = screen.getAllByRole('gridcell');
        expect(gridcells.length).toBeGreaterThan(0);
      });

      const entityCard = screen.getByRole('gridcell', { name: /hero character/i });
      await user.click(entityCard);

      // The detail panel uses hidden lg:flex, so we check the component renders
      // In jsdom we can't test CSS media queries properly, so check for the complementary role
      await waitFor(() => {
        // The element is rendered but may be hidden via CSS
        const detailPanel = document.querySelector('[role="complementary"]');
        expect(detailPanel).toBeInTheDocument();
        expect(detailPanel).toHaveAttribute('aria-label', 'Detalles de Hero Character');
      });
    });

    it('closes detail panel with close button', async () => {
      const user = userEvent.setup();
      
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      await waitFor(() => {
        const gridcells = screen.getAllByRole('gridcell');
        expect(gridcells.length).toBeGreaterThan(0);
      });

      // Select entity
      const entityCard = screen.getByRole('gridcell', { name: /hero character/i });
      await user.click(entityCard);

      await waitFor(() => {
        const detailPanel = document.querySelector('[role="complementary"]');
        expect(detailPanel).toBeInTheDocument();
      });

      // Close panel
      const closeButton = screen.getByRole('button', { name: /cerrar panel de detalles/i });
      await user.click(closeButton);

      await waitFor(() => {
        expect(document.querySelector('[role="complementary"]')).not.toBeInTheDocument();
      });
    });

    it('announces entity selection to screen readers', async () => {
      const user = userEvent.setup();
      
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      await waitFor(() => {
        const gridcells = screen.getAllByRole('gridcell');
        expect(gridcells.length).toBeGreaterThan(0);
      });

      const liveRegion = screen.getByRole('status');
      
      const entityCard = screen.getByRole('gridcell', { name: /hero character/i });
      await user.click(entityCard);

      await waitFor(() => {
        expect(liveRegion.textContent).toMatch(/seleccionada|hero character/i);
      });
    });
  });

  describe('Campaign Selector', () => {
    it('opens campaign selector dialog', async () => {
      const user = userEvent.setup();
      
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      const campaignButton = screen.getByRole('button', { name: /seleccionar campaña/i });
      await user.click(campaignButton);

      expect(screen.getByRole('dialog', { name: /selector de campaña/i })).toBeInTheDocument();
      expect(campaignButton).toHaveAttribute('aria-expanded', 'true');
    });

    it('displays available campaigns in selector', async () => {
      const user = userEvent.setup();
      
      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      const campaignButton = screen.getByRole('button', { name: /seleccionar campaña/i });
      await user.click(campaignButton);

      const dialog = screen.getByRole('dialog');
      expect(within(dialog).getByText('Test Campaign')).toBeInTheDocument();
    });
  });

  describe('Master Actions', () => {
    it('shows invite button for masters', async () => {
      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        isActiveCampaignMaster: true,
      });

      render(
        <GalleryPage 
          user={mockMasterUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      expect(screen.getByRole('button', { name: /mostrar código de invitación/i })).toBeInTheDocument();
    });

    it('shows add new card for masters', async () => {
      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        isActiveCampaignMaster: true,
      });

      render(
        <GalleryPage 
          user={mockMasterUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      await waitFor(() => {
        const addButton = screen.getByRole('button', { name: /crear nueva entidad/i });
        expect(addButton).toBeInTheDocument();
      });
    });

    it('hides invite button for players', async () => {
      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        isActiveCampaignMaster: false,
      });

      render(
        <GalleryPage 
          user={mockUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      expect(screen.queryByRole('button', { name: /mostrar código de invitación/i })).not.toBeInTheDocument();
    });

    it('navigates to generator when add new is clicked', async () => {
      const user = userEvent.setup();
      
      mockUseCampaign.mockReturnValue({
        ...mockUseCampaign(),
        isActiveCampaignMaster: true,
      });

      render(
        <GalleryPage 
          user={mockMasterUser} 
          onNavigate={mockOnNavigate} 
          onLogout={mockOnLogout} 
        />
      );

      await waitFor(() => {
        const addButton = screen.getByRole('button', { name: /crear nueva entidad/i });
        expect(addButton).toBeInTheDocument();
      });

      const addButton = screen.getByRole('button', { name: /crear nueva entidad/i });
      await user.click(addButton);

      expect(mockOnNavigate).toHaveBeenCalledWith(Screen.CHAR_GEN); // Default category is 'character'
    });
  });
});
