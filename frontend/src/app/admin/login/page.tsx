import type { Metadata } from "next";
import AdminLoginForm from "@/components/AdminLoginForm";

export const metadata: Metadata = { title: "Yönetici Girişi | NewsPortal", robots: { index: false, follow: false } };

export default function AdminLoginPage() {
  return <main className="loginPage">
    <section className="loginBrandPanel"><a className="adminBrand loginBrand" href="/"><span className="adminBrandIcon">N</span><span>NEWS<strong>PORTAL</strong><small>HABERİN MERKEZİ</small></span></a><div className="loginBrandCopy"><p className="eyebrow">NEWSROOM / 01</p><h1>Yayın masasına<br/>hoş geldin.</h1><p>İçeriği güvenle yönet. Her haberin kaynağı belli olsun.</p></div><span className="loginBrandFoot">GÜNCEL · GÜVENİLİR · KAYNAĞI BELLİ</span></section>
    <section className="loginPanel"><a className="loginBack" href="/">← Siteye dön</a><div className="loginCard"><p className="eyebrow">YÖNETİCİ ERİŞİMİ</p><h2>Giriş yap</h2><p className="loginIntro">NewsPortal yönetim paneline devam etmek için hesabını kullan.</p><AdminLoginForm/><p className="loginSecurity">Güvenli oturum · Parolalar veritabanında hash’lenerek saklanır.</p></div></section>
  </main>;
}
