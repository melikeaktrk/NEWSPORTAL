import type { ReactNode } from "react";
import AdminLogout from "@/components/AdminLogout";

export default function AdminLayout({ title, subtitle, children }: { title: string; subtitle: string; children: ReactNode }) {
  return <div className="adminLayout">
    <aside className="adminSidebar">
      <a className="adminBrand" href="/admin"><span className="adminBrandIcon">N</span><span>NEWS<strong>PORTAL</strong><small>İÇERİK MERKEZİ</small></span></a>
      <p className="sidebarLabel">ÇALIŞMA ALANI</p>
      <a className="sidebarLink" href="/admin"><span>▦</span> Genel görünüm</a>
      <a className="sidebarLink" href="/admin/rss"><span>◉</span> RSS haber akışı</a>
      <div className="sidebarDivider"/>
      <p className="sidebarLabel">PUBLIC SİTE</p>
      <a className="sidebarLink" href="/"><span>↗</span> Siteyi görüntüle</a>
      <div className="sidebarBottom"><span className="liveDot"/> Güvenli yönetici oturumu</div>
    </aside>
    <main className="adminMain">
      <header className="adminTopbar"><div><span>YÖNETİM</span><b>/</b><span>{title}</span></div><div className="adminTopActions"><a href="/">Siteyi aç ↗</a><AdminLogout /></div></header>
      <div className="adminPageContent"><section className="adminPageHeading"><div><p className="eyebrow">NEWS PORTAL / {title.toLocaleUpperCase("tr-TR")}</p><h1>{title}</h1><p>{subtitle}</p></div><span className="adminHeadingIcon">NP</span></section>{children}</div>
    </main>
  </div>;
}
