"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";

const apiBase = "";
type Source = { id: string; name: string; feedUrl: string; category: string; isActive: boolean; lastFetchedAt: string | null; lastError: string | null };
type Item = { id: string; sourceName: string; title: string; summary: string; originalUrl: string; category: string; originalPublishedAt: string | null };

export default function RssReviewPanel() {
  const [sources, setSources] = useState<Source[]>([]);
  const [items, setItems] = useState<Item[]>([]);
  const [message, setMessage] = useState("");
  const [publishedCategory, setPublishedCategory] = useState("");
  const [busy, setBusy] = useState(false);
  const [status, setStatusFilter] = useState<"PendingReview" | "Published" | "Rejected">("PendingReview");

  const load = useCallback(async () => {
    const [sourceResponse, itemResponse] = await Promise.all([fetch(`${apiBase}/api/admin/rss/sources`), fetch(`${apiBase}/api/admin/rss/items?status=${status}`)]);
    const [sourceBody, itemBody] = await Promise.all([sourceResponse.json(), itemResponse.json()]);
    if (!sourceResponse.ok || !itemResponse.ok) throw new Error("RSS yönetimi yalnızca yerel geliştirme modunda kullanılabilir. Backend’i Development profiliyle başlatın.");
    setSources(sourceBody.data); setItems(itemBody.data);
  }, [status]);

  useEffect(() => { load().catch(error => setMessage(error.message)); }, [load]);

  async function refresh() {
    setBusy(true); setMessage("RSS kaynakları kontrol ediliyor…");
    try { const response = await fetch(`${apiBase}/api/admin/rss/refresh`, { method: "POST" }); const body = await response.json(); if (!response.ok) throw new Error(body.message); setMessage(body.message); await load(); }
    catch (error) { setMessage(error instanceof Error ? error.message : "RSS kaynakları alınamadı."); }
    finally { setBusy(false); }
  }

  async function addSource(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true);
    const form = event.currentTarget;
    const data = new FormData(form);
    try {
      const response = await fetch(`${apiBase}/api/admin/rss/sources`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name: data.get("name"), feedUrl: data.get("feedUrl"), category: data.get("category") }) });
      const body = await response.json(); if (!response.ok) throw new Error(body.message);
      form.reset(); setMessage("RSS kaynağı eklendi."); await load();
    } catch (error) { setMessage(error instanceof Error ? error.message : "Kaynak eklenemedi."); }
    finally { setBusy(false); }
  }

  async function setStatus(id: string, status: "Published" | "Rejected" | "PendingReview") {
    setBusy(true);
    const item = items.find(candidate => candidate.id === id);
    try {
      const response = await fetch(`${apiBase}/api/admin/rss/items/${id}/status`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ status }) });
      const body = await response.json(); if (!response.ok) throw new Error(body.message || "İçerik durumu güncellenemedi.");
      if (status === "Published" && item) {
        setPublishedCategory(item.category);
        setMessage("Haber yayınlandı. Onu kategori sayfasında veya ‘Yayındakiler’ filtresinde görebilirsin.");
      } else { setPublishedCategory(""); setMessage(body.message || "Durum güncellendi."); }
      await load();
    } catch (error) { setMessage(error instanceof Error ? error.message : "İçerik durumu güncellenemedi."); }
    finally { setBusy(false); }
  }

  async function toggleSource(source: Source) {
    setBusy(true);
    try {
      const response = await fetch(`${apiBase}/api/admin/rss/sources/${source.id}/active`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ isActive: !source.isActive }) });
      const body = await response.json(); if (!response.ok) throw new Error(body.message || "Kaynak durumu güncellenemedi.");
      setMessage(body.message); await load();
    } catch (error) { setMessage(error instanceof Error ? error.message : "Kaynak durumu güncellenemedi."); }
    finally { setBusy(false); }
  }

  return <div className="adminContent">
    <div className="adminSectionHead"><div><p className="eyebrow">İÇERİK AKIŞI</p><h2>RSS kaynakları</h2></div><button className="primaryButton" disabled={busy} onClick={refresh}>{busy ? "Kontrol ediliyor…" : "Kaynakları şimdi tara"}</button></div>
    {message && <p className="inlineMessage" role="status">{message}{publishedCategory && <> <a className="messageLink" href={`/kategori/${publishedCategory}`}>Kategoriye git ↗</a></>}</p>}
    <div className="adminGrid"><section className="adminCard"><h3>Kaynak ekle</h3><form className="rssForm" onSubmit={addSource}><label>Kaynak adı<input name="name" required maxLength={100} placeholder="Haber kaynağı" /></label><label>RSS adresi<input name="feedUrl" required type="url" placeholder="https://ornek.com/rss.xml" /></label><label>Kategori<select name="category"><option value="gundem">Gündem</option><option value="ekonomi">Ekonomi</option><option value="spor">Spor</option><option value="dunya">Dünya</option></select></label><button disabled={busy}>Kaynağı ekle</button></form></section>
      <section className="adminCard"><h3>Haber kaynakları ({sources.length})</h3><div className="sourceList">{sources.map(source => <div className="sourceRow" key={source.id}><strong>{source.name}</strong><span>{source.category} · {source.isActive ? "Etkin" : "Duraklatıldı"} · {source.lastFetchedAt ? `Son tarama: ${new Date(source.lastFetchedAt).toLocaleTimeString("tr-TR")}` : "İlk tarama bekleniyor"}</span>{source.lastError && <small className="errorText">{source.lastError}</small>}<button className="secondaryButton sourceToggle" disabled={busy} onClick={() => toggleSource(source)}>{source.isActive ? "Kaynağı duraklat" : "Kaynağı etkinleştir"}</button></div>)}</div></section></div>
    <section className="adminCard reviewCard"><div className="adminSectionHead"><div><h3>Haber yönetimi</h3><p>{items.length} kayıt</p></div><select aria-label="İçerik durumuna göre filtrele" value={status} onChange={event => setStatusFilter(event.target.value as typeof status)}><option value="PendingReview">Onay bekleyenler</option><option value="Published">Yayındakiler</option><option value="Rejected">Reddedilenler</option></select></div>{items.length ? <div className="reviewList">{items.map(item => <article className="reviewItem" key={item.id}><div className="reviewMeta">{item.sourceName} · {item.category}</div><h4>{item.title}</h4><p>{item.summary || "Kaynakta açıklama yok."}</p><a href={item.originalUrl} target="_blank" rel="noreferrer">Orijinal haberi aç ↗</a>{status === "PendingReview" && <div className="reviewActions"><button disabled={busy} onClick={() => setStatus(item.id, "Published")}>Onayla ve yayınla</button><button disabled={busy} className="secondaryButton" onClick={() => setStatus(item.id, "Rejected")}>Reddet</button></div>}{status === "Published" && <div className="reviewActions"><button disabled={busy} className="secondaryButton" onClick={() => setStatus(item.id, "Rejected")}>Yayından kaldır</button></div>}{status === "Rejected" && <div className="reviewActions"><button disabled={busy} onClick={() => setStatus(item.id, "Published")}>Yayınla</button><button disabled={busy} className="secondaryButton" onClick={() => setStatus(item.id, "PendingReview")}>İncelemeye al</button></div>}</article>)}</div> : <p className="emptyAdmin">Bu durumda gösterilecek haber yok.</p>}</section>
  </div>;
}
