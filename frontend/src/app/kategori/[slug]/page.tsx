import type { Metadata } from "next";
import { notFound } from "next/navigation";
import SiteHeader from "@/components/SiteHeader";
import { getPublishedNews } from "@/lib/news";

const categories: Record<string, { title: string; description: string; marker: string }> = {
  gundem: { title: "Gündem", description: "Türkiye’nin gündemini belirleyen gelişmeler.", marker: "01" },
  ekonomi: { title: "Ekonomi", description: "Piyasalar, iş dünyası ve ekonomide öne çıkanlar.", marker: "02" },
  spor: { title: "Spor", description: "Sahadan, tribünden ve spor dünyasından haberler.", marker: "03" },
  dunya: { title: "Dünya", description: "Dünyada yaşanan gelişmeler ve uluslararası gündem.", marker: "04" },
};

export const dynamic = "force-dynamic";

export async function generateMetadata({ params }: { params: { slug: string } }): Promise<Metadata> {
  const category = categories[params.slug];
  return { title: category ? `${category.title} Haberleri | NewsPortal` : "Kategori Bulunamadı | NewsPortal", description: category?.description };
}

export default async function CategoryPage({ params }: { params: { slug: string } }) {
  const category = categories[params.slug];
  if (!category) notFound();
  const { items, error } = await getPublishedNews(params.slug);

  return <main className="siteFrame">
    <SiteHeader />
    <div className="contentWidth">
      <div className="breadcrumb"><a href="/">Ana Sayfa</a><span>/</span><span>{category.title}</span></div>
      <section className="sectionMasthead">
        <div><p className="eyebrow">KATEGORİ / {category.marker}</p><h1>{category.title}</h1><p>{category.description}</p></div>
        <span className="sectionNumber">{category.marker}</span>
      </section>
      {error ? <section className="statePanel stateError"><span className="stateIcon">!</span><div><h2>Haber servisine ulaşılamadı</h2><p>{error}</p><a href="/api/health" target="_blank" rel="noreferrer">API durumunu kontrol et</a></div></section> : items.length ? <>
        <div className="sectionBar"><h2>Yayındaki haberler</h2><span>{items.length} haber</span></div>
        <section className="articleGrid" aria-label={`${category.title} haberleri`}>
          {items.map((item, index) => <article className={`articleCard ${index === 0 ? "articleCardLead" : ""}`} key={item.id}>
            <div className="articleCardTop"><span className="articleCategory">{category.title}</span><span className="articleIndex">{String(index + 1).padStart(2, "0")}</span></div>
            <h2><a href={item.originalUrl} target="_blank" rel="noreferrer">{item.title}</a></h2>
            <p>{item.summary || "Kaynağın haber özeti bulunmuyor."}</p>
            <div className="articleCardFoot"><span>{item.sourceName}</span><time>{item.originalPublishedAt ? new Date(item.originalPublishedAt).toLocaleDateString("tr-TR") : "Yeni"}</time></div>
          </article>)}
        </section>
      </> : <section className="statePanel"><span className="stateIcon">N</span><div><h2>Bu kategoride henüz yayınlanan haber yok</h2><p>RSS içerikleri editör onayından sonra burada görünür.</p><a href="/admin/rss">Yönetim ekranında haber kuyruğunu aç <span aria-hidden="true">↗</span></a></div></section>}
      <footer className="siteFooter"><a className="brand" href="/">NEWS<span>PORTAL</span></a><span>Kaynak bağlantıları orijinal yayıncıya yönlendirir.</span></footer>
    </div>
  </main>;
}
