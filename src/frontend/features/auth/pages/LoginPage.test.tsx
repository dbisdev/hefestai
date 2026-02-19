/**
 * LoginPage Tests
 * Tests for user login functionality including:
 * - Form rendering
 * - Input handling
 * - Login submission
 * - Error handling
 * - Navigation
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { LoginPage } from './LoginPage';

// Mock useNavigate
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

// Mock AuthContext
const mockLogin = vi.fn();
const mockClearError = vi.fn();
const mockUseAuth = vi.fn();

vi.mock('@core/context/AuthContext', () => ({
  useAuth: () => mockUseAuth(),
}));

// Mock components
vi.mock('@shared/components/layout', () => ({
  GridBackground: vi.fn(() => <div data-testid="grid-background" />),
}));

vi.mock('@shared/components/ui', () => ({
  Input: ({ label, value, onChange, type, placeholder, error }: {
    label?: string;
    value?: string;
    onChange?: (e: React.ChangeEvent<HTMLInputElement>) => void;
    type?: string;
    placeholder?: string;
    error?: string;
  }) => (
    <div data-testid="input-wrapper">
      {label && <label data-testid="input-label">{label}</label>}
      <input
        data-testid="input"
        value={value}
        onChange={onChange}
        type={type}
        placeholder={placeholder}
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
      disabled={disabled || isLoading}
      type={type}
    >
      {isLoading ? 'CARGANDO...' : children}
    </button>
  ),
}));

vi.mock('@shared/components/feedback', () => ({
  ErrorMessage: ({ message }: { message: string }) => (
    <div data-testid="error-message">{message}</div>
  ),
}));

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockUseAuth.mockReturnValue({
      login: mockLogin,
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
        <LoginPage />
      </BrowserRouter>
    );
  };

  describe('Rendering', () => {
    it('renders the login page with title', () => {
      renderComponent();

      expect(screen.getByText('HefestAI')).toBeInTheDocument();
    });

    it('renders email input field', () => {
      renderComponent();

      const labels = screen.getAllByTestId('input-label');
      expect(labels.some(l => l.textContent === 'Identificador de Usuario')).toBe(true);
    });

    it('renders password input field', () => {
      renderComponent();

      const labels = screen.getAllByTestId('input-label');
      expect(labels.some(l => l.textContent === 'Contraseña')).toBe(true);
    });

    it('renders submit button', () => {
      renderComponent();

      expect(screen.getByTestId('button')).toBeInTheDocument();
    });

    it('renders back to home button', () => {
      renderComponent();

      expect(screen.getByText('VOLVER_AL_INICIO')).toBeInTheDocument();
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

    it('updates password state when typing', async () => {
      const user = userEvent.setup();
      renderComponent();

      const inputs = screen.getAllByTestId('input');
      const passwordInput = inputs[1];
      await user.type(passwordInput, 'password123');

      expect(passwordInput).toHaveValue('password123');
    });
  });

  describe('Form Submission', () => {
    it('calls login function when form is submitted', async () => {
      const user = userEvent.setup();
      mockLogin.mockResolvedValue({ success: true });

      renderComponent();

      // Fill in the form
      const inputs = screen.getAllByTestId('input');
      await user.type(inputs[0], 'test@example.com');
      await user.type(inputs[1], 'password123');

      // Submit the form
      const button = screen.getByTestId('button');
      await user.click(button);

      await waitFor(() => {
        expect(mockLogin).toHaveBeenCalledWith({
          email: 'test@example.com',
          password: 'password123',
        });
      });
    });

    it('clears error before submission', async () => {
      const user = userEvent.setup();
      mockLogin.mockResolvedValue({ success: true });

      mockUseAuth.mockReturnValue({
        login: mockLogin,
        error: 'Previous error',
        isLoading: false,
        clearError: mockClearError,
      });

      renderComponent();

      const inputs = screen.getAllByTestId('input');
      await user.type(inputs[0], 'test@example.com');
      await user.type(inputs[1], 'password123');

      const button = screen.getByTestId('button');
      await user.click(button);

      await waitFor(() => {
        expect(mockClearError).toHaveBeenCalled();
      });
    });
  });

  describe('Loading State', () => {
    it('disables button when loading', () => {
      mockUseAuth.mockReturnValue({
        login: mockLogin,
        error: null,
        isLoading: true,
        clearError: mockClearError,
      });

      renderComponent();

      const button = screen.getByTestId('button');
      expect(button).toBeDisabled();
    });

    it('shows loading indicator when loading', () => {
      mockUseAuth.mockReturnValue({
        login: mockLogin,
        error: null,
        isLoading: true,
        clearError: mockClearError,
      });

      renderComponent();

      expect(screen.getByText(/CARGANDO/i)).toBeInTheDocument();
    });
  });

  describe('Error Handling', () => {
    it('displays error message when auth error exists', () => {
      mockUseAuth.mockReturnValue({
        login: mockLogin,
        error: 'Invalid credentials',
        isLoading: false,
        clearError: mockClearError,
      });

      renderComponent();

      expect(screen.getByTestId('error-message')).toHaveTextContent('Invalid credentials');
    });
  });

  describe('Navigation', () => {
    it('navigates to home when back button is clicked', async () => {
      const user = userEvent.setup();
      renderComponent();

      const backButton = screen.getByText('VOLVER_AL_INICIO');
      await user.click(backButton);

      expect(mockNavigate).toHaveBeenCalledWith('/');
    });
  });
});
