import SiteHeader from "@/components/SiteHeader";
import LiveWidgets from "@/components/LiveWidgets";
import HomeSlider from "@/components/HomeSlider";
import { getPublishedNews } from "@/lib/news";

const labels: Record<string, string> = { gundem: "Gündem", ekonomi: "Ekonomi", spor: "Spor", dunya: "Dünya" };

function AdRail({ side }: { side: "left" | "right" }) {
  return <aside className={`homeAdRail homeAdRail-${side}`} aria-hidden="true" />;
}

export default async function HomePage() {
  const { items, error } = await getPublishedNews();
  const [lead, ...rest] = items;
  return <main className="siteFrame">
    <SiteHeader />
    <div className="homepageLayout">
      <AdRail side="left" />
      <div className="contentWidth homeContent">
        <div className="topline"><span><i className="liveDot" /> GÜNCEL HABER AKIŞI</span><span>GÜVENİLİR KAYNAKLARDAN · EDİTÖR ONAYLI</span></div>
        <div className="breakingTicker"><strong>SON DAKİKA</strong><span>{lead?.title ?? "Türkiye’den ve dünyadan güncel haberler"}</span><a href="#son-haberler" aria-label="Son haberlere git">→</a></div>
        {error ? <div className="statePanel stateError"><span className="stateIcon">!</span><div><h2>Haber servisine ulaşılamadı</h2><p>{error}</p><a href="/api/health" target="_blank" rel="noreferrer">API durumunu kontrol et</a></div></div> : items.length ? <HomeSlider items={items.slice(0, 5)} labels={labels} /> : <div className="statePanel"><span className="stateIcon">N</span><div><h2>Yayın akışı hazır</h2><p>RSS’ten alınan haberler editör onayıyla yayınlanmaya başlayacak.</p><a href="/admin/rss">Haber kuyruğunu görüntüle <span>↗</span></a></div></div>}
        <section id="son-haberler" className="newsSection">
          <div className="sectionBar"><div><p className="eyebrow">GÜNCELDEN SEÇTİKLERİMİZ</p><h2>Son haberler</h2></div><span>{items.length ? `${items.length} yayın` : "Haber akışı"}</span></div>
          {items.length > 1 && <div className="homeNewsGrid">
            <div className="sideArticles">{rest.slice(0, 8).map((item, index) => <article className="sideArticle" key={item.id}><span className="sideArticleIndex">{String(index + 2).padStart(2, "0")}</span><div><a className="articleCategory" href={`/kategori/${item.category}`}>{labels[item.category] ?? item.category}</a><h3><a href={item.originalUrl} target="_blank" rel="noreferrer">{item.title}</a></h3><span className="sideSource">{item.sourceName}</span></div></article>)}</div>
            {lead && <aside className="homeBrief"><p className="eyebrow">GÜNDEMDE</p><h3>{lead.title}</h3><p>{lead.summary || "Günün öne çıkan gelişmelerini NewsPortal’da takip edin."}</p><a className="textLink" href={lead.originalUrl} target="_blank" rel="noreferrer">Kaynağında oku <span>↗</span></a></aside>}
          </div>}
        </section>
        <LiveWidgets />
        <footer className="siteFooter"><a className="brand" href="/">NEWS<span>PORTAL</span></a><span>Güncel · Güvenilir · Kaynağı belli</span><a href="/admin">Yönetim</a></footer>
      </div>
      <AdRail side="right" />
    </div>
  </main>;
}
