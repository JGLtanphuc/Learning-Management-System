/* eslint-disable react-refresh/only-export-components */
import React, { createContext, useContext, useEffect, type ReactNode } from "react";
import { useSelector, useDispatch } from "react-redux";
import { type RootState } from "../store/store";
import { setLogin, setLogout, finishInitializing } from "../store/slices/auth/authSlice";
import { type AuthContextType, type LoginRequest, type LoginResponse } from "../types/features/auth";
import { authApi } from "../api/features/auth.api";

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const dispatch = useDispatch();

    const {
        token,
        email,
        firstName,
        lastName,
        role,
        roles,
        permissions,
        rolePermissions,
        isAuthenticated,
        isInitializing,
    } = useSelector((state: RootState) => state.auth);

    // Khi ứng dụng khởi động (hoặc khi người dùng F5):
    // Tự động khôi phục AccessToken vào RAM qua HttpOnly Cookie của trình duyệt
    useEffect(() => {
        let isMounted = true;
        const hydrateAuth = async () => {
            try {
                const response = await authApi.refreshToken();
                if (isMounted) {
                    dispatch(setLogin(response));
                }
            } catch {
                if (isMounted) {
                    dispatch(finishInitializing());
                }
            }
        };

        hydrateAuth();
        return () => {
            isMounted = false;
        };
    }, [dispatch]);

    const user = isAuthenticated
        ? ({ email, firstName, lastName, role, roles, permissions, rolePermissions, token } as LoginResponse)
        : null;

    const login = async (credentials: LoginRequest): Promise<LoginResponse> => {
        const response = await authApi.login(credentials);
        dispatch(setLogin(response));
        return response;
    };

    const logout = async () => {
        try {
            await authApi.logout();
        } finally {
            dispatch(setLogout());
        }
    };

    const hasPermission = (p: string) => permissions.includes(p);
    const hasAnyPermission = (pms: string[]) => pms.some((p) => permissions.includes(p));
    const hasAllPermissions = (pms: string[]) => pms.every((p) => permissions.includes(p));
    const setGoogleUser = (userData: LoginResponse) => dispatch(setLogin(userData));

    const value: AuthContextType = {
        user,
        login,
        logout,
        hasPermission,
        hasAnyPermission,
        hasAllPermissions,
        isAuthenticated,
        setGoogleUser,
    };

    // Trong 0.1-0.2s khi đang khôi phục token ngầm, hiển thị spinner nhẹ tránh giật màn hình
    if (isInitializing) {
        return (
            <div className="flex h-screen w-screen items-center justify-center bg-background">
                <div className="flex flex-col items-center gap-3">
                    <div className="h-9 w-9 animate-spin rounded-full border-4 border-primary border-t-transparent" />
                    <p className="text-sm text-muted-foreground animate-pulse">Đang khôi phục phiên làm việc...</p>
                </div>
            </div>
        );
    }

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (context === undefined) {
        throw new Error("useAuth must be used within an AuthProvider");
    }
    return context;
};
