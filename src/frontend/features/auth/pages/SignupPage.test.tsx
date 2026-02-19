/**
 * SignupPage Tests
 * Tests for user registration functionality including:
 * - Form rendering
 * - Role selection
 * - Input handling
 * - Validation
 * - Registration submission
 * - Navigation
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { SignupPage } from './SignupPage';

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

const mockRegister = vi.fn();
const mockClearError = vi.fn();
const mockUseAuth = vi.fn();

vi.mock('@core/context/AuthContext', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('@shared/components/layout', () => ({
  GridBackground: vi.fn(() => <div data-testid="grid-background" />),
}));

vi.mock('@shared/components/ui', () => ({
  Input: ({ label, value, onChange, type, placeholder, error, required }: {
    label?: string;
    value?: string;
    onChange?: (e: React.ChangeEvent<HTMLInputElement>) => void;
    type?: string;
    placeholder?: string;
    error?: string;
    required?: boolean;
  }) => (
    <div data-testid="input-wrapper">
      {label && <label data-testid="input-label">{label}</label>}
      <input
        data-testid="input"
        value={value}
        onChange={onChange}
        type={type}
        placeholder={placeholder}
        required={required}
      />
      {error && <span data-testid="input-error">{error}</span>}
    </div>
  ),
  Button: ({ children, onClick, variant, disabled, type, isLoading }: {
    children: React.ReactNode;
    onClick?: () => void;
    variant?: string;
    disabled?: boolean;
    type?: 'button' | 'submit';
    isLoading?: boolean;
  }) => (
    <button
      data-testid="button"
      onClick={onClick}
      data-variant={variant}
      disabled={disabled}
      type={type}
    >
      {isLoading ? 'LOADING...' : children}
    </button>
  ),
}));

vi.mock('@shared/components/feedback', () => ({
  ErrorMessage: ({ message }: { message: string }) => (
    <div data-testid="error-message">{message}</div>
  ),
}));

vi.mock('@core/utils', () => ({
  validateEmail: vi.fn((email: string) => ({
    isValid: email.includes('@'),
    errors: email.includes('@') ? [] : ['Invalid email'],
  })),
  validatePassword: vi.fn((password: string) => ({
    isValid: password.length >= 8,
    errors: password.length >= 8 ? [] : ['Password too short'],
  })),
  validateDisplayName: vi.fn((name: string) => ({
    isValid: name.length >= 2,
    errors: name.length >= 2 ? [] : ['Name too short'],
  })),
  validateInviteCode: vi.fn((code: string) => ({
    isValid: code.length > 0,
    errors: code.length > 0 ? [] : ['Invalid code'],
  })),
}));

describe('SignupPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockUseAuth.mockReturnValue({
      register: mockRegister,
      error: null,
      isLoading: false,
      clearError: mockClearError,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const renderComponent = () => {
    return render(
      <BrowserRouter>
        <SignupPage />
      </BrowserRouter>
    );
  };

  describe('Rendering', () => {
    it('renders the signup page with title', () => {
      renderComponent();

      expect(screen.getByText('HefestAI')).toBeInTheDocument();
    });

    it('renders role selection buttons', () => {
      renderComponent();

      expect(screen.getByText('Jugador (Player)')).toBeInTheDocument();
      expect(screen.getByText('Maestro (Master)')).toBeInTheDocument();
    });

    it('renders email input field', () => {
      renderComponent();

      const labels = screen.getAllByTestId('input-label');
      expect(labels.some(l => l.textContent === 'Correo Electrónico')).toBe(true);
    });

    it('renders password input fields', () => {
      renderComponent();

      const inputs = screen.getAllByTestId('input');
      expect(inputs.length).toBeGreaterThanOrEqual(2);
    });

    it('renders display name input field', () => {
      renderComponent();

      const labels = screen.getAllByTestId('input-label');
      expect(labels.some(l => l.textContent === 'Nombre de Operativo')).toBe(true);
    });

    it('renders submit button', () => {
      renderComponent();

      expect(screen.getByText('INICIALIZAR_PERFIL')).toBeInTheDocument();
    });

    it('renders back to login button', () => {
      renderComponent();

      expect(screen.getByText('VOLVER_AL_ACCESO')).toBeInTheDocument();
    });

    it('renders back to home button', () => {
      renderComponent();

      expect(screen.getByText('VOLVER_AL_INICIO')).toBeInTheDocument();
    });
  });

  describe('Role Selection', () => {
    it('defaults to PLAYER role', () => {
      renderComponent();

      const playerButton = screen.getByText('Jugador (Player)');
      expect(playerButton).toBeInTheDocument();
    });

    it('shows invite code field for PLAYER role', () => {
      renderComponent();

      const labels = screen.getAllByTestId('input-label');
      expect(labels.some(l => l.textContent === 'Código de Invitación del Maestro')).toBe(true);
    });

    it('hides invite code field for MASTER role', async () => {
      const user = userEvent.setup();
      renderComponent();

      const masterButton = screen.getByText('Maestro (Master)');
      await user.click(masterButton);

      const labels = screen.queryAllByTestId('input-label');
      expect(labels.some(l => l.textContent === 'Código de Invitación del Maestro')).toBe(false);
    });
  });

  describe('Form Interaction', () => {
    it('updates email state when typing', async () => {
      const user = userEvent.setup();
      renderComponent();

      const inputs = screen.getAllByTestId('input');
      const emailInput = inputs[0];
      await user.type(emailInput, 'test@example.com');

      expect(emailInput).toHaveValue('test@example.com');
    });

    it('updates display name state when typing', async () => {
      const user = userEvent.setup();
      renderComponent();

      const inputs = screen.getAllByTestId('input');
      const displayNameInput = inputs[3];
      await user.type(displayNameInput, 'TestUser');

      expect(displayNameInput).toHaveValue('TestUser');
    });
  });

  describe('Form Submission', () => {
    it('validates password match', async () => {
      const user = userEvent.setup();
      mockRegister.mockResolvedValue({});

      renderComponent();

      const inputs = screen.getAllByTestId('input');
      await user.type(inputs[0], 'test@example.com');
      await user.type(inputs[1], 'Password123');
      await user.type(inputs[2], 'DifferentPassword');
      await user.type(inputs[3], 'TestUser');
      await user.type(inputs[4], 'INVITE-CODE');

      const submitButton = screen.getByText('INICIALIZAR_PERFIL');
      await user.click(submitButton);

      expect(mockRegister).not.toHaveBeenCalled();
    });

    it('calls register function when form is valid', async () => {
      const user = userEvent.setup();
      mockRegister.mockResolvedValue({});

      renderComponent();

      const inputs = screen.getAllByTestId('input');
      await user.type(inputs[0], 'test@example.com');
      await user.type(inputs[1], 'Password123');
      await user.type(inputs[2], 'Password123');
      await user.type(inputs[3], 'TestUser');
      await user.type(inputs[4], 'INVITE-CODE');

      const submitButton = screen.getByText('INICIALIZAR_PERFIL');
      await user.click(submitButton);

      await waitFor(() => {
        expect(mockRegister).toHaveBeenCalled();
      });
    });

    it('shows loading state during submission', async () => {
      const user = userEvent.setup();
      mockRegister.mockImplementation(() => new Promise(() => {}));

      renderComponent();

      const inputs = screen.getAllByTestId('input');
      await user.type(inputs[0], 'test@example.com');
      await user.type(inputs[1], 'Password123');
      await user.type(inputs[2], 'Password123');
      await user.type(inputs[3], 'TestUser');

      const masterButton = screen.getByText('Maestro (Master)');
      await user.click(masterButton);

      const submitButton = screen.getByText('INICIALIZAR_PERFIL');
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText('LOADING...')).toBeInTheDocument();
      });
    });
  });

  describe('Error Handling', () => {
    it('displays registration error', async () => {
      const user = userEvent.setup();
      mockRegister.mockRejectedValue(new Error('Email already exists'));

      renderComponent();

      const inputs = screen.getAllByTestId('input');
      await user.type(inputs[0], 'test@example.com');
      await user.type(inputs[1], 'Password123');
      await user.type(inputs[2], 'Password123');
      await user.type(inputs[3], 'TestUser');

      const masterButton = screen.getByText('Maestro (Master)');
      await user.click(masterButton);

      const submitButton = screen.getByText('INICIALIZAR_PERFIL');
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByTestId('error-message')).toHaveTextContent('Email already exists');
      });
    });
  });

  describe('Navigation', () => {
    it('navigates to login page when login link is clicked', async () => {
      const user = userEvent.setup();
      renderComponent();

      const loginLink = screen.getByText('VOLVER_AL_ACCESO');
      await user.click(loginLink);

      expect(mockNavigate).toHaveBeenCalledWith('/login');
    });

    it('navigates to home when back button is clicked', async () => {
      const user = userEvent.setup();
      renderComponent();

      const backButton = screen.getByText('VOLVER_AL_INICIO');
      await user.click(backButton);

      expect(mockNavigate).toHaveBeenCalledWith('/');
    });
  });
});
