import AdminLayout from "@/components/AdminLayout";

export const metadata = { title: "Yönetim | NewsPortal" };

export default function AdminPage() {
  return <AdminLayout title="Genel görünüm" subtitle="Haber akışını ve yayın kuyruğunu buradan yönet.">
    <div className="adminDashboardGrid">
      <a className="dashboardFeature" href="/admin/rss"><span className="featureIcon">◉</span><p className="eyebrow">İÇERİK AKIŞI</p><h2>RSS haber merkezi</h2><p>Kaynakları yönet, yeni haberleri tara ve yayın öncesi incele.</p><span className="featureAction">Haber merkezini aç <b>→</b></span></a>
      <section className="dashboardWelcome"><p className="eyebrow">BAŞLANGIÇ</p><h2>Yayın masasına<br/>hoş geldin.</h2><p>Yeni içerikler RSS üzerinden inceleme kuyruğuna eklenir. Editör onayı verilen haberler kendi kategorilerinde yayınlanır.</p><div className="dashboardSteps"><span><b>01</b> Kaynağı tara</span><span><b>02</b> Haberi gözden geçir</span><span><b>03</b> Yayınla</span></div></section>
    </div>
    <div className="adminNotice"><span>i</span><p>SuperAdmin hesabın doğrulandı. Tüm yönetim ve yayın işlemlerine erişimin var.</p></div>
  </AdminLayout>;
}
