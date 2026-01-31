import type { ChatStepRequest, ChatStepResponse } from '../types';

const API_BASE = 'http://localhost:8080/api/chat';

export async function executeStep(
  request: ChatStepRequest,
  signal?: AbortSignal
): Promise<ChatStepResponse> {
  const response = await fetch(`${API_BASE}/step`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
    signal,
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new Error(error.details || `HTTP ${response.status}: Failed to execute step`);
  }

  return response.json();
}

export async function healthCheck(): Promise<boolean> {
  try {
    const response = await fetch(`${API_BASE.replace('/chat', '')}/health`);
    return response.ok;
  } catch {
    return false;
  }
}