export type PublishedNews = {
  id: string;
  title: string;
  summary: string;
  originalUrl: string;
  sourceName: string;
  category: string;
  originalPublishedAt: string | null;
  importedAt: string;
};

export type PublishedNewsResult = { items: PublishedNews[]; error: string | null };

export async function getPublishedNews(category?: string): Promise<PublishedNewsResult> {
  // Server Components cannot use the browser's relative /api rewrite URL.
  const apiBase = (process.env.API_INTERNAL_URL || process.env.API_PROXY_TARGET || "http://localhost:5179").replace(/\/$/, "");
  const query = category ? `?category=${encodeURIComponent(category)}` : "";
  try {
    const response = await fetch(`${apiBase}/api/public/news${query}`, { cache: "no-store" });
    if (!response.ok) return { items: [], error: `Haber servisi HTTP ${response.status} yanıtı verdi.` };
    const body = await response.json();
    if (!body.success || !Array.isArray(body.data)) return { items: [], error: "Haber servisi beklenmeyen bir yanıt verdi." };
    return { items: body.data as PublishedNews[], error: null };
  } catch {
    return { items: [], error: "Backend’e ulaşılamadı. API’nin çalıştığını ve API_INTERNAL_URL ayarını kontrol edin." };
  }
}
