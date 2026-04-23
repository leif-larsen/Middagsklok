import type { Metadata } from "next";
import { Geist_Mono, Sora } from "next/font/google";
import DishesMetadataProvider from "./components/DishesMetadataProvider";
import IngredientsMetadataProvider from "./components/IngredientsMetadataProvider";
import IngredientsProvider from "./components/IngredientsProvider";
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
  title: "Middagsklok",
  description: "Planlegg og lag mat med selvtillit.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="nb">
      <body className={`${sora.variable} ${geistMono.variable} antialiased`}>
        <IngredientsProvider>
          <DishesMetadataProvider>
            <IngredientsMetadataProvider>
              {children}
            </IngredientsMetadataProvider>
          </DishesMetadataProvider>
        </IngredientsProvider>
      </body>
    </html>
  );
}
