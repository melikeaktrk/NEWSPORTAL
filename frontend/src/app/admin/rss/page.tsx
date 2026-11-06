import SiteHeader from "@/components/SiteHeader";
import RssReviewPanel from "@/components/RssReviewPanel";

export const metadata = { title: "RSS Kaynakları | NewsPortal" };

export default function AdminRssPage() {
  return <main className="page"><SiteHeader/><section className="categoryIntro adminIntro"><p className="eyebrow">YÖNETİM / RSS</p><h1>Haber akışı</h1><p>RSS içerikleri varsayılan olarak onay bekler; yalnızca yayınlanan haberler kategori sayfalarında görünür.</p></section><RssReviewPanel/><footer>Kaynak izinlerini ve yayın koşullarını kontrol edin. Haberler varsayılan olarak yalnızca başlık, kısa özet ve kaynak bağlantısıyla listelenir.</footer></main>;
}
