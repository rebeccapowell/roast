import { SubmissionPolicy } from "@/types";
import {
  ActionState,
  createErrorState,
  validateRequiredString,
  validateNumber,
  makeApiRequest,
  getFormData,
} from "./actionUtils";

export type CreateCoffeeBarState = ActionState<{
  id: string;
  code: string;
  theme: string;
  defaultMaxIngredientsPerHipster: number;
  submissionPolicy: SubmissionPolicy;
}>;

export async function createCoffeeBarAction(
  prevState: CreateCoffeeBarState,
  formData: FormData
): Promise<CreateCoffeeBarState> {
  // Extract form data
  const { theme, maxPerHipster, submissionPolicy } = getFormData(formData, [
    "theme",
    "maxPerHipster",
    "submissionPolicy",
  ]);

  // Validate theme
  const themeValidation = validateRequiredString(theme, "Theme");
  if (!themeValidation.isValid) {
    return createErrorState(themeValidation.error!);
  }

  // Validate optional max per hipster
  const quotaValidation = validateNumber(
    maxPerHipster,
    "max ingredients value",
    1
  );
  if (!quotaValidation.isValid) {
    return createErrorState(quotaValidation.error!);
  }

  // Make API request
  return makeApiRequest<NonNullable<CreateCoffeeBarState["result"]>>(
    "/coffee-bars",
    {
      method: "POST",
      body: JSON.stringify({
        theme: theme.trim(),
        defaultMaxIngredientsPerHipster: quotaValidation.value,
        submissionPolicy: submissionPolicy as SubmissionPolicy,
      }),
    },
    {
      generic: "We couldn't create the coffee bar.",
    }
  );
}
