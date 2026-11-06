const categories = [
  ["Gündem", "gundem"], ["Ekonomi", "ekonomi"], ["Spor", "spor"], ["Dünya", "dunya"],
];

export default function SiteHeader() {
  return <header className="header">
    <a className="brand" href="/">NEWS<span>PORTAL</span></a>
    <nav aria-label="Ana menü"><a href="/">Ana Sayfa</a>{categories.map(([name, slug]) => <a key={slug} href={`/kategori/${slug}`}>{name}</a>)}<a href="/#hava-durumu">Hava Durumu</a></nav>
    <a className="adminLink" href="/admin">Yönetim</a>
  </header>;
}
