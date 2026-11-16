"use client";

import { useState } from "react";

const symbols: Record<string, string> = { gundem: "G", ekonomi: "₺", spor: "S", dunya: "D" };

export default function NewsCover({ imageUrl, category, index }: { imageUrl: string | null; category: string; index: number }) {
  const [failed, setFailed] = useState(false);
  return <>
    {(!imageUrl || failed) && <span className="homeNewsCardSymbol" aria-hidden="true">{symbols[category] ?? "N"}</span>}
    {imageUrl && !failed && <img className="homeNewsCardImage" src={imageUrl} alt="" loading="lazy" decoding="async" onError={() => setFailed(true)} />}
    <span className="homeNewsCardVisualCaption" aria-hidden="true">NEWS PORTAL <b>{String(index + 1).padStart(2, "0")}</b></span>
  </>;
}
