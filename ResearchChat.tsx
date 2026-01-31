import { useState } from 'react';
import type { AgentState } from '../types';
import { executeStep } from '../services/chatService';
import { useResearchWorkflow } from '../hooks/useResearchWorkflow';
import { QueryInput } from './QueryInput';
import { ClarificationDialog } from './ClarificationDialog';
import { ContentDisplay } from './ContentDisplay';
import { ErrorAlert } from './ErrorAlert';

export function ResearchChat() {
  const { state, setState, reset } = useResearchWorkflow();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>();

  const currentStep = determineCurrentStep(state);
  const isClarificationNeeded =
    state.needsQualityRepair && state.researchBrief?.includes('Clarification needed');

  const handleSubmitQuery = async (query: string) => {
    setError(undefined);
    const newState: AgentState = {
      messages: [{ role: 'user', content: query }],
      supervisorMessages: [],
      rawNotes: [],
      needsQualityRepair: true,
    };
    setState(newState);
    await executeStepHandler(newState);
  };

  const handleProvideClarification = async (clarification: string) => {
    setError(undefined);
    const updatedState = {
      ...state,
      messages: [{ role: 'user', content: clarification }],
    };
    setState(updatedState);
    await executeStepHandler(updatedState, clarification);
  };

  const handleContinue = async () => {
    setError(undefined);
    await executeStepHandler(state);
  };

  const executeStepHandler = async (
    currentState: AgentState,
    userResponse?: string
  ) => {
    setLoading(true);
    try {
      const response = await executeStep({
        currentState,
        userResponse,
      });

      setState(response.updatedState);

      if (response.statusMessage.includes('error')) {
        setError(response.statusMessage);
      }
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : 'An unexpected error occurred';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="research-chat-app">
      {error && (
        <ErrorAlert
          message={error}
          onRetry={() => handleContinue()}
          onDismiss={() => setError(undefined)}
        />
      )}

      {!state.researchBrief && (
        <QueryInput onSubmit={handleSubmitQuery} disabled={loading} />
      )}

      {isClarificationNeeded && (
        <ClarificationDialog
          question={extractQuestion(state.researchBrief)}
          onSubmit={handleProvideClarification}
          disabled={loading}
        />
      )}

      {state.researchBrief && !isClarificationNeeded && (
        <>
          <ContentDisplay
            state={state}
            loading={loading}
            onContinue={handleContinue}
            currentStep={currentStep}
          />

          {currentStep === 5 && (
            <button onClick={reset} className="btn-secondary btn-new-research">
              🔄 Start New Research
            </button>
          )}
        </>
      )}
    </div>
  );
}

function determineCurrentStep(state: AgentState): number {
  if (!state.researchBrief) return 0;
  if (!state.draftReport) return 2;
  if (!state.supervisorMessages?.length) return 3;
  if (!state.finalReport) return 4;
  return 5;
}

function extractQuestion(brief?: string): string {
  if (!brief) return '';
  const prefix = 'Clarification needed: ';
  return brief.startsWith(prefix) ? brief.substring(prefix.length) : brief;
}