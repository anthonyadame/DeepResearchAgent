interface Props {
  currentStep: number;
  totalSteps: number;
}

const STEP_LABELS = [
  'Start',
  'Clarify',
  'Research Brief',
  'Draft Report',
  'Refinement',
  'Final Report',
];

export function ProgressBar({ currentStep, totalSteps }: Props) {
  return (
    <div className="progress-container">
      <div className="progress-bar">
        <div
          className="progress-fill"
          style={{ width: `${(currentStep / totalSteps) * 100}%` }}
        />
      </div>
      <div className="progress-label">
        Step {currentStep} of {totalSteps}: {STEP_LABELS[currentStep]}
      </div>
    </div>
  );
}