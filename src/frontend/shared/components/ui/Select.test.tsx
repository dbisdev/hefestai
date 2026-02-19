/**
 * Select Component Tests
 * Tests for terminal-styled select dropdown
 */
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Select } from './Select';

const mockOptions = [
  { value: 'option1', label: 'Option 1' },
  { value: 'option2', label: 'Option 2' },
  { value: 'option3', label: 'Option 3' },
];

describe('Select', () => {
  describe('Rendering', () => {
    it('renders select element', () => {
      render(<Select options={mockOptions} />);
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    it('renders label when provided', () => {
      render(<Select options={mockOptions} label="Choose Option" />);
      expect(screen.getByText('Choose Option')).toBeInTheDocument();
    });

    it('renders all options', () => {
      render(<Select options={mockOptions} />);
      expect(screen.getByText('Option 1')).toBeInTheDocument();
      expect(screen.getByText('Option 2')).toBeInTheDocument();
      expect(screen.getByText('Option 3')).toBeInTheDocument();
    });

    it('renders placeholder option when provided', () => {
      render(<Select options={mockOptions} placeholder="Select an option" />);
      expect(screen.getByText('Select an option')).toBeInTheDocument();
    });

    it('renders icon when provided', () => {
      render(<Select options={mockOptions} label="Select" icon="list" />);
      const label = screen.getByText('Select');
      expect(label.querySelector('.material-icons')).toHaveTextContent('list');
    });

    it('applies custom className', () => {
      render(<Select options={mockOptions} className="custom-class" />);
      const select = screen.getByRole('combobox');
      expect(select.className).toContain('custom-class');
    });
  });

  describe('Error State', () => {
    it('renders error message when provided', () => {
      render(<Select options={mockOptions} error="Please select an option" />);
      expect(screen.getByText('Please select an option')).toBeInTheDocument();
    });

    it('applies error classes to select', () => {
      render(<Select options={mockOptions} error="Error" />);
      const select = screen.getByRole('combobox');
      expect(select.className).toContain('border-danger');
    });
  });

  describe('Value Handling', () => {
    it('displays controlled value', () => {
      render(<Select options={mockOptions} value="option2" onChange={() => {}} />);
      expect(screen.getByDisplayValue('Option 2')).toBeInTheDocument();
    });

    it('calls onChange when selection changes', async () => {
      const handleChange = vi.fn();
      const user = userEvent.setup();
      
      render(<Select options={mockOptions} onChange={handleChange} />);
      
      await user.selectOptions(screen.getByRole('combobox'), 'option2');
      expect(handleChange).toHaveBeenCalled();
    });
  });

  describe('Accessibility', () => {
    it('associates label with select using id', () => {
      render(<Select id="select-input" label="Choose" options={mockOptions} />);
      const select = screen.getByLabelText('Choose');
      expect(select).toHaveAttribute('id', 'select-input');
    });

    it('uses name as id when id not provided', () => {
      render(<Select name="choice" label="Choose" options={mockOptions} />);
      const select = screen.getByLabelText('Choose');
      expect(select).toHaveAttribute('name', 'choice');
    });
  });

  describe('Disabled State', () => {
    it('is disabled when disabled prop is true', () => {
      render(<Select options={mockOptions} disabled />);
      expect(screen.getByRole('combobox')).toBeDisabled();
    });

    it('applies disabled classes', () => {
      render(<Select options={mockOptions} disabled />);
      const select = screen.getByRole('combobox');
      expect(select.className).toContain('disabled:opacity-50');
    });
  });

  describe('Required Attribute', () => {
    it('has required attribute when required prop is true', () => {
      render(<Select options={mockOptions} required />);
      expect(screen.getByRole('combobox')).toBeRequired();
    });
  });
});
