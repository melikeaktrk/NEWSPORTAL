import SiteHeader from "@/components/SiteHeader";
import LiveWidgets from "@/components/LiveWidgets";
import HomeSlider from "@/components/HomeSlider";
import NewsCover from "@/components/NewsCover";
import { getPublishedNews, type PublishedNews } from "@/lib/news";

const labels: Record<string, string> = { gundem: "Gündem", ekonomi: "Ekonomi", spor: "Spor", dunya: "Dünya" };

function AdRail({ side }: { side: "left" | "right" }) {
  return <aside className={`homeAdRail homeAdRail-${side}`} aria-hidden="true" />;
}

function NewsCard({ item, index }: { item: PublishedNews; index: number }) {
  return <article className="homeNewsCard" key={item.id}>
    <a className={`homeNewsCardVisual newsVisual-${item.category}`} href={item.originalUrl} target="_blank" rel="noreferrer" aria-label={`${item.title} haberini kaynağında aç`}>
      <span className="homeNewsCardCategory">{labels[item.category] ?? item.category}</span>
      <NewsCover imageUrl={item.imageUrl} category={item.category} index={index} />
    </a>
    <div className="homeNewsCardBody">
      <div className="homeNewsCardMeta"><a href={`/kategori/${item.category}`}>{labels[item.category] ?? item.category}</a><span>{item.sourceName}</span></div>
      <h3><a href={item.originalUrl} target="_blank" rel="noreferrer">{item.title}</a></h3>
      <p>{item.summary || "Haberi kaynağından okuyun."}</p>
      <a className="homeNewsCardLink" href={item.originalUrl} target="_blank" rel="noreferrer">Haberi oku <span aria-hidden="true">↗</span></a>
    </div>
  </article>;
}

export default async function HomePage() {
  const { items, error } = await getPublishedNews();
  const lead = items[0];
  return <main className="siteFrame">
    <SiteHeader />
    <div className="homepageLayout">
      <AdRail side="left" />
      <div className="contentWidth homeContent">
        <div className="topline"><span><i className="liveDot" /> GÜNCEL HABER AKIŞI</span><span>GÜVENİLİR KAYNAKLARDAN · EDİTÖR ONAYLI</span></div>
        <div className="breakingTicker"><strong>SON DAKİKA</strong><span>{lead?.title ?? "Türkiye’den ve dünyadan güncel haberler"}</span><a href="#son-haberler" aria-label="Son haberlere git">→</a></div>
        {error ? <div className="statePanel stateError"><span className="stateIcon">!</span><div><h2>Haber servisine ulaşılamadı</h2><p>{error}</p><a href="/api/health" target="_blank" rel="noreferrer">API durumunu kontrol et</a></div></div> : items.length ? <HomeSlider items={items.slice(0, 5)} labels={labels} /> : <div className="statePanel"><span className="stateIcon">N</span><div><h2>Yayın akışı hazır</h2><p>RSS’ten alınan haberler editör onayından sonra burada görünür.</p></div></div>}
        <section id="son-haberler" className="newsSection">
          <div className="sectionBar"><div><p className="eyebrow">GÜNCELDEN SEÇTİKLERİMİZ</p><h2>Son haberler</h2></div><span>{items.length ? `${items.length} yayın` : "Haber akışı"}</span></div>
          {items.length > 0 && [...new Set([...Object.keys(labels), ...items.map(item => item.category)])].map(category => {
            const categoryItems = items.filter(item => item.category === category);
            if (!categoryItems.length) return null;
            return <section className="homeCategorySection" key={category} aria-labelledby={`home-category-${category}`}>
              <div className="homeCategoryHeader"><div><p className="eyebrow">KATEGORİ</p><h3 id={`home-category-${category}`}>{labels[category] ?? category}</h3></div><span>{categoryItems.length} haber</span><a href={`/kategori/${category}`}>Tümünü gör <span aria-hidden="true">→</span></a></div>
              <div className="homeCardGrid">{categoryItems.map((item, index) => <NewsCard item={item} index={index} key={item.id} />)}</div>
            </section>;
          })}
        </section>
        <LiveWidgets />
        <footer className="siteFooter">
          <div className="footerColumn footerBrandColumn"><a className="brand" href="/">NEWS<span>PORTAL</span></a><p>Güncel, güvenilir ve kaynağı belli haberler.</p></div>
          <nav className="footerColumn" aria-label="Kurumsal"><h2>Kurumsal</h2><a href="/">Ana Sayfa</a><a href="/kategori/gundem">Gündem</a><a href="/kategori/ekonomi">Ekonomi</a><a href="/kategori/spor">Spor</a><a href="/kategori/dunya">Dünya</a></nav>
          <div className="footerColumn footerSocial"><h2>Bizi takip edin</h2><div className="socialTiles" aria-label="Sosyal medya hesapları"><span aria-label="Facebook">f</span><span aria-label="Instagram">◎</span><span aria-label="YouTube">▶</span><span aria-label="X">X</span></div><p>Sosyal medya hesap bağlantıları yakında eklenecek.</p></div>
        </footer>
      </div>
      <AdRail side="right" />
    </div>
  </main>;
}
