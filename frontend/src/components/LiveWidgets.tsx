"use client";

import { useEffect, useState } from "react";

const apiBase = "";
type Weather = { city: string; temperature: number; apparentTemperature: number | null; relativeHumidity: number | null; windSpeed: number | null; weatherCode: number; description: string; dailyMinTemperature: number | null; dailyMaxTemperature: number | null; fetchedAt: string; source: string };
type Rate = { baseCurrency: string; targetCurrency: string; rate: number; rateDate: string; fetchedAt: string; source: string };

export default function LiveWidgets() {
  const [weather, setWeather] = useState<Weather | null>(null);
  const [rates, setRates] = useState<Rate[]>([]);
  const [weatherError, setWeatherError] = useState("");
  const [ratesError, setRatesError] = useState("");
  const [city, setCity] = useState("Malatya");
  const [cities, setCities] = useState<string[]>([]);

  useEffect(() => {
    let active = true;
    fetch(`${apiBase}/api/public/weather/cities`).then(async response => {
      const body = await response.json();
      if (!response.ok || !body.success) throw new Error(body.message || "İl listesi alınamadı.");
      if (active) setCities(body.data);
    }).catch(() => { if (active) setWeatherError("İl listesi alınamadı. Backend bağlantısını kontrol edin."); });
    fetch(`${apiBase}/api/public/weather?city=${encodeURIComponent(city)}`).then(async response => {
      const body = await response.json();
      if (!response.ok || !body.success) throw new Error(body.message || "Hava durumu alınamadı.");
      if (active) { setWeather(body.data); setWeatherError(""); }
    }).catch(error => { if (active) setWeatherError(error instanceof Error ? error.message : "API bağlantısı kurulamadı."); });
    fetch(`${apiBase}/api/public/exchange-rates`).then(async response => {
      const body = await response.json();
      if (!response.ok || !body.success) throw new Error(body.message || "Döviz verisi alınamadı.");
      if (active) { setRates(body.data); setRatesError(""); }
    }).catch(error => { if (active) setRatesError(error instanceof Error ? error.message : "API bağlantısı kurulamadı."); });
    return () => { active = false; };
  }, [city]);

  return <section className="widgets" aria-label="Hava durumu ve döviz">
    <article className="widget" id="hava-durumu">
      <div className="widgetTop"><div><p className="eyebrow">ŞEHRİNDE BUGÜN</p><h2>Hava durumu</h2></div><select aria-label="İl seç" value={city} disabled={!cities.length} onChange={event => setCity(event.target.value)}>{cities.length ? cities.map(value => <option key={value}>{value}</option>) : <option>İller yükleniyor…</option>}</select></div>
      {weather ? <><div className="weatherReading"><strong>{Math.round(weather.temperature)}°</strong><span>{weather.description}<br/>Hissedilen {weather.apparentTemperature == null ? "—" : `${Math.round(weather.apparentTemperature)}°`}</span></div><p className="widgetMeta">En düşük {weather.dailyMinTemperature ?? "—"}° · En yüksek {weather.dailyMaxTemperature ?? "—"}° · Nem {weather.relativeHumidity ?? "—"}% · Rüzgâr {weather.windSpeed ?? "—"} km/sa</p><p className="sourceNote">{weather.source} · Güncelleme {new Date(weather.fetchedAt).toLocaleTimeString("tr-TR", { hour: "2-digit", minute: "2-digit" })}</p></> : <p className="widgetMeta">{weatherError || "Veri yükleniyor…"}</p>}
    </article>
    <article className="widget">
      <div className="widgetTop"><div><p className="eyebrow">PİYASALAR</p><h2>Döviz kurları</h2></div><span className="dailyLabel">Günlük referans</span></div>
      {rates.length ? <div className="ratesList">{rates.map(rate => <div className="rateRow" key={rate.baseCurrency}><span>{rate.baseCurrency}<small> / TRY</small></span><strong>{rate.rate.toLocaleString("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 4 })} ₺</strong></div>)}</div> : <p className="widgetMeta">{ratesError || "Veri yükleniyor…"}</p>}
      <p className="sourceNote">ECB ve diğer resmî kaynaklardan derlenen günlük referans kuru. Canlı banka alış/satış fiyatı değildir.</p>
    </article>
  </section>;
}
