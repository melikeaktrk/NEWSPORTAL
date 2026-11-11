import type { Metadata } from "next";
import SiteHeader from "@/components/SiteHeader";
import HomeSlider from "@/components/HomeSlider";
import { getPublishedNews } from "@/lib/news";

export const dynamic = "force-dynamic";
export const metadata: Metadata = { title: "Son Haberler | NewsPortal", description: "Yayınlanan en güncel haberleri takip edin." };

const labels: Record<string, string> = { gundem: "Gündem", ekonomi: "Ekonomi", spor: "Spor", dunya: "Dünya" };

export default async function LatestNewsPage() {
  const { items, error } = await getPublishedNews();
  return <main className="siteFrame">
    <SiteHeader />
    <div className="homepageLayout categoryPageLayout">
      <aside className="homeAdRail" aria-hidden="true" />
      <div className="contentWidth homeContent">
        <div className="breadcrumb"><a href="/">Ana Sayfa</a><span>/</span><span>Son Haberler</span></div>
        <section className="sectionMasthead latestMasthead">
          <div><p className="eyebrow">GÜNCEL HABER AKIŞI</p><h1>Son Haberler</h1><p>Yayınlanan en güncel haberler, kaynaklarıyla birlikte.</p></div>
          <span className="sectionNumber">24/7</span>
        </section>
        {error ? <section className="statePanel stateError"><span className="stateIcon">!</span><div><h2>Haber servisine ulaşılamadı</h2><p>{error}</p><a href="/api/health" target="_blank" rel="noreferrer">API durumunu kontrol et</a></div></section> : items.length ? <>
          <HomeSlider items={items.slice(0, 5)} labels={labels} />
          <section className="categoryLatestSection">
            <div className="sectionBar"><div><p className="eyebrow">EDİTÖR ONAYLI</p><h2>Yayındaki haberler</h2></div><span>{items.length} haber</span></div>
            <section className="articleGrid" aria-label="Son haberler">
              {items.map((item, index) => <article className={`articleCard ${index === 0 ? "articleCardLead" : ""}`} key={item.id}>
                <div className="articleCardTop"><a className="articleCategory" href={`/kategori/${item.category}`}>{labels[item.category] ?? item.category}</a><span className="articleIndex">{String(index + 1).padStart(2, "0")}</span></div>
                <h2><a href={item.originalUrl} target="_blank" rel="noreferrer">{item.title}</a></h2>
                <p>{item.summary || "Kaynağın haber özeti bulunmuyor."}</p>
                <div className="articleCardFoot"><span>{item.sourceName}</span><time>{item.originalPublishedAt ? new Date(item.originalPublishedAt).toLocaleDateString("tr-TR") : "Yeni"}</time></div>
              </article>)}
            </section>
          </section>
        </> : <section className="statePanel"><span className="stateIcon">N</span><div><h2>Henüz yayınlanan haber yok</h2><p>RSS içerikleri editör onayından sonra burada görünür.</p><a href="/admin/rss">Yönetim ekranında haber kuyruğunu aç <span aria-hidden="true">↗</span></a></div></section>}
        <footer className="siteFooter"><a className="brand" href="/">NEWS<span>PORTAL</span></a><span>Kaynak bağlantıları orijinal yayıncıya yönlendirir.</span></footer>
      </div>
      <aside className="homeAdRail" aria-hidden="true" />
    </div>
  </main>;
}
