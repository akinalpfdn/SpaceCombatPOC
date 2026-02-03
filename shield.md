Güzel özellik! Shield visual aslında çok zor değil. Adım adım gidelim:

🛡️ Shield Sistemi - Genel Bakış
Lazerin kalkana çarpması:
1. Lazer → Shield collider'a çarpar
2. Shield HP azalır
3. Shield visual kısa süreliğine görünür (hit effect)
4. Shield HP 0 olunca → kalkan kırılır, artık hull'a hasar gider

🎯 Implementation Adımları
Adım 1: Shield Visual (3D Sphere veya 2D Sprite)
Option A: 3D Approach (Önerim)
- Geminin etrafına yarı-saydam sphere mesh
- Shader ile glow/fresnel efekt
- Normal zamanda invisible (alpha = 0)
- Hit anında alpha flash → fade out
Option B: 2D Sprite Approach
- Bubble/dome sprite geminin üstünde
- SpriteRenderer ile alpha kontrol
- Daha basit, daha az performans maliyeti

Adım 2: Shield Shader (URP için basit)
Shader özellikleri:
- Fresnel effect (kenarlar daha parlak)
- Hit noktasında ripple/wave animasyonu
- Renk: Cyan/mavi (veya gemi rengine göre)
- Transparency: Normal 0%, hit anında 50-80%

Adım 3: Script Mantığı
csharppublic class ShieldVisual : MonoBehaviour
{
    [SerializeField] private Renderer shieldRenderer;
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private Color shieldColor = new Color(0, 1, 1, 0.5f);
    
    private Material shieldMaterial;
    private Coroutine flashCoroutine;
    
    private void Awake()
    {
        shieldMaterial = shieldRenderer.material;
        SetShieldAlpha(0f); // Başta görünmez
    }
    
    public void OnShieldHit(Vector3 hitPoint)
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        
        flashCoroutine = StartCoroutine(FlashShield(hitPoint));
    }
    
    private IEnumerator FlashShield(Vector3 hitPoint)
    {
        // Hit noktasını shader'a gönder (ripple için)
        shieldMaterial.SetVector("_HitPoint", hitPoint);
        
        // Flash in
        SetShieldAlpha(0.6f);
        
        // Fade out
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.6f, 0f, elapsed / flashDuration);
            SetShieldAlpha(alpha);
            yield return null;
        }
        
        SetShieldAlpha(0f);
    }
    
    private void SetShieldAlpha(float alpha)
    {
        Color color = shieldColor;
        color.a = alpha;
        shieldMaterial.SetColor("_BaseColor", color);
    }
}

Adım 4: Damage System Entegrasyonu
Mevcut damage sistemine ekle:
csharppublic void TakeDamage(float damage, Vector3 hitPoint)
{
    if (currentShield > 0)
    {
        // Shield'a hasar
        currentShield -= damage;
        
        // Visual feedback
        shieldVisual.OnShieldHit(hitPoint);
        
        if (currentShield <= 0)
        {
            currentShield = 0;
            OnShieldBroken(); // Kalkan kırılma efekti
        }
    }
    else
    {
        // Hull'a hasar
        currentHealth -= damage;
        // Explosion/spark efekti
    }
}