import { JoinCoffeeBarResponse } from "@/types";
import {
  ActionState,
  createErrorState,
  validateRequiredString,
  validateCoffeeBarCode,
  makeApiRequest,
  getFormData,
} from "./actionUtils";

export interface JoinCoffeeBarState
  extends ActionState<JoinCoffeeBarResponse> {}

export async function joinCoffeeBarAction(
  prevState: JoinCoffeeBarState,
  formData: FormData
): Promise<JoinCoffeeBarState> {
  // Extract form data
  const { code, username } = getFormData(formData, ["code", "username"]);

  // Validate coffee bar code
  const codeValidation = validateCoffeeBarCode(code);
  if (!codeValidation.isValid) {
    return createErrorState(codeValidation.error!);
  }

  // Validate username
  const usernameValidation = validateRequiredString(
    username,
    "Username",
    3,
    20
  );
  if (!usernameValidation.isValid) {
    return createErrorState(usernameValidation.error!);
  }

  const normalizedCode = code.trim().toUpperCase();

  // Make API request
  return makeApiRequest<JoinCoffeeBarResponse>(
    `/coffee-bars/${normalizedCode}/hipsters`,
    {
      method: "POST",
      body: JSON.stringify({ username: username.trim() }),
    },
    {
      notFound:
        "We couldn't find a coffee bar with that code. Double-check the six characters.",
      generic: "We couldn't join that coffee bar.",
    }
  );
}
