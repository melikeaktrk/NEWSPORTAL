import SiteHeader from "@/components/SiteHeader";

export const metadata = { title: "Yönetim | NewsPortal" };

export default function AdminPage() {
  return <main className="page"><SiteHeader/><section className="categoryIntro adminIntro"><p className="eyebrow">NEWS PORTAL / YÖNETİM</p><h1>İçerik merkezi</h1><p>Haber akışlarını gözden geçir ve yayın kuyruğunu yönet.</p></section><div className="adminTiles"><a className="adminTile" href="/admin/rss"><span>01 / RSS</span><strong>Kaynaklar ve haber onayı</strong><p>Kaynak ekle, RSS akışını tara ve haberleri incele.</p></a><a className="adminTile" href="/"><span>02 / SİTE</span><strong>Haber portalı</strong><p>Public sayfaların görünümünü aç.</p></a></div><p className="devNotice">RSS yönetim uçları şu anda yerel geliştirme ortamında açıktır. Yayına almadan önce kullanıcı girişi ve rol yetkilendirmesi eklenmelidir.</p><footer>NewsPortal Yönetim</footer></main>;
}
