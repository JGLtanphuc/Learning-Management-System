import { useSelector } from "react-redux";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { Toaster } from "sonner";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { AuthProvider } from "./contexts/AuthContext";
import { RoleSwitchProvider } from "./contexts/RoleSwitchContext";
import { usePermissions } from "./hooks/usePermissions";
import { useActiveModuleGroups } from "./hooks/useSidebarMenus";
import { NotFoundRedirect } from "./pages/error/NotFoundRedirect";
import { routes } from "./router/component-registry";
import type { RootState } from "./store/store";
import { getHomePathByRole } from "./utils/rbac.utils";

function AppRoutes() {
    const { isAuthenticated } = useSelector((state: RootState) => state.auth);
    const { activeRole } = usePermissions();
    const { data: moduleGroups = [] } = useActiveModuleGroups(isAuthenticated);

    const homePath = isAuthenticated ? getHomePathByRole(activeRole) : "/login";

    // Create component lookup map from module-driven routes
    const componentRegistry = Object.fromEntries(
        routes.filter((r) => r.isModuleDriven).map((r) => [r.path, r.component]),
    );

    // Track which paths are registered dynamically via backend module groups
    const registeredPaths = new Set<string>();

    const dynamicRoutes = moduleGroups.flatMap((group) =>
        group.modules.map((m) => {
            if (!m.url) return null;
            const Component = componentRegistry[m.url];
            if (!Component) return null;
            registeredPaths.add(m.url);

            return (
                <Route
                    key={`mod-${m.id ?? m.url}`}
                    path={m.url}
                    element={
                        <ProtectedRoute requiredPermission={m.requiredPermission}>
                            <Component />
                        </ProtectedRoute>
                    }
                />
            );
        }),
    );

    // Register all remaining routes from routes (static, public, entity routes and action routes)
    const fallbackRoutes = routes.map((route, index) => {
        if (registeredPaths.has(route.path)) return null;
        const Component = route.component;

        if (route.isPublic) {
            return <Route key={route.path || index} path={route.path} element={<Component />} />;
        }

        return (
            <Route
                key={route.path || index}
                path={route.path}
                element={
                    <ProtectedRoute requiredPermission={route.requiredPermission}>
                        <Component />
                    </ProtectedRoute>
                }
            />
        );
    });

    return (
        <Routes>
            {/* Root redirect */}
            <Route path="/" element={<Navigate to={homePath} replace />} />

            {/* Dynamic routes from backend Module table */}
            {dynamicRoutes}

            {/* All registered routes from componentRegistry */}
            {fallbackRoutes}

            {/* Catch all */}
            <Route path="*" element={<NotFoundRedirect />} />
        </Routes>
    );
}

function App() {
    return (
        <BrowserRouter>
            <Toaster
                duration={1500}
                position="top-right"
                richColors
                toastOptions={{
                    className: "p-4",
                }}
            />
            <AuthProvider>
                <RoleSwitchProvider>
                    <AppRoutes />
                </RoleSwitchProvider>
            </AuthProvider>
        </BrowserRouter>
    );
}

export default App;
