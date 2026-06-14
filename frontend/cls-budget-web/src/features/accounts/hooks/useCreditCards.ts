"use client";

import { useMemo } from "react";
import { useAccounts } from "@/features/accounts/hooks/useAccounts";
import { isCreditCardAccount } from "@/features/accounts/utils/accountMapper";

export function useCreditCards() {
  const { accounts, loading, error, reload } = useAccounts();

  const creditCards = useMemo(
    () => accounts.filter(isCreditCardAccount),
    [accounts],
  );

  return { accounts: creditCards, loading, error, reload };
}
