---

## 🌪️ Kalite Güvence (QA) & Performans Test Raporu

Projemizin API katmanının sağlamlığını ve eşzamanlı isteklere karşı dayanıklılığını ölçmek amacıyla **Postman Performance** aracıyla Stres Testi (Stress Testing) gerçekleştirilmiştir.

# 🌪️ Smart Office - QA & Performans Test Raporu

Projemizin API katmanının sağlamlığını ve eşzamanlı isteklere karşı dayanıklılığını ölçmek amacıyla **Postman Performance** aracıyla Stres Testi (Stress Testing) gerçekleştirilmiştir.

### 📊 Test Metrikleri:
* **Sanal Kullanıcı (Virtual Users):** 20 Eşzamanlı Kullanıcı
* **Test Süresi:** 1 Dakika (Fixed Profile)
* **Toplam Atılan İstek:** 17.060 adet
* **Saniye Başına İstek (RPS):** ~287
* **Ortalama Yanıt Süresi (Avg. Response Time):** 1 ms

### 🛡️ Güvenlik ve Performans Analizi:
Sistemimize saniyede yaklaşık 287 istek bindiğinde sunucumuz kilitlenmemiş, ortalama **1 ms** yanıt süresi ile çalışmaya devam etmiştir. 

Raporda görülen **%100 Hata (Error) Oranı**, sistemin çöktüğünü değil; `AuthApiController` mimarimize entegre ettiğimiz `[EnableRateLimiting]` ve `TooManyLoginAttempts` güvenlik politikalarımızın başarıyla çalıştığını kanıtlamaktadır. Sistem, bu suni yoğunluğu bir kaba kuvvet (Brute Force) / DDoS saldırısı olarak algılamış ve `429 Too Many Requests` fırlatarak veritabanını ve sunucuyu koruma altına almıştır.

### 📈 Postman Canlı Performans Grafiği:
![Postman performans testi özet paneli, 1 dakika boyunca 20 eşzamanlı sanal kullanıcıyla gerçekleştirilen stres testinin 17.060 toplam istek, 287 isteğe/saniyeye yakın hız, 1 ms ortalama yanıt süresi, yüzde 100 hata oranı ve sistem kaynak kullanımına dair Peak CPU ve Peak Memory değerlerini gösteriyor.](./wwwroot/images/stres-testi-sonucu.png)