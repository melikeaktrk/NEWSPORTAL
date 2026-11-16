import { protectAdminRoute } from "../admin-panel/auth";

export const middleware = protectAdminRoute;

export const config = { matcher: ["/admin/:path*"] };
