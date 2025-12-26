import { createContext, useContext, type ReactNode } from "react"
import { AnonymousAuthenticationProvider } from "@microsoft/kiota-abstractions";
import { FetchRequestAdapter } from "@microsoft/kiota-http-fetchlibrary";
import { createHraBotApiClient, type HraBotApiClient } from "../hraBotApiClient/hraBotApiClient"

export const ApiClientContext = createContext<HraBotApiClient | undefined>(undefined);

export function ApiClientProvider({ children }: { children: ReactNode }) {
    const authProvider = new AnonymousAuthenticationProvider();
    const adapter = new FetchRequestAdapter(authProvider);
    adapter.baseUrl = import.meta.env.VITE_API_ENDPOINT as string;
    const apiClient = createHraBotApiClient(adapter);
    return (
        <ApiClientContext.Provider value={apiClient}>
            {children}
        </ApiClientContext.Provider>
    )
}

export function useApiClient() {
    const context = useContext(ApiClientContext)
    if (!context) {
        throw new Error("useApiClient must be used within an ApiClientProvider")
    }
    return context;
}