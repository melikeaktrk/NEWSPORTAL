"use client";

import { useState } from "react";
import type { PublishedNews } from "@/lib/news";

export default function HomeSlider({ items, labels }: { items: PublishedNews[]; labels: Record<string, string> }) {
  const [active, setActive] = useState(0);
  const item = items[active];
  if (!item) return null;
  const move = (step: number) => setActive(index => (index + step + items.length) % items.length);
  return <section className="homeSlider" aria-label="Öne çıkan haberler" aria-roledescription="carousel">
    <div className="sliderCover" aria-hidden="true">
      <div className="sliderCoverLines" />
      <span className="sliderCoverKicker">NEWS PORTAL · {labels[item.category] ?? item.category}</span>
      <span className="sliderCoverWord">{String(active + 1).padStart(2, "0")}</span>
      <span className="sliderCoverCaption">GÜNDEMİN NABZI</span>
    </div>
    <div className="sliderStory" aria-live="polite" aria-atomic="true">
      <div className="sliderMeta"><span><i className="liveDot" /> ÖNE ÇIKAN HABER</span><span>{item.sourceName}</span></div>
      <a className="articleCategory" href={`/kategori/${item.category}`}>{labels[item.category] ?? item.category}</a>
      <h1><a href={item.originalUrl} target="_blank" rel="noreferrer">{item.title}</a></h1>
      <p>{item.summary || "Günün öne çıkan gelişmesi. Haberin ayrıntılarını kaynağında okuyun."}</p>
      <div className="sliderControls">
        <div className="sliderDots" aria-label="Haber seçimi">{items.map((news, index) => <button key={news.id} type="button" onClick={() => setActive(index)} className={index === active ? "isActive" : ""} aria-label={`${index + 1}. haber`} aria-current={index === active ? "true" : undefined} />)}</div>
        <span className="sliderCount">{String(active + 1).padStart(2, "0")} <i>/</i> {String(items.length).padStart(2, "0")}</span>
        <button className="sliderArrow" type="button" onClick={() => move(-1)} aria-label="Önceki haber">←</button>
        <button className="sliderArrow sliderArrowNext" type="button" onClick={() => move(1)} aria-label="Sonraki haber">→</button>
      </div>
    </div>
  </section>;
}
