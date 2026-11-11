import AdminLayout from "@/components/AdminLayout";
import RssReviewPanel from "@/components/RssReviewPanel";

export const metadata = { title: "RSS Haber Akışı | NewsPortal" };

export default function AdminRssPage() {
  return <AdminLayout title="RSS haber akışı" subtitle="Haberleri kaynaklarıyla incele, yayın kuyruğunu düzenle.">
    <div className="workflowBanner"><span className="workflowIcon">✓</span><div><strong>Önce incele, sonra yayınla</strong><p>RSS kayıtları onay bekler. Yayına alınan haberler ilgili kategori sayfasında görünür.</p></div><a href="/kategori/gundem">Kategorileri gör ↗</a></div>
    <RssReviewPanel />
  </AdminLayout>;
}
