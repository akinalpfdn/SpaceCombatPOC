# SpaceCombat Project Rules

Bu dosya Claude Code'un her konusmada otomatik okudugu kural setidir.
**Bu projede "basit tut / over-engineering yapma" varsayilani GECERSIZDIR.**
Kod her zaman production-grade, scalable ve SOLID uyumlu olmalidir.

---

## Kullanici Profili

- Kullanici Unity'yi yeni ogreniyor, bu ilk projesi.
- Aciklamalar ogretici olmali: "ne yapiyoruz" degil "neden yapiyoruz" ve "bu Unity'de ne ise yariyor" da anlatilmali.
- Yeni bir Unity konsepti (component, material, shader, ScriptableObject, prefab, vb.) ilk kez kullanildiginda kisa bir aciklama ekle.
- Teknik kararlarda seceneklerin artilerini/eksilerini goster, sadece "su sekilde yap" deme.
- Inspector'da yapilacak islemleri adim adim tarif et (hangi obje → hangi component → hangi field).

---

## Mimari Prensipler

### SOLID - Her Zaman Uyulmali
- **Single Responsibility:** Her sinif tek bir sorumluluk tasir. Yeni sorumluluk = yeni sinif.
- **Open/Closed:** Davranis degisikligi yeni sinif/strateji ile yapilir, mevcut sinif degistirilmez.
- **Liskov Substitution:** Alt siniflar, ust sinifin yerine gecebilmeli.
- **Interface Segregation:** Buyuk interface'ler parcalanir, client sadece ihtiyaci olani implement eder.
- **Dependency Inversion:** Concrete class'lara degil, interface/abstraction'lara bagimli ol.

### Design Pattern Kullanimi
- Yeni sistem eklerken uygun design pattern sec ve uygula.
- Projede aktif kullanilan patternler: **Strategy, Observer (EventBus), Factory, State Machine, Template Method, Service Layer, Object Pool, Facade, DI (VContainer)**.
- Pattern secimi keyfi degil, problem tipiyle eslesmelidir.
- Pattern uygularken interface tanimi ile basla.

### Dependency Injection - VContainer
- Tum dependency'ler `[Inject] public void Construct(...)` ile alinir.
- Yeni servis eklendiginde `GameLifetimeScope.Configure()` icinde register edilmelidir.
- Servisler mumkunse interface uzerinden register edilir: `.As<IMyService>()`.
- Pool-spawned objeler (Enemy, Projectile) icin `IObjectResolver.InjectGameObject()` kullanilir.
- **YASAK:** `static Instance`, `ServiceLocator`, `FindObjectOfType`, `GameObject.Find`, `FindWithTag`.

### Event-Driven Communication
- Sistemler arasi iletisim `EventBus.Publish/Subscribe` ile yapilir.
- Direct reference yerine event kullan (UI ← EventBus ← Combat gibi).
- Yeni event eklerken `GameEvents.cs` icinde tanimla, `IGameEvent` implement et.

---

## Kod Standartlari

### Naming Conventions
- **Siniflar:** PascalCase → `WeaponController`, `EnemySpawnService`
- **Interface'ler:** `I` prefix → `ISpawnService`, `IDamageable`
- **Private field'lar:** `_camelCase` → `_currentHealth`, `_poolManager`
- **Public property'ler:** PascalCase → `CurrentHealth`, `IsAlive`
- **Metodlar:** PascalCase → `Initialize()`, `TryFire()`
- **Sabitler:** ALL_CAPS → `MAX_ATTEMPTS`, `DEFAULT_Y_POSITION`
- **Event'ler:** `On` prefix → `OnHealthChanged`, `OnDeath`
- **Enum'lar:** PascalCase → `DamageType`, `EnemyState`

### Dosya ve Sinif Organizasyonu
- **Bir dosya = bir sinif.** Dosya adi sinif adiyla ayni olmali.
- **Namespace:** Klasor yapisiyla eslesen `SpaceCombat.*` namespace kullan.
  ```
  Core/ → SpaceCombat.Core
  AI/States/ → SpaceCombat.AI.States
  Combat/ → SpaceCombat.Combat
  ```

### MonoBehaviour Yapisi (Siralama)
```csharp
// 1. Serialized fields ([Header] ile grupla)
[Header("Configuration")]
[SerializeField] private MyConfig _config;

// 2. Injected dependencies (private field + [Inject] Construct)
private IMyService _myService;

[Inject]
public void Construct(IMyService myService)
{
    _myService = myService;
}

// 3. Runtime state (private fields)
private float _timer;

// 4. Public properties
public float Timer => _timer;

// 5. Events
public event Action OnTimerComplete;

// 6. Unity lifecycle (Awake → Start → Update → FixedUpdate → OnDestroy)
// 7. Public methods
// 8. Private methods
// 9. Event handlers
// 10. Debug/Editor methods
```

### Section Comments
Buyuk siniflarda bolum ayiricilar kullan:
```csharp
// ============================================
// INITIALIZATION
// ============================================
```

### SerializeField Kullanimi
- Private field'lar `[SerializeField]` ile expose edilir, public field KULLANMA.
- `[Header("...")]` ile inspector'da grupla.
- `[Range(...)]`, `[Tooltip("...")]` ile constraint ve dokumantasyon ekle.
- Debug toggle'lari ayri header altinda:
  ```csharp
  [Header("Debug")]
  [SerializeField] private bool _debugLogEnabled = false;
  ```

### Dokumantasyon
- Her public sinif `/// <summary>` XML doc ile dokumante edilir.
- Dosya basi yorum blogu ile sistem amaci ve kullanilan patternler belirtilir.
- Non-obvious logic icin inline yorum ekle, trivial kod icin ekleme.

---

## ScriptableObject Driven Design
- Oyun balance degerlerini, konfigurasyonlari ScriptableObject'lerde tut.
- Yeni config tipi eklerken `[CreateAssetMenu]` attribute kullan.
- Mevcut config'ler: `ShipConfig`, `EnemyConfig`, `WeaponConfig`, `SpawnConfig`, `GameBalanceConfig`.

## Object Pooling
- Sik olusturulan/yok edilen objeler (projectile, VFX, enemy) icin `ObjectPool<T>` kullan.
- `IPoolable` interface'ini implement et: `OnSpawn()`, `OnDespawn()`, `ResetState()`.
- Pool'lar `PoolManager` uzerinden yonetilir.

## Koordinat Sistemi
- 3D dunya, XZ duzleminde top-down gameplay (Y = yukari).
- Event'ler ve UI icin `Vector2(x, z)` donusumu tutarli yapilir.
- Her spawn/hareket isleminde `position.y = 0f` kontrolu.

---

## Yasak Patternler - ASLA KULLANMA
1. `static Instance` singleton pattern
2. `ServiceLocator` pattern
3. `FindObjectOfType<T>()`, `GameObject.Find()`, `FindWithTag()`
4. Public field'lar (inspector icin `[SerializeField] private` kullan)
5. God class / mega-method (300+ satirlik metod)
6. String-based component lookup
7. `Awake()` icinde baska objelere erisim (injection sirasina guvenme)

---

## Yeni Sistem Ekleme Checklist
1. [ ] Interface tanimla (`Interfaces/` klasorunde)
2. [ ] Concrete sinif olustur (uygun klasorde, tek dosya)
3. [ ] Dogru namespace kullan (`SpaceCombat.*`)
4. [ ] Dependency'leri `[Inject] Construct(...)` ile al
5. [ ] `GameLifetimeScope`'a register et
6. [ ] Gerekli event'leri `GameEvents.cs`'e ekle
7. [ ] ScriptableObject config gerekiyorsa olustur
8. [ ] Poolable objeyse `IPoolable` implement et
9. [ ] XML doc ve section comment'leri ekle
