/**
 * EmptyState Component Tests
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { EmptyState } from './EmptyState';

describe('EmptyState', () => {
  it('renders with required props', () => {
    render(<EmptyState icon="inventory_2" title="No items" />);
    
    expect(screen.getByText('No items')).toBeInTheDocument();
    expect(screen.getByRole('status')).toBeInTheDocument();
    expect(screen.getByText('inventory_2', { selector: '.material-icons' })).toBeInTheDocument();
  });

  it('renders with description', () => {
    render(
      <EmptyState 
        icon="inventory_2" 
        title="No items" 
        description="Add your first item to get started"
      />
    );
    
    expect(screen.getByText('Add your first item to get started')).toBeInTheDocument();
  });

  it('renders action button when provided', () => {
    const onActionClick = vi.fn();
    render(
      <EmptyState 
        icon="inventory_2" 
        title="No items" 
        action={{ label: 'Add Item', onClick: onActionClick }}
      />
    );
    
    const button = screen.getByRole('button', { name: 'Add Item' });
    expect(button).toBeInTheDocument();
    
    fireEvent.click(button);
    expect(onActionClick).toHaveBeenCalledTimes(1);
  });

  it('renders action button with icon', () => {
    const onActionClick = vi.fn();
    render(
      <EmptyState 
        icon="inventory_2" 
        title="No items" 
        action={{ label: 'Add Item', onClick: onActionClick, icon: 'add' }}
      />
    );
    
    const button = screen.getByRole('button');
    expect(button).toBeInTheDocument();
    expect(button).toHaveTextContent('Add Item');
  });

  it('has correct accessibility attributes', () => {
    render(<EmptyState icon="inventory_2" title="No items" />);
    
    const status = screen.getByRole('status');
    expect(status).toHaveAttribute('aria-label', 'No items');
  });

  it('applies custom className', () => {
    const { container } = render(
      <EmptyState icon="inventory_2" title="No items" className="custom-class" />
    );
    
    expect(container.firstChild).toHaveClass('custom-class');
  });
});
