// Ekran okuyucu (VoiceOver/TalkBack) için etiket kaydı (komple-oyun tasarımı §2.4, Ek A.3; 2026-10-26).
// Neden bu kadarı: Unity 6'da ekran okuyucuya ağaç verme yolu UnityEngine.Accessibility (AccessibilityHierarchy,
// AssistiveSupport). Bulut derleme denetimi Unity 2021.3 modül DLL'leriyle yapılıyor ve o sürümde bu API yok; adını
// tahmin edip yazmak CI'daki gerçek Unity 6 derlemesini kırabilir. Bu yüzden şimdilik her etkileşimli öğe etiketini, rolünü
// ve durumunu burada taşır (ErisimBilgi, VisualElement.userData), Topla() görünür öğeleri ekran dikdörtgeniyle listeler.
// Köprü tek adım kalır:
// TODO(Unity 6000.0 belgeleriyle doğrula, sonra yaz; derleme kapısı #if UNITY_6000_0_OR_NEWER):
//   var h = new UnityEngine.Accessibility.AccessibilityHierarchy();
//   foreach (var d in liste) { var n = h.AddNode(d.etiket);              // AddNode(string label, AccessibilityNode parent = null)
//       n.role = AccessibilityRole.Button;                                // rol "dugme" | "sekme" (TabBar?) | "baslik" (Header) | "metin" (StaticText)
//       n.state = d.secili ? AccessibilityState.Selected : (d.pasif ? AccessibilityState.Disabled : AccessibilityState.None);
//       n.value = d.deger;  n.frame = d.ekran;                             // frame ekran pikseli, sol üst köken mi? (doğrula)
//       n.selected += () => { /* d.oge'nin Clickable'ını tetikle */ return true; }; }   // olay imzası Func<bool> mı Action mı? (doğrula)
//   UnityEngine.Accessibility.AssistiveSupport.activeHierarchy = h;      // menü görünürken; maçta yalnız Duraklat + skor düğümü
//   ekran değişince AssistiveSupport.notificationDispatcher.SendLayoutChanged() (ya da SendScreenChanged) çağrılır.
//   AssistiveSupport.isScreenReaderEnabled false iken ağaç kurulmaz (bedava kalsın).
// Ayrıca Unity 6'da UnityEngine.Accessibility.AccessibilitySettings.fontScale var mı bakılmalı: varsa Yerel.YaziOlcek'in
// yerel eklentisi yerine o okunabilir (aynı değer, bir eklenti daha az).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BirdieSmash.Game
{
    public class ErisimBilgi { public string etiket, rol, deger; public bool secili, pasif; }

    public static class Erisim
    {
        public struct Dugum { public string etiket, rol, deger; public bool secili, pasif; public Rect alan; public VisualElement oge; }

        public static void Etiket(VisualElement el, string etiket, string rol, bool secili = false, bool pasif = false, string deger = null)
        {
            if (el == null) return; el.userData = new ErisimBilgi { etiket = etiket, rol = rol, secili = secili, pasif = pasif, deger = deger };
        }

        // Görünür ve etiketli öğeler, panel koordinatında (worldBound). Yalnız ağaç kurulurken çağrılır, karede değil.
        public static void Topla(VisualElement kok, List<Dugum> liste)
        {
            if (kok == null) return;
            if (kok.resolvedStyle.display == DisplayStyle.None || kok.resolvedStyle.visibility == Visibility.Hidden) return;
            var b = kok.userData as ErisimBilgi;
            if (b != null) liste.Add(new Dugum { etiket = b.etiket, rol = b.rol, deger = b.deger, secili = b.secili, pasif = b.pasif, alan = kok.worldBound, oge = kok });
            for (int i = 0; i < kok.childCount; i++) Topla(kok[i], liste);
        }
    }
}
