import type { Metadata } from "next";
import "./globals.css";
import AppLayout from "@/components/app-layout";

export const metadata: Metadata = {
  title: "Middagsklok - Weekly Meal Planning",
  description: "Simple weekly meal planning application",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>
        <AppLayout>{children}</AppLayout>
      </body>
    </html>
  );
}
