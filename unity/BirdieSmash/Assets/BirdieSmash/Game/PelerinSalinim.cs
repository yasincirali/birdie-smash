// Pelerin salınımı (2026-11-01, yeni skinler: "pelerin vs gibi mistik şeyler"). Kemik yok, tek eksenli yay: pelerin kökü
// (omuz çizgisi, sırt yüzeyi) yatay eksende geriye açılır. Hedef açı kökün dünya hızının ileri bileşeninden (koşarken
// savrulur, geri geri giderken bacağa yapışır, sıfırın altına inmez) ve yana hızdan küçük bir burulma; yay-sönüm ile izler,
// böylece durunca birkaç salınımla yerine gelir. Hareketi azalt açıkken sabit durur (Ayar.reduce). Kare süresinden bağımsız
// olsun diye adım en fazla 1/30 sn (takılan karede patlamaz). Yalnız görünüş: maç hesabına girmez.
using UnityEngine;

namespace BirdieSmash.Game
{
    public class PelerinSalinim : MonoBehaviour
    {
        const float K = 70f, D = 11f, ILERI_DERECE = 26f, AZAMI = 60f, BURULMA = 10f;
        Quaternion r0; Vector3 onceki; bool ilk = true; float aci, hiz, bur, burHiz;

        void Start() { r0 = transform.localRotation; }

        void LateUpdate()
        {
            if (Ayar.reduce) { transform.localRotation = r0; ilk = true; return; }
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f); if (dt <= 0) return;
            var p = transform.position;
            if (ilk) { onceki = p; ilk = false; return; }
            var v = (p - onceki) / Mathf.Max(1e-4f, Time.deltaTime); onceki = p;
            var par = transform.parent != null ? transform.parent.rotation * r0 : transform.rotation;
            float ileri = Vector3.Dot(v, par * Vector3.forward), yan = Vector3.Dot(v, par * Vector3.right);
            float hedef = Mathf.Clamp(ileri * ILERI_DERECE, 0f, AZAMI) + 4f;          // 4°: durgunken bile sırttan hafif açık
            float hedefB = Mathf.Clamp(-yan * BURULMA, -18f, 18f);
            hiz += (K * (hedef - aci) - D * hiz) * dt; aci += hiz * dt;
            burHiz += (K * (hedefB - bur) - D * burHiz) * dt; bur += burHiz * dt;
            transform.localRotation = r0 * Quaternion.Euler(Mathf.Clamp(aci, -5f, AZAMI + 10f), bur, 0);
        }
    }
}
