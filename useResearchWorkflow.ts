import { useState, useEffect } from 'react';
import type { AgentState } from '../types';

const STORAGE_KEY = 'research_workflow_state';

const INITIAL_STATE: AgentState = {
  messages: [],
  supervisorMessages: [],
  rawNotes: [],
  needsQualityRepair: true,
};

export function useResearchWorkflow() {
  const [state, setState] = useState<AgentState>(() => {
    // Load from localStorage if exists
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch {
        return INITIAL_STATE;
      }
    }
    return INITIAL_STATE;
  });

  // Auto-persist to localStorage on change
  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  }, [state]);

  const reset = () => {
    localStorage.removeItem(STORAGE_KEY);
    setState(INITIAL_STATE);
  };

  return { state, setState, reset };
}