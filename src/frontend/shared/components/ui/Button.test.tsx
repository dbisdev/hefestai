/**
 * Button Component Tests
 * Tests for terminal-styled button with variants
 */
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Button } from './Button';

describe('Button', () => {
  describe('Rendering', () => {
    it('renders children correctly', () => {
      render(<Button>Click Me</Button>);
      expect(screen.getByRole('button', { name: 'Click Me' })).toBeInTheDocument();
    });

    it('applies primary variant classes by default', () => {
      render(<Button>Primary</Button>);
      const button = screen.getByRole('button');
      expect(button.className).toContain('bg-primary');
    });

    it('applies secondary variant classes', () => {
      render(<Button variant="secondary">Secondary</Button>);
      const button = screen.getByRole('button');
      expect(button.className).toContain('bg-primary/10');
    });

    it('applies danger variant classes', () => {
      render(<Button variant="danger">Danger</Button>);
      const button = screen.getByRole('button');
      expect(button.className).toContain('text-danger');
    });

    it('applies ghost variant classes', () => {
      render(<Button variant="ghost">Ghost</Button>);
      const button = screen.getByRole('button');
      expect(button.className).toContain('text-primary/60');
    });

    it('applies size classes', () => {
      const { rerender } = render(<Button size="sm">Small</Button>);
      expect(screen.getByRole('button').className).toContain('h-8');

      rerender(<Button size="md">Medium</Button>);
      expect(screen.getByRole('button').className).toContain('h-10');

      rerender(<Button size="lg">Large</Button>);
      expect(screen.getByRole('button').className).toContain('h-12');
    });

    it('applies fullWidth class when enabled', () => {
      render(<Button fullWidth>Full Width</Button>);
      const button = screen.getByRole('button');
      expect(button.className).toContain('w-full');
    });

    it('renders icon when provided', () => {
      render(<Button icon="add">With Icon</Button>);
      const button = screen.getByRole('button');
      expect(button.querySelector('.material-icons')).toHaveTextContent('add');
    });
  });

  describe('Loading State', () => {
    it('shows loading spinner when isLoading is true', () => {
      render(<Button isLoading>Loading</Button>);
      const button = screen.getByRole('button');
      expect(button.querySelector('.material-icons.animate-spin')).toBeInTheDocument();
    });

    it('is disabled when loading', () => {
      render(<Button isLoading>Loading</Button>);
      expect(screen.getByRole('button')).toBeDisabled();
    });
  });

  describe('Disabled State', () => {
    it('is disabled when disabled prop is true', () => {
      render(<Button disabled>Disabled</Button>);
      expect(screen.getByRole('button')).toBeDisabled();
    });

    it('applies disabled classes', () => {
      render(<Button disabled>Disabled</Button>);
      const button = screen.getByRole('button');
      expect(button.className).toContain('disabled:opacity-50');
    });

    it('does not call onClick when disabled', async () => {
      const handleClick = vi.fn();
      const user = userEvent.setup();
      
      render(<Button disabled onClick={handleClick}>Disabled</Button>);
      
      await user.click(screen.getByRole('button'));
      expect(handleClick).not.toHaveBeenCalled();
    });
  });

  describe('Interactions', () => {
    it('calls onClick when clicked', async () => {
      const handleClick = vi.fn();
      const user = userEvent.setup();
      
      render(<Button onClick={handleClick}>Click Me</Button>);
      
      await user.click(screen.getByRole('button'));
      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('can be submitted in a form', () => {
      const handleSubmit = vi.fn((e) => e.preventDefault());
      
      render(
        <form onSubmit={handleSubmit}>
          <Button type="submit">Submit</Button>
        </form>
      );
      
      screen.getByRole('button').click();
      expect(handleSubmit).toHaveBeenCalled();
    });
  });

  describe('Custom Classes', () => {
    it('merges custom className', () => {
      render(<Button className="custom-class">Custom</Button>);
      const button = screen.getByRole('button');
      expect(button.className).toContain('custom-class');
    });
  });
});
