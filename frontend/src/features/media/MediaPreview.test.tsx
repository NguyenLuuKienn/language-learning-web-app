import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { MediaPreview } from './MediaPreview';

describe('MediaPreview', () => {
  it('shows a fallback when an illustration cannot load', () => {
    render(<MediaPreview text="hello" imageUrl="/missing.png" />);
    fireEvent.error(screen.getByRole('img', { name: 'Minh họa hello' }));
    expect(screen.getByRole('status')).toHaveTextContent('Không tải được ảnh minh họa.');
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
  });
});
