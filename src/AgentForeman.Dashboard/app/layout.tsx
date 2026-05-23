import type { Metadata } from "next";
import type { ReactNode } from "react";
import { AppShell } from "@/components/app-shell";
import "./globals.css";

export const metadata: Metadata = {
  title: "Agent Foreman Dashboard",
  description: "Read-only mission control shell for Agent Foreman.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: ReactNode;
}>) {
  return (
    <html lang="en" className="h-full antialiased">
      <body className="min-h-full font-sans text-[var(--foreground)]">
        <AppShell>{children}</AppShell>
      </body>
    </html>
  );
}
