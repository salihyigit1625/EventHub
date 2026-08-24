# EventHub Web (Nuxt 4)

Modern etkinlik sitesi: yayınlanan etkinlikleri görüntüle, giriş yapıp bilet al, cüzdan/bekleme listesini yönet. Organizatör ve admin panelleri rol ile açılır.

## Çalıştırma

### Docker (önerilen)

Repoda:

```bash
docker compose up --build
```

- Web: http://localhost:3000  
- API: http://localhost:8080  
- Swagger: http://localhost:8080/swagger  

### Lokal

1. API ayakta olsun (`dotnet run --project src/EventHub.Api` → `http://localhost:5170`).
2. Frontend:

```bash
cd frontend
cp .env.example .env
npm install
npm run generate:api   # API açıksa Swagger'dan client üretir
npm run dev
```

http://localhost:3000

## Kullanım akışı

| Rol | Ne yapar |
|---|---|
| Misafir | Ana sayfada etkinlikleri görür; **Bilet al** → giriş |
| Katılımcı | Bilet alır / iptal eder, cüzdan yükler, bekleme listesine girer |
| Organizatör | Etkinlik + afiş + bilet tipi (VIP/Standart…), yayınlar |
| GateStaff | Check-in ekranından bilet kodunu doğrular |
| Admin | Organizatör onayı, istatistik, kapı görevlisi |

## Seed hesaplar

| Rol | E-posta | Şifre |
|---|---|---|
| Admin | admin@eventhub.local | Admin123! |
| Organizer | organizer@eventhub.local | Organizer123! |
| Attendee | attendee@eventhub.local | Attendee123! |
| GateStaff | gatestaff@eventhub.local | GateStaff123! |
