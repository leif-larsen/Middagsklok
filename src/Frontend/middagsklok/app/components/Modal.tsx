"use client";

import { useEffect, type ReactNode } from "react";

type ModalProps = {
  isOpen: boolean;
  title: string;
  description?: string;
  children: ReactNode;
  footer?: ReactNode;
  onClose: () => void;
  maxWidthClassName?: string;
};

export default function Modal({
  isOpen,
  title,
  description,
  children,
  footer,
  onClose,
  maxWidthClassName = "max-w-xl",
}: ModalProps) {
  useEffect(() => {
    if (!isOpen) {
      return undefined;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key !== "Escape") {
        return;
      }

      event.preventDefault();
      onClose();
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [isOpen, onClose]);

  if (!isOpen) {
    return null;
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/45 px-4 py-10"
      role="dialog"
      aria-modal="true"
      aria-labelledby="modal-title"
      aria-describedby={description ? "modal-description" : undefined}
    >
      <div
        className={`w-full ${maxWidthClassName} rounded-[22px] border-2 border-[#2d69ff] bg-white px-6 py-5 shadow-[0_24px_60px_-28px_rgba(20,32,24,0.6)]`}
      >
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2
              id="modal-title"
              className="text-lg font-semibold text-[#1f2a22]"
            >
              {title}
            </h2>
            {description ? (
              <p
                id="modal-description"
                className="mt-1 text-sm text-[#6b7a71]"
              >
                {description}
              </p>
            ) : null}
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close"
            className="grid h-9 w-9 place-items-center rounded-full border border-[#e4e9e0] text-[#6d7b72] transition hover:bg-[#f5f7f3]"
          >
            <CloseIcon className="h-4 w-4" />
          </button>
        </div>

        {children}

        {footer ? (
          <div className="mt-6 flex items-center justify-end gap-3">
            {footer}
          </div>
        ) : null}
      </div>
    </div>
  );
}

function CloseIcon({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M6 6l12 12M18 6l-12 12" />
    </svg>
  );
}
