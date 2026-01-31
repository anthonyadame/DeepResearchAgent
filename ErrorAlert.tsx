interface Props {
  message: string;
  onRetry: () => void;
  onDismiss?: () => void;
}

export function ErrorAlert({ message, onRetry, onDismiss }: Props) {
  return (
    <div className="error-container">
      <div className="error-alert">
        <div className="error-icon">⚠️</div>
        <div className="error-content">
          <h3>Error</h3>
          <p>{message}</p>
        </div>
        <div className="error-actions">
          <button onClick={onRetry} className="btn-retry">
            🔄 Retry
          </button>
          {onDismiss && (
            <button onClick={onDismiss} className="btn-dismiss">
              ✕ Dismiss
            </button>
          )}
        </div>
      </div>
    </div>
  );
}