// API Configuration
export const API_BASE_URL = (
  process.env.NEXT_PUBLIC_API_BASE_URL ?? ""
).replace(/\/$/, "");

// Default Values
export const DEFAULT_QUOTA = 5;

// Validation Constants
export const USERNAME_MIN_LENGTH = 3;
export const USERNAME_MAX_LENGTH = 20;
export const COFFEE_BAR_CODE_LENGTH = 6;

// Common Placeholders
export const DEFAULT_USERNAME_PLACEHOLDER = "DJ Espresso";

// Request Headers
export const JSON_HEADERS = { "Content-Type": "application/json" };

// HTTP Methods
export const HTTP_METHODS = {
  GET: "GET",
  POST: "POST",
  PUT: "PUT",
  DELETE: "DELETE",
} as const;
