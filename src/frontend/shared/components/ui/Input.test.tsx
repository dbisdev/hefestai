/**
 * Input Component Tests
 * Tests for terminal-styled input field
 */
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Input } from './Input';

describe('Input', () => {
  describe('Rendering', () => {
    it('renders input element', () => {
      render(<Input />);
      expect(screen.getByRole('textbox')).toBeInTheDocument();
    });

    it('renders label when provided', () => {
      render(<Input label="Email" />);
      expect(screen.getByText('Email')).toBeInTheDocument();
    });

    it('renders placeholder when provided', () => {
      render(<Input placeholder="Enter email" />);
      expect(screen.getByPlaceholderText('Enter email')).toBeInTheDocument();
    });

    it('renders icon when provided', () => {
      render(<Input label="Email" icon="email" />);
      const label = screen.getByText('Email');
      expect(label.querySelector('.material-icons')).toHaveTextContent('email');
    });

    it('applies custom className', () => {
      render(<Input className="custom-class" />);
      const input = screen.getByRole('textbox');
      expect(input.className).toContain('custom-class');
    });
  });

  describe('Error State', () => {
    it('renders error message when provided', () => {
      render(<Input error="Invalid input" />);
      expect(screen.getByText('Invalid input')).toBeInTheDocument();
    });

    it('applies error classes to input', () => {
      render(<Input error="Invalid input" />);
      const input = screen.getByRole('textbox');
      expect(input.className).toContain('border-danger');
    });
  });

  describe('Input Types', () => {
    it('renders input element', () => {
      render(<Input />);
      expect(screen.getByRole('textbox')).toBeInTheDocument();
    });

    it('renders email input', () => {
      render(<Input type="email" />);
      const input = screen.getByRole('textbox');
      expect(input).toBeInTheDocument();
    });

    it('renders password input', () => {
      render(<Input type="password" />);
      const input = document.querySelector('input[type="password"]');
      expect(input).toBeInTheDocument();
    });

    it('renders number input', () => {
      render(<Input type="number" />);
      expect(screen.getByRole('spinbutton')).toBeInTheDocument();
    });
  });

  describe('Value Handling', () => {
    it('displays controlled value', () => {
      render(<Input value="test value" onChange={() => {}} />);
      expect(screen.getByDisplayValue('test value')).toBeInTheDocument();
    });

    it('calls onChange when value changes', async () => {
      const handleChange = vi.fn();
      const user = userEvent.setup();
      
      render(<Input onChange={handleChange} />);
      
      await user.type(screen.getByRole('textbox'), 'test');
      expect(handleChange).toHaveBeenCalledTimes(4);
    });
  });

  describe('Accessibility', () => {
    it('associates label with input using id', () => {
      render(<Input id="email-input" label="Email" />);
      const input = screen.getByLabelText('Email');
      expect(input).toHaveAttribute('id', 'email-input');
    });

    it('uses name as id when id not provided', () => {
      render(<Input name="password" label="Password" type="password" />);
      const input = screen.getByLabelText('Password');
      expect(input).toHaveAttribute('name', 'password');
    });
  });

  describe('Disabled State', () => {
    it('is disabled when disabled prop is true', () => {
      render(<Input disabled />);
      expect(screen.getByRole('textbox')).toBeDisabled();
    });

    it('applies disabled classes', () => {
      render(<Input disabled />);
      const input = screen.getByRole('textbox');
      expect(input.className).toContain('disabled:opacity-50');
    });
  });

  describe('Required Attribute', () => {
    it('has required attribute when required prop is true', () => {
      render(<Input required />);
      expect(screen.getByRole('textbox')).toBeRequired();
    });
  });

  describe('AutoComplete', () => {
    it('applies autoComplete attribute', () => {
      render(<Input autoComplete="email" />);
      expect(screen.getByRole('textbox')).toHaveAttribute('autocomplete', 'email');
    });
  });
});
