import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import { RootAuthShell } from "@/components/layout/RootAuthShell";
import { AuthProvider } from "@/features/auth";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "CLS Budget App",
  description: "Personal budget management",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body
        className={`${geistSans.variable} ${geistMono.variable} min-h-screen bg-[var(--background)] text-[var(--foreground)] antialiased`}
      >
        <AuthProvider>
          <RootAuthShell>{children}</RootAuthShell>
        </AuthProvider>
      </body>
    </html>
  );
}
