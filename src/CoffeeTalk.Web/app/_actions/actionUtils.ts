// Common action state interface
export interface ActionState<T = unknown> {
  success: boolean;
  error?: string;
  result?: T;
}

// Validation result type
export interface ValidationResult {
  isValid: boolean;
  error?: string;
}

// API configuration
const API_BASE_URL = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "").replace(
  /\/$/,
  ""
);

/**
 * Creates an error state for actions
 */
export function createErrorState<T>(error: string): ActionState<T> {
  return {
    success: false,
    error,
  };
}

/**
 * Creates a success state for actions
 */
export function createSuccessState<T>(result: T): ActionState<T> {
  return {
    success: true,
    result,
  };
}

/**
 * Validates a required string field
 */
export function validateRequiredString(
  value: string | null | undefined,
  fieldName: string,
  minLength?: number,
  maxLength?: number
): ValidationResult {
  if (!value || value.trim().length === 0) {
    return {
      isValid: false,
      error: `${fieldName} is required.`,
    };
  }

  const trimmed = value.trim();

  if (minLength && trimmed.length < minLength) {
    return {
      isValid: false,
      error: `${fieldName} must be at least ${minLength} characters long.`,
    };
  }

  if (maxLength && trimmed.length > maxLength) {
    return {
      isValid: false,
      error: `${fieldName} must be no more than ${maxLength} characters long.`,
    };
  }

  return { isValid: true };
}

/**
 * Validates a coffee bar code (6 characters, uppercase)
 */
export function validateCoffeeBarCode(
  code: string | null | undefined
): ValidationResult {
  const normalizedCode = code?.trim().toUpperCase();

  if (!normalizedCode) {
    return {
      isValid: false,
      error: "Please enter a coffee bar code.",
    };
  }

  if (normalizedCode.length !== 6) {
    return {
      isValid: false,
      error: "Coffee bar code must be exactly 6 characters.",
    };
  }

  return { isValid: true };
}

/**
 * Validates a numeric input with optional min/max constraints
 */
export function validateNumber(
  value: string | null | undefined,
  fieldName: string,
  min?: number,
  max?: number
): ValidationResult & { value?: number } {
  if (!value || value.trim() === "") {
    return { isValid: true }; // Optional field
  }

  const numValue = Number(value);

  if (Number.isNaN(numValue)) {
    return {
      isValid: false,
      error: `Please enter a valid ${fieldName}.`,
    };
  }

  if (min !== undefined && numValue < min) {
    return {
      isValid: false,
      error: `${fieldName} must be at least ${min}.`,
    };
  }

  if (max !== undefined && numValue > max) {
    return {
      isValid: false,
      error: `${fieldName} must be no more than ${max}.`,
    };
  }

  return { isValid: true, value: numValue };
}

/**
 * Makes an API request and handles common error scenarios
 */
export async function makeApiRequest<T>(
  url: string,
  options: RequestInit,
  errorMessages: {
    notFound?: string;
    generic?: string;
  } = {}
): Promise<ActionState<T>> {
  try {
    const response = await fetch(`${API_BASE_URL}${url}`, {
      headers: { "Content-Type": "application/json" },
      ...options,
    });

    const payload = await response.json();

    if (response.status === 404 && errorMessages.notFound) {
      return createErrorState(errorMessages.notFound);
    }

    if (!response.ok) {
      const errorMessage =
        (payload && (payload.detail ?? payload.title)) ||
        errorMessages.generic ||
        "An error occurred.";
      return createErrorState(errorMessage);
    }

    return createSuccessState(payload as T);
  } catch (error) {
    console.error("API request failed:", error);
    return createErrorState(
      errorMessages.generic || "Something went wrong. Please try again."
    );
  }
}

/**
 * Safely extracts and validates form data
 */
export function getFormData(
  formData: FormData,
  fields: string[]
): Record<string, string> {
  const result: Record<string, string> = {};

  for (const field of fields) {
    result[field] = (formData.get(field) as string) || "";
  }

  return result;
}
