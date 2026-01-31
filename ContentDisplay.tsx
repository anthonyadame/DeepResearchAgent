import type { AgentState } from '../types';
import { ProgressBar } from './ProgressBar';

interface Props {
  state: AgentState;
  loading: boolean;
  onContinue: () => void;
  currentStep: number;
}

export function ContentDisplay({
  state,
  loading,
  onContinue,
  currentStep,
}: Props) {
  return (
    <div className="content-container">
      <ProgressBar currentStep={currentStep} totalSteps={5} />

      <div className="content-card">
        {currentStep === 2 && (
          <section>
            <h2>📋 Research Brief</h2>
            <div className="content-preview">
              {truncate(state.researchBrief || '', 400)}
            </div>
          </section>
        )}

        {currentStep === 3 && (
          <section>
            <h2>📄 Initial Draft</h2>
            <div className="content-preview">
              {truncate(state.draftReport || '', 400)}
            </div>
          </section>
        )}

        {currentStep === 4 && (
          <section>
            <h2>🔍 Refined Findings</h2>
            <ul className="findings-list">
              {(state.rawNotes || []).slice(0, 3).map((note, i) => (
                <li key={i}>{note}</li>
              ))}
            </ul>
          </section>
        )}

        {currentStep === 5 && (
          <section>
            <h2>✨ Final Report</h2>
            <div className="final-report">
              {state.finalReport || 'Generating...'}
            </div>
          </section>
        )}
      </div>

      {currentStep < 5 && (
        <button
          onClick={onContinue}
          disabled={loading}
          className="btn-primary btn-continue"
        >
          {loading ? '⏳ Processing...' : '▶ Continue'}
        </button>
      )}

      {currentStep === 5 && (
        <div className="completion-badge">
          ✓ Research Complete!
        </div>
      )}
    </div>
  );
}

function truncate(text: string, length: number): string {
  return text.length > length ? text.substring(0, length) + '...' : text;
}