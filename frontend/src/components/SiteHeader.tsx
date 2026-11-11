"use client";

import { usePathname } from "next/navigation";

const categories = [
  ["Gündem", "gundem"], ["Ekonomi", "ekonomi"], ["Spor", "spor"], ["Dünya", "dunya"],
];

export default function SiteHeader() {
  const pathname = usePathname() ?? "/";
  return <>
    <div className="publicationBar"><div className="publicationInner"><span><i className="liveDot"/> Güvenilir kaynaklardan, editör onaylı</span><a href="/admin">İçerik yönetimi <span aria-hidden="true">↗</span></a></div></div>
    <header className="siteHeader">
      <div className="headerMain">
        <a className="brand" href="/" aria-label="NewsPortal ana sayfa"><span className="brandIcon">N</span><span className="brandWords">NEWS<strong>PORTAL</strong><small>HABERİN MERKEZİ</small></span></a>
        <div className="headerStatement">Gündemi takip et.<br/><span>Bilgiye doğrudan ulaş.</span></div>
        <a className="weatherQuickLink" href="/#hava-durumu"><span className="weatherSun">☀</span><span>Hava durumu<small>Şehrindeki son durum</small></span><b>↘</b></a>
      </div>
      <nav className="siteNav" aria-label="Ana menü"><a className={pathname === "/" ? "navActive" : ""} href="/" aria-current={pathname === "/" ? "page" : undefined}>Ana Sayfa</a>{categories.map(([name, slug]) => { const active = pathname === `/kategori/${slug}`; return <a className={active ? "navActive" : ""} aria-current={active ? "page" : undefined} key={slug} href={`/kategori/${slug}`}>{name}</a>; })}<a href="/#son-haberler">Son haberler</a></nav>
    </header>
  </>;
}
