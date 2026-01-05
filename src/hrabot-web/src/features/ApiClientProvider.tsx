import { createContext, useContext, type ReactNode } from "react"
import { AnonymousAuthenticationProvider } from "@microsoft/kiota-abstractions";
import { FetchRequestAdapter } from "@microsoft/kiota-http-fetchlibrary";
import { createHraBotApiClient, type HraBotApiClient } from "../hraBotApiClient/hraBotApiClient"

export const ApiClientContext = createContext<HraBotApiClient | undefined>(undefined);

function getApiBaseUrl() {
    const apiBaseUrl = (import.meta.env.VITE_API_ENDPOINT as string | undefined)?.trim();
    if (!apiBaseUrl) {
        throw new Error("VITE_API_ENDPOINT is not configured");
    }
    return apiBaseUrl;
}

export function ApiClientProvider({ children }: { children: ReactNode }) {
    const authProvider = new AnonymousAuthenticationProvider();
    const adapter = new FetchRequestAdapter(authProvider);
    adapter.baseUrl = getApiBaseUrl();
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
