import type { Metadata } from "next";
import { Geist_Mono, Sora } from "next/font/google";
import IngredientsMetadataProvider from "./components/IngredientsMetadataProvider";
import "./globals.css";

const sora = Sora({
  variable: "--font-sora",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Meal Planner",
  description: "Plan & cook with confidence.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className={`${sora.variable} ${geistMono.variable} antialiased`}>
        <IngredientsMetadataProvider>
          {children}
        </IngredientsMetadataProvider>
      </body>
    </html>
  );
}
