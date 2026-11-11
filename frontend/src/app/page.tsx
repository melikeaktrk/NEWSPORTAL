import SiteHeader from "@/components/SiteHeader";
import LiveWidgets from "@/components/LiveWidgets";
import { getPublishedNews } from "@/lib/news";

const labels: Record<string, string> = { gundem: "Gündem", ekonomi: "Ekonomi", spor: "Spor", dunya: "Dünya" };

export default async function HomePage() {
  const { items, error } = await getPublishedNews();
  const [lead, ...rest] = items;
  return <main className="siteFrame">
    <SiteHeader />
    <div className="contentWidth">
      <div className="topline"><span><i className="liveDot" /> GÜNCEL HABER AKIŞI</span><span>GÜVENİLİR KAYNAKLARDAN · EDİTÖR ONAYLI</span></div>
      <section className="homeHero">
        <div className="heroCopy"><p className="eyebrow">HABERİN MERKEZİ</p><h1>Gündemi<br/><em>yakından</em> takip et.</h1><p>Türkiye’den ve dünyadan seçilmiş haberler, kaynağıyla birlikte.</p><a className="heroLink" href="#son-haberler">Son haberleri keşfet <span>↓</span></a></div>
        <div className="heroAside"><span className="heroOrb heroOrbOne"/><span className="heroOrb heroOrbTwo"/><span className="heroMark">NP</span><p>Bilgiye<br/>doğrudan<br/>ulaşın.</p><span className="heroAsideFoot">NEWS / 01</span></div>
      </section>
      <section id="son-haberler" className="newsSection">
        <div className="sectionBar"><div><p className="eyebrow">GÜNCELDEN SEÇTİKLERİMİZ</p><h2>Son haberler</h2></div><span>{items.length ? `${items.length} yayın` : "Haber akışı"}</span></div>
        {error ? <div className="statePanel stateError"><span className="stateIcon">!</span><div><h2>Haber servisine ulaşılamadı</h2><p>{error}</p><a href="/api/health" target="_blank" rel="noreferrer">API durumunu kontrol et</a></div></div> : items.length ? <div className="homeNewsGrid">
          <article className="leadArticle"><div className="leadVisual"><span className="leadVisualLabel">ÖNE ÇIKAN</span><span className="leadVisualIndex">01</span><span className="leadVisualWord">HABER</span></div><div className="leadBody"><div className="articleCardTop"><a className="articleCategory" href={`/kategori/${lead.category}`}>{labels[lead.category] ?? lead.category}</a><span className="articleIndex">{lead.sourceName}</span></div><h2><a href={lead.originalUrl} target="_blank" rel="noreferrer">{lead.title}</a></h2><p>{lead.summary || "Kaynağın haber özeti bulunmuyor."}</p><a className="textLink" href={lead.originalUrl} target="_blank" rel="noreferrer">Haberi kaynağında oku <span>↗</span></a></div></article>
          <div className="sideArticles">{rest.slice(0, 4).map((item, index) => <article className="sideArticle" key={item.id}><span className="sideArticleIndex">{String(index + 2).padStart(2, "0")}</span><div><a className="articleCategory" href={`/kategori/${item.category}`}>{labels[item.category] ?? item.category}</a><h3><a href={item.originalUrl} target="_blank" rel="noreferrer">{item.title}</a></h3><span className="sideSource">{item.sourceName}</span></div></article>)}</div>
        </div> : <div className="statePanel"><span className="stateIcon">N</span><div><h2>Yayın akışı hazır</h2><p>RSS’ten alınan haberler editör onayıyla yayınlanmaya başlayacak.</p><a href="/admin/rss">Haber kuyruğunu görüntüle <span>↗</span></a></div></div>}
      </section>
      <LiveWidgets />
      <footer className="siteFooter"><a className="brand" href="/">NEWS<span>PORTAL</span></a><span>Güncel · Güvenilir · Kaynağı belli</span><a href="/admin">Yönetim</a></footer>
    </div>
  </main>;
}
