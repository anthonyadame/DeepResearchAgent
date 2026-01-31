import { FormEvent, useState } from 'react';

interface Props {
  onSubmit: (query: string) => void;
  disabled: boolean;
}

export function QueryInput({ onSubmit, disabled }: Props) {
  const [query, setQuery] = useState('');

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (query.trim()) {
      onSubmit(query);
      setQuery('');
    }
  };

  return (
    <div className="query-input-container">
      <h1>Deep Research Agent</h1>
      <p>Enter your research query to begin</p>

      <form onSubmit={handleSubmit} className="query-form">
        <textarea
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="E.g., What are the effects of artificial intelligence on job markets in 2024?"
          disabled={disabled}
          rows={4}
          className="query-textarea"
        />
        <button
          type="submit"
          disabled={disabled || !query.trim()}
          className="btn-primary"
        >
          {disabled ? 'Processing...' : 'Start Research'}
        </button>
      </form>

      <div className="query-examples">
        <p>Examples:</p>
        <ul>
          <li>"What is quantum computing and how does it work?"</li>
          <li>"Analyze the impact of climate change on agriculture"</li>
          <li>"Explain blockchain technology and its applications"</li>
        </ul>
      </div>
    </div>
  );
}