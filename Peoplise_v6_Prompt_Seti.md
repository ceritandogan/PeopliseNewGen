# Peoplise v6 — Yeniden Yazım Prompt Seti

> **Amaç:** Mevcut Peoplise (v4/v5) platformunu, kesinleşen mimari kararlar doğrultusunda .NET 9 üzerinde sıfırdan inşa etmek.
> **Strateji:** Promptlar 5 aşamaya ayrılmıştır. Her aşama bir öncekinin çıktısına bağımlıdır.  
> Aşamaları sırasıyla çalıştırın; aradaki çıktıyı bir sonraki promptun bağlamına (context) verin.

---

## Kesinleşen Mimari Kararlar Özeti

| Karar Alanı | Tercih |
|---|---|
| **.NET Sürümü** | .NET 9 |
| **Veritabanı** | DB-agnostic (PostgreSQL + MSSQL desteği, EF Core) |
| **Multi-Tenancy** | Paylaşımlı DB, TenantId Global Query Filter |
| **Mesaj Kuyruğu** | RabbitMQ (MassTransit üzerinden) |
| **Dosya Depolama** | Soyut katman (Azure Blob, S3, MinIO desteği) |
| **AI / LLM** | Strategy Pattern (Azure OpenAI, OpenAI, Claude) |
| **Kimlik Doğrulama** | Custom JWT Token altyapısı |
| **Frontend** | React + TypeScript |
| **Lokalizasyon** | Türkçe + İngilizce (i18n) |
| **Ekip Büyüklüğü** | 1–3 geliştirici (küçük ekip) |
| **MVP Kapsamı** | ATS + İK Botu + Video Mülakat (3 çekirdek modül) |
| **Zorunlu Entegrasyonlar** | Logo JHR, SAP SuccessFactors, MS Teams/Graph, Facebook Messenger |
| **Dağıtım** | Henüz kesinleşmedi (konteyner-ready hazırlanacak) |
| **Veri Göçü** | Mevcut v4/v5 veritabanından migration gerekecek |
| **Ölçek** | 50–200 eş zamanlı kullanıcı |

---

## AŞAMA 1 — Temel Mimari İskelet ve Shared Kernel

```
Sen yüksek ölçeklenebilir, dağıtık ve kurumsal SaaS sistemleri tasarlama konusunda
uzman, Kıdemli Bir .NET Yazılım Mimarı ve Baş Mühendissin.

"Peoplise v6" adlı kurumsal dijital işe alım ve aday değerlendirme platformunu
.NET 9 üzerinde sıfırdan inşa edeceğiz.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
KESİNLEŞEN MİMARİ KARARLAR
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• Mimari Desen: Clean Architecture + DDD prensiplerine dayalı Modular Monolith
  (İleride mikroservise bölünebilecek gevşek bağlı modüller).
• CQRS: MediatR ile Command/Query ayrımı.
• Event-Driven: MassTransit + RabbitMQ, modüller arası Outbox Pattern.
• Veritabanı: EF Core 9 — DB-agnostic (hem PostgreSQL hem MSSQL desteği).
  Multi-tenancy: Paylaşımlı DB, TenantId ile Global Query Filters.
  Full Audit Trail: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted (Soft Delete).
• Kimlik Doğrulama: Custom JWT token tabanlı auth sistemi (Access + Refresh Token).
• Dosya Depolama: Soyut IFileStorageService (Azure Blob, S3, MinIO uyumlu).
• AI/LLM Soyutlaması: Strategy Pattern ile IAIProvider (Azure OpenAI, OpenAI, Claude).
• Dayanıklılık: Polly ile Circuit Breaker, Retry, Timeout politikaları.
• Önbellekleme: IDistributedCache (Redis veya InMemory).
• Gözlemlenebilirlik: Serilog (Structured Logging), OpenTelemetry, Health Checks.
• Test Altyapısı: xUnit + FluentAssertions + NSubstitute. Hedef: %85+ kod kapsamı.
• Lokalizasyon: Türkçe + İngilizce (i18n).
• Ekip: 1-3 kişi (küçük ekip için sürdürülebilir karmaşıklık seviyesi).
• MVP Kapsamı: ATS Workflow + İK Botu + Video Mülakat.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
SENDEN İSTEDİĞİM (AŞAMA 1)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Projenin temel iskeletini ve Shared Kernel katmanını eksiksiz oluştur:

1. SOLUTION YAPISI
   Tam klasör hiyerarşisini (.sln, src/, tests/, docs/) göster.
   Modüller: Peoplise.Modules.ATS, Peoplise.Modules.HrBot, Peoplise.Modules.VideoInterview
   Ortak: Peoplise.SharedKernel, Peoplise.Infrastructure, Peoplise.Api (Gateway)

2. SHARED KERNEL (Peoplise.SharedKernel)
   - BaseEntity<TId>, AggregateRoot<TId>, ValueObject abstract sınıfları.
   - IDomainEvent arayüzü ve DomainEventDispatcher.
   - IRepository<T> ve IUnitOfWork soyutlamaları.
   - Result<T> / Error modeli (Railway-Oriented Programming).
   - ITenantContext, TenantId ValueObject ve TenantInterceptor (EF Core SaveChanges).
   - IAuditableEntity arayüzü ve AuditInterceptor.
   - Sayfalama: PagedRequest<T> ve PagedResult<T>.

3. INFRASTRUCTURE KATMANI (Peoplise.Infrastructure)
   - AppDbContext: Multi-tenant Global Query Filter + Audit interceptor entegrasyonu.
   - GenericRepository<T> implementasyonu.
   - Custom JWT token üretim ve doğrulama servisleri (JwtTokenService, RefreshTokenService).
   - Outbox Pattern: OutboxMessage entity, OutboxPublisher (MassTransit).
   - IFileStorageService soyutlama + Azure Blob implementasyonu.

4. API GATEWAY (Peoplise.Api)
   - Program.cs ve Minimal API / Controller tabanlı yapı.
   - Global Exception Handling Middleware.
   - Serilog + OpenTelemetry konfigürasyonu.
   - Health Check endpoint'leri.
   - Swagger/OpenAPI konfigürasyonu.
   - CORS, Rate Limiting, Authentication/Authorization middleware.

5. UNIT TESTLER
   - Result<T> ve ValueObject eşitlik testleri.
   - TenantInterceptor'ın TenantId'yi doğru set ettiğini doğrulayan test.
   - AuditInterceptor'ın CreatedAt/UpdatedAt alanlarını doldurduğunu doğrulayan test.
   - JwtTokenService'in token üretim ve validasyon testleri.
   - Outbox mesajının doğru serialize edildiğini doğrulayan test.

KURALLAR:
• Kod üretim kalitesinde (production-ready) olacak; TODO, placeholder veya stub bırakma.
• Her sınıfın XML doc comment'i olacak.
• Küçük ekip (1-3 kişi) için sürdürülebilir karmaşıklıkta tut; gereksiz soyutlama katma.
• Tüm NuGet paket referanslarını .csproj dosyalarında göster.
```

---

## AŞAMA 2 — Modül 1: ATS ve İş Akışı Motoru (MVP Çekirdeği)

```
Sen Kıdemli bir .NET Backend Geliştiricisisin.
Aşama 1'de kurduğumuz Peoplise v6 Clean Architecture iskeleti üzerinde,
platformun en kritik modülü olan "Aday Takip ve İşe Alım Süreç Yönetimi
(ATS & Workflow Engine)" modülünü eksiksiz inşa edeceksin.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
İŞ MANTIĞI KAPSAMI
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

A. Pozisyon ve Proje Yönetimi
• Pozisyon (Position) oluşturma: Başlık, departman, lokasyon (şehir/ülke),
  çalışma modeli (Ofis/Uzaktan/Hibrit), kıdem seviyesi, pozisyon tipi (Tam/Yarı Zamanlı).
• Pozisyona özel dinamik değişkenler (Custom Variables): Anahtar-Değer çiftleri.
• Yasal metinler (KVKK/Aydınlatma): Pozisyon bazında özelleştirilebilir.
• Pozisyon Ekibi ve Yetki Matrisi: Rol bazlı erişim (Yönetici, Değerlendirici, Gözlemci).
• Şirket bilgileri, logo ve kariyer sayfası bağlantıları.

B. Süreç Akış Motoru (Workflow / Task Flow Engine)
• Her pozisyona N adet ardışık veya paralel "Aşama (Stage)" tanımlanabilir.
• Aşama Tipleri: Bilgi Formu, Ön Eleme Testi, Video Mülakat, Evrak Toplama,
  Canlı Mülakat, Değerlendirici Onayı, Teklif Aşaması.
• Tetikleyici Kuralları (Triggers):
  - "Puan ≥ X ise sonraki aşamaya otomatik taşı"
  - "Puan < Y ise adayı otomatik ele"
  - "Önceki aşama tamamlandıktan Z gün sonra sonraki aşamayı aktif et"
• Paralel görev desteği (Evrak yükleme + Anket doldurma eş zamanlı ilerleyebilir).
• Görev Kütüphanesi: Sık kullanılan aşama şablonları kaydedilip yeniden kullanılabilir.

C. Aday Yaşam Döngüsü ve Havuz Yönetimi
• Aday pipeline: Yeni Başvuru → İncelemede → Test → Mülakat → Teklif → Kabul/Red.
• Aday Profili: İletişim, özgeçmiş, eğitim, deneyim, sertifika, dil bilgisi.
• Değerlendirici notları (tarihli, gizli/paylaşımlı).
• Çoklu değerlendirici puanlaması ve konsensüs hesaplaması.
• Yetenek Havuzu (Talent Pool): Etiketleme, filtreleme, farklı pozisyonlara yönlendirme.
• Zaman aşımı: Verilen sürede tamamlamayan adaylar otomatik "Zaman Aşımı" statüsüne geçer.

D. Bildirim ve İletişim Motoru (Bu Modülün Soyutlaması)
• INotificationService soyutlaması (E-posta, SMS).
• Aşama başlangıcında adaya davet, tamamlanmada yöneticiye bilgi.
• Hatırlatma zamanlaması (son 24 saat, son 2 saat).
• Dahili bilgilendirme grupları (IT, İdari İşler vb. departman bildirimi).

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
SENDEN BEKLENEN ÇIKTILAR
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. DOMAIN KATMANI (Peoplise.Modules.ATS.Domain)
   - Aggregate Root'lar: Position, CandidateProcess, WorkflowDefinition.
   - Entity'ler: Stage, StageRule, CandidateNote, CandidateEvaluation,
     TalentPool, PositionTeamMember, CustomVariable, LegalDocument.
   - Value Object'ler: PositionId, CandidateId, StageType, EvaluationScore,
     PipelineStatus, WorkMode, SeniorityLevel.
   - Domain Event'ler: CandidateAppliedEvent, CandidateMovedToNextStageEvent,
     CandidateEliminatedEvent, StageCompletedEvent, EvaluationSubmittedEvent.
   - Invariant'lar (İş kuralları): "Bir aday aynı pozisyona iki kez başvuramaz",
     "Aşama geçişi ancak önceki aşama tamamlanmışsa yapılabilir",
     "Puan ortalaması hesaplanmadan konsensüs oluşturulamaz".

2. APPLICATION KATMANI (Peoplise.Modules.ATS.Application)
   CQRS Commands (en az 5):
   - CreatePositionCommand + Handler + FluentValidation
   - AddStageToWorkflowCommand + Handler
   - SubmitCandidateApplicationCommand + Handler
   - TransitionCandidateStageCommand + Handler (tetikleyici kurallarını kontrol eder)
   - SubmitEvaluationCommand + Handler
   CQRS Queries (en az 3):
   - GetPositionDashboardQuery (aday dağılımı, aşama istatistikleri)
   - GetCandidatePipelineQuery (filtrelenebilir aday listesi)
   - GetCandidateDetailQuery (tam profil + tüm değerlendirmeler)

3. INFRASTRUCTURE / VERİTABANI MODELİ
   - EF Core Fluent API konfigürasyonları (ilişkiler, indexler, unique constraint'ler).
   - Soft Delete ve TenantId filtreleri.
   - Migration'a hazır dosya.

4. UNIT TESTLER (en az 10 test)
   - Position Aggregate: Geçersiz aşama ekleme, takım üyesi yetki kontrolü.
   - CandidateProcess Aggregate: Aşama geçiş kuralları, puan hesaplaması.
   - Command Handler'lar: Mock repository ile doğru domain event fırlatma doğrulaması.
   - WorkflowEngine: Tetikleyici kural senaryoları (puan bazlı, süre bazlı).

KURALLAR:
• Eksik/yarım kod bırakma. Her handler, her entity, her test eksiksiz olacak.
• Domain Event'lerin fırlatılması (Raise) entity içinde, yayılması (Dispatch) handler'da olacak.
• Küçük ekip için okunabilir ve bakımı kolay kod üret.
• Mevcut Peoplise v5'teki iş kurallarını (PositionFlow, ProcessTaskFlow, Trigger, Notification)
  domain içinde explicit olarak modelle.
```

---

## AŞAMA 3 — Modül 2: İK Karşılama ve Ön Eleme Botu (HR Bot)

```
Sen Kıdemli bir .NET Backend ve Konuşma Akışı (Conversational Flow) Geliştiricisisin.
Peoplise v6 iskeleti üzerinde "Etkileşimli İK Ön Eleme ve Karşılama Botu (HR Bot)"
modülünü inşa edeceksin.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
İŞ MANTIĞI KAPSAMI
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

A. Sohbet Akışı Tasarımı (Flow Editor / Conversation Designer)
• Her pozisyon için özelleştirilebilir sohbet senaryosu (Flow) tanımlanır.
• Bir akış, sıralı "Adımlardan (Step)" oluşur. Her adımın bir tipi vardır:
  - Mesaj Gönder (SendMessage): Karşılama metni, bilgilendirme.
  - Hızlı Yanıt (SendQuickReply): Butonlu seçenekler ([Evet]/[Hayır], [1-3 Yıl]/[3-5 Yıl]).
  - Yanıt Bekle (WaitResponse): Adayın serbest metin yazmasını bekle.
  - Görsel Gönder (SendImage): Şirket logosu, infografik.
  - Video Gönder (SendVideo): Tanıtım videosu.
  - E-posta Gönder (SendEmail): Onay veya bilgilendirme maili.
  - Alt Akışa Dallan (SwitchFlow): Başka bir senaryo akışına geçiş.
  - SSS Motoru (FaqEngine): Adayın sorduğu soruyu bilgi bankasından yanıtla.
  - Dış Servis Çağrısı (CallWebHook): Harici sisteme veri gönder/al.

B. Koşullu Yönlendirme ve Dallanma (Step Route & Conditions)
• Bir adımdan diğerine geçiş koşullu olabilir:
  - Koşulsuz İlerleme (NoCondition): Doğrudan bir sonraki adıma geç.
  - Anahtar Kelime İçeriyor (HasAnyKeywords / HasAllKeywords): Adayın yanıtında
    belirli kelimelerin geçip geçmediğini kontrol et.
  - Anahtar Kelime İçermiyor (DoesNotContainsKeywords).
  - Tek Kelime Eşleştirme (HasOnlyKeyword): Butona tıklama yanıtı kontrolü.
• Dallanma sonucu adayı farklı adıma yönlendir veya süreci bitir (IsFinalStep).

C. Dinamik Veri Toplama ve Aday Profiline Besleme
• Sohbet sırasında adaydan toplanan yanıtlar (maaş beklentisi, lokasyon, tecrübe yılı vb.)
  Proje Değişkeni (ProjectVariable) ve Konuşma Değişkeni (ConversationVariable) olarak
  saklanır ve ATS modülündeki aday profiline aktarılır.
• Her konuşma (Conversation) bir adaya aittir ve durum bilgisi saklanır
  (Devam Ediyor, Tamamlandı, Elendi, Zaman Aşımı).

D. Bilgi Bankası ve SSS (Knowledgebase)
• Şirkete/pozisyona özel soru-cevap çiftleri sisteme girilir.
• Adayın serbest metin olarak sorduğu sorular bilgi bankasından aranır ve eşleşme
  bulunursa yanıtlanır. Eşleşme yoksa soru loglanır (İK ekibinin görmesi için).

E. Çok Kanallı Destek (Omnichannel)
• Web Chat (Ana kanal): Adaya gönderilen link ile tarayıcıda açılan sohbet penceresi.
• Facebook Messenger: Sosyal medya üzerinden bot ile etkileşim.
• Kanal bilgisi ConversationInterface enum ile saklanır.

F. Konuşma Geçmişi ve İzleme (Conversation Logs)
• Adım adım tüm konuşma akışı loglanır (CaseStepLog).
• İK uzmanı tüm konuşma dökümünü, adayın seçimlerini ve harcadığı süreyi görebilir.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
SENDEN BEKLENEN ÇIKTILAR
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. DOMAIN KATMANI
   Aggregate Root'lar: BotProject, Conversation.
   Entity'ler: Flow, Step, StepRoute, Knowledgebase, KnowledgebaseQuestion,
   KnowledgebaseAnswer, ProjectVariable, ConversationVariable, ConversationLog.
   Value Object'ler: StepType, ConditionType, StepRouteType, ConversationStatus,
   ConversationInterface.
   Domain Event'ler: ConversationStartedEvent, ConversationCompletedEvent,
   CandidateScreenedOutEvent, UnmatchedQuestionLoggedEvent.

2. APPLICATION KATMANI — ConversationProcessor (Akış Motoru)
   "Akıllı" motor: Adayın her yanıtında mevcut adımın StepRoute koşullarını değerlendirir,
   doğru dalı (branch) seçer ve sonraki adımı belirler. IsFinalStep ise konuşmayı kapatır.
   - StartConversationCommand + Handler
   - ProcessUserResponseCommand + Handler (ana motor — koşul değerlendirme)
   - GetConversationHistoryQuery + Handler

3. INFRASTRUCTURE
   EF Core konfigürasyonları, Flow → Step → StepRoute hiyerarşik ilişki mapping'leri.

4. UNIT TESTLER (en az 8 test)
   - ConversationProcessor: Koşulsuz ilerleme, anahtar kelime eşleşme, dallanma,
     IsFinalStep senaryoları.
   - Bilgi bankası arama: eşleşme bulunan ve bulunmayan durumlar.
   - Değişken toplama: Yanıtın doğru değişkene yazıldığının doğrulanması.
```

---

## AŞAMA 4 — Modül 3: Asenkron Video Mülakat ve Vaka Değerlendirme

```
Sen Kıdemli bir .NET Backend ve Medya İşleme Geliştiricisisin.
Peoplise v6 üzerinde "Asenkron Video Mülakat ve Vaka Değerlendirme (CaseBot)"
modülünü inşa edeceksin.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
İŞ MANTIĞI KAPSAMI
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

A. Video / Vaka Projesi Kurgusu
• Her değerlendirme birimi bir "CaseBot Project" olarak tanımlanır.
• Proje birden fazla "Flow" içerebilir; her Flow sıralı adımlardan (Step) oluşur.
• Adım Tipleri: Mesaj göster, Video sorusu oynat, Adaydan video kaydet,
  Adaydan doküman yükle, Not aldır, Takvim etkinliği ekle, Puan ver (SetPoint),
  Quick Reply, Boşluk doldurma (FillInTheBlank), Sepet sorusu (BasketQuestion),
  Yazılım geliştirme sorusu (SoftwareDevelopmentQuestion).

B. Aday Deneyimi Kuralları
• Düşünme Süresi (Preparation Time): Soru gösterildikten sonra geri sayım.
• Cevaplama Süresi (Recording Time): Kameranın açık olduğu maks. kayıt süresi.
• Tekrar Çekim Hakkı (Retake): Pozisyon politikasına göre 0, 1 veya N tekrar çekim.
• Medya Ön Kontrol: Kamera, mikrofon ve bağlantı testi (frontend tarafı).

C. Yetkinlik Modeli ve Puanlama (Competency Framework)
• Yetkinlikler (Competency): Analitik Düşünme, Liderlik, Problem Çözme vb.
• Her yetkinliğin altında Davranış Göstergeleri (BehavioralIndicator) ve
  Seviyeler (CompetencyLevel, 1-5 arası) tanımlanır.
• Her soru bir veya birden fazla yetkinlikle ilişkilendirilebilir.

D. Değerlendirici Deneyimi (Reviewer Scoring)
• Değerlendirici adayın videosunu izler, zaman damgalı notlar düşer.
• Önceden tanımlı rubrik (SetPoint) üzerinden puanlama yapar.
• Çoklu değerlendirici: Bağımsız puanlama, sonradan konsensüs tablosu.
• AI Ön Puanlama (ChatGptScoring): LLM ile otomatik kriter uygunluk taraması.

E. Aday Yetkinlik Raporu (Insight Report)
• Yetkinlik radar grafiği, güçlü yönler, gelişim önerileri.
• Aday karşılaştırma: Aynı pozisyondaki adayların yetkinlik sıralaması.
• Rapor şablonu (ReportTemplate): Bölümler, paneller ve özelleştirilebilir yapı.

F. Kod Değerlendirme (Software Development Question & AI Code Review)
• Adaya programlama sorusu sorulur, aday kod yazar.
• AI ile 5 boyutlu otomatik kod incelemesi:
  Okunabilirlik, İşlevsellik, Veri Doğrulama, Kullanım Senaryosu, Sözdizimi.
  Her kategori 20 üzerinden, toplam 100 puan.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
SENDEN BEKLENEN ÇIKTILAR
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. DOMAIN KATMANI
   Aggregate Root: CaseBotProject, Case (adayın bir projedeki oturumu).
   Entity'ler: Flow, Step (polimorfik adım detayları), StepRoute,
   Competency, CompetencyLevel, BehavioralIndicator, CaseStepConversation,
   CaseScoring, CaseResult, CompetencyResult, Report, ReportTemplate.
   Domain Event'ler: CaseStartedEvent, CaseCompletedEvent,
   VideoRecordedEvent (AI transkripsiyon tetikleyicisi), ScoringSubmittedEvent.

2. APPLICATION KATMANI
   Commands: CreateCaseBotProjectCommand, StartCandidateCaseCommand,
   SubmitVideoAnswerCommand (dosya depolama + arka plan transkripsiyon tetikleme),
   SubmitReviewerScoringCommand, RequestAICodeReviewCommand.
   Queries: GetCaseReportQuery, GetCandidateComparisonQuery.
   Background Job (MassTransit Consumer): VideoTranscriptionRequestedEvent
   → IAIProvider.TranscribeAsync → TranscriptionCompletedEvent.

3. INFRASTRUCTURE
   EF Core mapping'ler, IFileStorageService ile video/doküman depolama,
   IAIProvider ile transkripsiyon ve kod inceleme entegrasyonu.

4. UNIT TESTLER (en az 10 test)
   - Yetkinlik puanlama hesaplaması (kısmi puan, ağırlıklı ortalama).
   - Tekrar çekim hakkı sınır kontrolü.
   - AI Code Review: mock IAIProvider ile 5 boyutlu skor doğrulaması.
   - Rapor üretimi: Şablon bölümlerinin doğru doldurulduğunun testi.
```

---

## AŞAMA 5 — React Frontend İskeleti ve Ortak Bileşenler

```
Sen Kıdemli bir React + TypeScript Frontend Mimarısın.
Peoplise v6 platformunun yönetim paneli (Admin Panel) ve aday arayüzü (Candidate App)
için modern, erişilebilir ve ölçeklenebilir bir frontend iskeleti oluşturacaksın.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TEKNOLOJİ STACKI
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• React 19 + TypeScript 5
• Vite (Build tool)
• React Router v7 (Routing)
• TanStack Query v5 (Server state management / API cache)
• Zustand (Client state — Auth, Theme, Tenant)
• React Hook Form + Zod (Form validation)
• Tailwind CSS + shadcn/ui (UI component library)
• react-i18next (Türkçe + İngilizce lokalizasyon)
• Axios (HTTP client, interceptor ile JWT refresh)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
SENDEN BEKLENEN ÇIKTILAR
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. PROJE YAPISI
   apps/
     panel/          → Yönetim Paneli (İK Uzmanı, Yönetici)
     candidate/      → Aday Arayüzü (Bot, Video, Test)
   packages/
     ui/             → Paylaşımlı UI bileşenleri (Button, Modal, DataTable, Badge)
     api-client/     → Otomatik tiplenmiş API istemcisi (OpenAPI → TypeScript)
     i18n/           → Ortak çeviri dosyaları (tr.json, en.json)

2. PANEL UYGULAMASI — TEMEL SAYFA İSKELETLERİ
   - Layout: Sidebar navigasyon, Header (tenant seçici, dil, profil).
   - Dashboard: Pozisyon kartları, aday dağılım özeti.
   - Pozisyon Listesi / Detay / Akış Editörü (sürükle-bırak aşama tasarımcısı wireframe).
   - Aday Pipeline: Kanban board görünümü.
   - Aday Detay: Profil, değerlendirme timeline, video izleme, not ekleme.

3. ADAY UYGULAMASI — TEMEL SAYFA İSKELETLERİ
   - Bot Sohbet Ekranı: Mesaj baloncukları, hızlı yanıt butonları, medya görüntüleme.
   - Video Mülakat Ekranı: Kamera önizleme, geri sayım, kayıt kontrolü.
   - Test / Anket Ekranı: Soru tipleri render (çoktan seçmeli, matris, sıralama vb.).

4. ORTAK MEKANİZMALAR
   - Auth Context: JWT token yönetimi, otomatik refresh, 401 redirect.
   - Tenant Context: URL veya subdomain'den tenant çözümleme.
   - Protected Route: Rol bazlı sayfa erişim kontrolü.
   - Global Error Boundary ve Toast notification sistemi.

KURALLAR:
• Tüm bileşenler TypeScript strict mode ile yazılacak.
• API çağrıları TanStack Query hook'ları ile yapılacak (useQuery, useMutation).
• Responsive tasarım (mobil öncelikli): Aday arayüzü %90 mobil kullanılacak.
• Erişilebilirlik (a11y): Semantic HTML, ARIA etiketleri, klavye navigasyonu.
```

---

## Kullanım Rehberi ve İpuçları

> [!IMPORTANT]
> ### Sıralı Çalıştırma Zorunluluğu
> Her aşama bir öncekinin çıktısına bağımlıdır. Aşama 2'yi vermeden önce
> Aşama 1'in ürettiği `SharedKernel`, `Infrastructure` ve `AppDbContext`
> kodlarının context'e eklenmesi gerekir.

> [!TIP]
> ### Küçük Ekip İçin Pragmatik Tavsiyeler
> - **Modular Monolith** ile başlayın. 1-3 kişilik ekiple 8 ayrı mikroservis
>   yönetmek sürdürülemez; tek solution, modüler sınırlar yeterlidir.
> - **Test-First yaklaşımı:** Her aşamada "önce testleri, sonra implementasyonu yaz"
>   komutunu verin. Bu, AI'ın yarım/placeholder kod üretmesini engeller.
> - **Veri göçü ayrı bir aşamada ele alınmalıdır.** Yeni şema kararlılık
>   kazandıktan sonra v5 → v6 migration scriptleri yazılmalıdır.

> [!WARNING]
> ### Tek Prompt Tuzağı
> Tüm 8 modülü tek prompt ile yazdırmaya çalışmayın. AI'ın context limiti
> aşılır ve üretilen kod kalitesi dramatik düşer. Her aşamayı ayrı bir
> konuşma oturumunda veya `/goal` komutuyla çalıştırın.
