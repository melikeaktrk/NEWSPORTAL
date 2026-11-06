import SiteHeader from "@/components/SiteHeader";
import LiveWidgets from "@/components/LiveWidgets";

export default function HomePage() {
  return (
    <main className="page">
      <SiteHeader />
      <section className="intro">
        <p className="eyebrow">GÜNDEMİN NABZI</p>
        <h1>Haberin doğru<br />adresi.</h1>
        <p className="lead">Türkiye’den ve dünyadan gelişmeleri takip edin.</p>
        <p className="note">Haberler kaynak denetimi ve editör onayından sonra yayınlanır.</p>
      </section>
      <LiveWidgets />
      <footer>NewsPortal · Güncel, güvenilir, tarafsız.</footer>
    </main>
  );
}
