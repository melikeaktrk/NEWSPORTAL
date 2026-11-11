# NewsPortal

Türkçe haber portalı. Proje; `backend` altında ASP.NET Core .NET 8 katmanlı API çözümü ve `frontend` altında Next.js uygulaması olarak yapılandırılmıştır.

## Gereksinimler

- .NET 8 SDK
- Node.js 20+
- SQL Server (yerel geliştirmede varsayılan bağlantı `localhost` kullanır)

## Geliştirme

Backend: `dotnet run --project backend/NewsPortal.API`

Frontend: `cd frontend`, `npm install`, `npm run dev`

API sağlık adresleri: `/health` ve `/api/health`. Swagger geliştirme ortamında `/swagger` adresindedir.

## İlk çalışan özellikler

- Public kategoriler: `/kategori/gundem`, `/kategori/ekonomi`, `/kategori/spor`, `/kategori/dunya`
- Yönetim ekranı: `/admin`; RSS inceleme: `/admin/rss`
- Yönetici girişi: `/admin/login`
- Hava durumu API'si: `GET /api/public/weather?city=Malatya`
- Döviz API'si: `GET /api/public/exchange-rates`
- Onaylanmış RSS haberleri: `GET /api/public/news?category=gundem`
- Hava durumu il listesi: `GET /api/public/weather/cities` (Türkiye'nin 81 ili)
- RSS yönetimi Admin rolüyle korunur.

Hava durumu Open-Meteo, günlük referans döviz kurları Frankfurter üzerinden gelir; ikisi için de API anahtarı gerekmez. Geliştirme ortamında TRT Haber'in resmi RSS akışları otomatik taranır. Yeni kaynaklar `/admin/rss` ekranından eklenebilir. RSS ile gelen kayıtlar önce `PendingReview` olur; editör onaylamadan kategori sayfalarında görünmez. Kaynak bağlantısı korunur ve tam haber kopyalanmaz.

RSS kaynakları ve çekilen kayıtlar SQL Server'daki `RssFeedSources` ve `RssNewsItems` tablolarında kalıcı tutulur. Yönetici hesapları ASP.NET Identity ile `AspNetUsers` ve `AspNetRoles` tablolarında tutulur. İlk yönetici hesabını oluşturmak için API başlatılırken `AdminBootstrap__Email` ve `AdminBootstrap__Password` ortam değişkenleri verin; parola proje dosyalarına yazılmaz.

Migration'ları uygulamak için SQL Server'ı başlatıp `dotnet ef database update --project backend/NewsPortal.Infrastructure --startup-project backend/NewsPortal.API` komutunu çalıştırın. Admin tablolarını oluşturan migration `AddAdminIdentity` adını taşır.

Frontend tarayıcı API istekleri için `API_PROXY_TARGET`, Next.js sunucu bileşenlerinin backend isteği için `API_INTERNAL_URL` değerlerini kullanır. Yerel HTTP profili varsayılan olarak `http://localhost:5179` adresindedir.
