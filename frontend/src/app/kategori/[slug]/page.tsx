import type { Metadata } from "next";
import SiteHeader from "@/components/SiteHeader";

const categories: Record<string, string> = { gundem: "Gündem", ekonomi: "Ekonomi", spor: "Spor", dunya: "Dünya" };
const apiBase = "";
type News = { id: string; title: string; summary: string; originalUrl: string; sourceName: string; originalPublishedAt: string | null };

export const dynamic = "force-dynamic";
export async function generateMetadata({ params }: { params: { slug: string } }): Promise<Metadata> {
  const title = categories[params.slug];
  return { title: title ? `${title} Haberleri | NewsPortal` : "Kategori Bulunamadı | NewsPortal", description: title ? `${title} kategorisindeki güncel haberler.` : undefined };
}

export default async function CategoryPage({ params }: { params: { slug: string } }) {
  const title = categories[params.slug];
  if (!title) return <main className="page"><SiteHeader/><section className="simpleState"><p className="eyebrow">404</p><h1>Kategori bulunamadı</h1><a href="/">Ana sayfaya dön</a></section></main>;
  let news: News[] = [];
  let apiUnavailable = false;
  try {
    const response = await fetch(`${apiBase}/api/public/news?category=${params.slug}`, { cache: "no-store" });
    if (!response.ok) apiUnavailable = true;
    else { const result = await response.json(); news = result.data ?? []; }
  } catch { apiUnavailable = true; }

  return <main className="page"><SiteHeader/><section className="categoryIntro"><p className="eyebrow">NEWS / {title.toLocaleUpperCase("tr-TR")}</p><h1>{title}</h1><p>Onaylanmış haberler ve güncel gelişmeler.</p></section>
    {news.length ? <section className="newsList" aria-label={`${title} haberleri`}>{news.map(item => <article className="newsItem" key={item.id}><p className="eyebrow">{item.sourceName}</p><h2><a href={item.originalUrl} target="_blank" rel="noreferrer">{item.title}</a></h2><p>{item.summary || "Kaynağın haber özeti."}</p><small>{item.originalPublishedAt ? new Date(item.originalPublishedAt).toLocaleString("tr-TR") : ""} · Kaynağa git</small></article>)}</section> : <div className="emptyState"><h2>{apiUnavailable ? "Haber servisine bağlanılamadı" : "Bu kategoride henüz yayınlanmış haber yok"}</h2><p>{apiUnavailable ? "Backend’i başlatıp sayfayı yenileyin." : "RSS haberleri editör onayından sonra burada yayınlanır."}</p>{apiUnavailable && <a href="http://localhost:5179/swagger" target="_blank" rel="noreferrer">API durumunu kontrol et</a>}</div>}
    <footer>NewsPortal · Kaynak bağlantıları orijinal yayıncıya yönlendirir.</footer></main>;
}
