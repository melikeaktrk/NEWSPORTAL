/** @type {import('next').NextConfig} */
const backend = (process.env.API_PROXY_TARGET || "http://localhost:5179").replace(/\/$/, "");
const nextConfig = {
  distDir: process.env.NEXT_BUILD_DIR || ".next",
  async rewrites() {
    return [{ source: "/api/:path*", destination: `${backend}/api/:path*` }];
  },
};

export default nextConfig;
