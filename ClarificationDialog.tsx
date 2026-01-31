import { FormEvent, useState } from 'react';

interface Props {
  question: string;
  onSubmit: (response: string) => void;
  disabled: boolean;
}

export function ClarificationDialog({ question, onSubmit, disabled }: Props) {
  const [input, setInput] = useState('');

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (input.trim()) {
      onSubmit(input);
      setInput('');
    }
  };

  return (
    <div className="clarification-container">
      <div className="clarification-card">
        <div className="clarification-icon">❓</div>
        <h2>Clarification Needed</h2>
        <p className="clarification-question">{question}</p>

        <form onSubmit={handleSubmit} className="clarification-form">
          <textarea
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder="Please provide more details..."
            disabled={disabled}
            rows={3}
            className="clarification-input"
          />
          <button
            type="submit"
            disabled={disabled || !input.trim()}
            className="btn-primary"
          >
            {disabled ? 'Processing...' : 'Submit'}
          </button>
        </form>
      </div>
    </div>
  );
}