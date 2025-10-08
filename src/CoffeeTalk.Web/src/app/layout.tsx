import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Coffee-Time-Music ☕🎶",
  description: "Brew your beats and sip your style with Coffee-Time."
};

export default function RootLayout({
  children
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
