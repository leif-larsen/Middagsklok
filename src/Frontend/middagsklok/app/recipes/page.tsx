"use client";

import { useMemo, useState } from "react";
import Sidebar from "../components/Sidebar";

type TabKey = "instructions" | "ai";

export default function RecipesPage() {
  const [activeTab, setActiveTab] = useState<TabKey>("ai");
  const [prompt, setPrompt] = useState("");

  const showAiEmptyState = useMemo(
    () => activeTab === "ai" && prompt.trim().length === 0,
    [activeTab, prompt],
  );

  return (
    <div className="min-h-screen w-full p-6 sm:p-8">
      <div className="flex flex-wrap items-start gap-6">
        <Sidebar />
        <main className="min-w-[280px] flex-1 space-y-6">
          <header className="space-y-3">
            <div className="flex items-center gap-3">
              <span className="grid h-12 w-12 place-items-center rounded-2xl bg-[#eef4ee] text-[#2f6b4f]">
                <BookIcon className="h-5 w-5" />
              </span>
              <div>
                <h1 className="text-2xl font-semibold text-[#1f2a22]">
                  Recipes &amp; Instructions
                </h1>
                <p className="text-sm text-[#6c7a70]">
                  View detailed cooking instructions and get AI-powered suggestions
                </p>
              </div>
            </div>

            <div className="inline-flex rounded-2xl bg-[#e8ebe6] p-1">
              <button
                type="button"
                onClick={() => setActiveTab("instructions")}
                className={`min-w-48 rounded-xl px-4 py-2 text-sm font-semibold transition ${
                  activeTab === "instructions"
                    ? "bg-white text-[#1f2a22] shadow-[0_10px_20px_-16px_rgba(23,43,30,0.7)]"
                    : "text-[#4f5f55]"
                }`}
              >
                Instructions
              </button>
              <button
                type="button"
                onClick={() => setActiveTab("ai")}
                className={`min-w-48 rounded-xl border px-4 py-2 text-sm font-semibold transition ${
                  activeTab === "ai"
                    ? "border-[#4cb287] bg-white text-[#1f2a22] shadow-[0_10px_20px_-16px_rgba(23,43,30,0.7)]"
                    : "border-transparent text-[#4f5f55]"
                }`}
              >
                AI Suggestions
              </button>
            </div>
          </header>

          {activeTab === "instructions" ? (
            <section className="rounded-[18px] border border-[#e1e8dc] bg-white/80 p-6 shadow-[0_20px_48px_-36px_rgba(30,60,40,0.4)]">
              <div className="flex min-h-[420px] items-center justify-center rounded-2xl border border-dashed border-[#d7dfd2] bg-[#fafcf8] px-6 text-center text-sm text-[#6f7f74]">
                Select a dish to view detailed cooking instructions.
              </div>
            </section>
          ) : (
            <section className="rounded-[18px] border border-[#e1e8dc] bg-white/80 p-6 shadow-[0_20px_48px_-36px_rgba(30,60,40,0.4)]">
              <div className="space-y-4">
                <div className="space-y-1">
                  <div className="flex items-center gap-2 text-xl font-semibold text-[#1f2a22]">
                    <SparkleIcon className="h-5 w-5 text-[#2f6b4f]" />
                    AI Recipe Suggestions
                  </div>
                  <p className="text-sm text-[#6c7a70]">
                    Describe what you&apos;re looking for and get personalized recipe ideas
                  </p>
                </div>

                <textarea
                  value={prompt}
                  onChange={(event) => setPrompt(event.target.value)}
                  rows={5}
                  placeholder="E.g., I want a healthy vegetarian dinner that takes less than 30 minutes..."
                  className="w-full resize-none rounded-2xl border border-[#dfe7d7] bg-white px-4 py-3 text-sm text-[#1f2a22] shadow-[0_10px_20px_-18px_rgba(30,60,40,0.4)] outline-none placeholder:text-[#8a968d] focus:border-[#7fc3a5]"
                />

                <button
                  type="button"
                  className="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-[#2f6b4f] px-4 py-2.5 text-sm font-semibold text-white shadow-[0_12px_24px_-18px_rgba(32,78,54,0.9)] transition hover:bg-[#2a5c46]"
                >
                  <SparkleIcon className="h-4 w-4" />
                  Get AI Suggestions
                </button>

                <div className="rounded-2xl border border-[#e6ece2] bg-[#fcfdfb] px-4 py-14">
                  {showAiEmptyState ? (
                    <div className="flex flex-col items-center justify-center gap-4 text-center text-[#7a857d]">
                      <SparkleIcon className="h-12 w-12 text-[#b3bab3]" />
                      <p className="text-sm font-medium">
                        Enter a prompt above to get AI-powered recipe suggestions
                      </p>
                    </div>
                  ) : (
                    <div className="text-center text-sm text-[#7a857d]">
                      Suggestions will appear here once API integration is enabled.
                    </div>
                  )}
                </div>
              </div>
            </section>
          )}
        </main>
      </div>
    </div>
  );
}

type IconProps = {
  className?: string;
};

function BookIcon({ className }: IconProps) {
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
      <path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v16H6.5A2.5 2.5 0 0 0 4 21z" />
      <path d="M4 5.5v15" />
      <path d="M12 3v16" />
    </svg>
  );
}

function SparkleIcon({ className }: IconProps) {
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
      <path d="m12 3 1.6 4.4L18 9l-4.4 1.6L12 15l-1.6-4.4L6 9l4.4-1.6Z" />
      <path d="m19 4 .6 1.6 1.6.6-1.6.6L19 8.4l-.6-1.6-1.6-.6 1.6-.6Z" />
      <path d="m5 15 .5 1.2L6.8 17l-1.3.5L5 18.8l-.5-1.3L3.2 17l1.3-.8Z" />
    </svg>
  );
}
