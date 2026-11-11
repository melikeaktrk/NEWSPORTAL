"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";

export default function AdminLoginForm() {
  const router = useRouter();
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setError(""); setBusy(true);
    const data = new FormData(event.currentTarget);
    try {
      const response = await fetch("/api/auth/login", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email: data.get("email"), password: data.get("password"), rememberMe: data.get("rememberMe") === "on" }) });
      const body = await response.json();
      if (!response.ok) throw new Error(body.message || "Giriş yapılamadı.");
      const returnUrl = new URLSearchParams(window.location.search).get("returnUrl");
      router.replace(returnUrl?.startsWith("/admin") && returnUrl !== "/admin/login" ? returnUrl : "/admin");
      router.refresh();
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Sunucuya bağlanılamadı."); }
    finally { setBusy(false); }
  }

  return <form className="loginForm" onSubmit={submit}>
    <label>E-posta<input type="email" name="email" autoComplete="username" placeholder="admin@newsportal.local" required autoFocus /></label>
    <label>Parola<input type="password" name="password" autoComplete="current-password" required /></label>
    <label className="rememberLabel"><input type="checkbox" name="rememberMe"/> Bu cihazda oturumumu açık tut</label>
    {error && <p className="loginError" role="alert">{error}</p>}
    <button type="submit" disabled={busy}>{busy ? "Giriş yapılıyor…" : "Yönetim paneline gir"}</button>
  </form>;
}
