// Ekran üstü: menü (mod, kariyer basamağı, Oyna, Antrenman, Ayarlar, Lisanslar), maç içi skor ve ipuçları, rakip giriş
// kartı (1,5 sn), antrenman kartı, nefes etiketi, duraklat/devam, maç sonu. IMGUI (OnGUI): paket gerektirmez; demo için
// yeterli, son ürün UI Toolkit'e taşınır (VoiceOver/TalkBack etiketi IMGUI'de yok: bilinen eksik).
// Dokunma alanı: her düğme en az 44/640 ekran yüksekliği (iPhone SE 667 pt dahil ≥ 44 pt); güvenli alan Screen.safeArea.
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class Hud : MonoBehaviour
    {
        public GameRoot root; public string panel = "ana";
        GUIStyle big, mid, small, tiny, btn, btnP, chip, chipOn, card, left, leftB; float u; Vector2 scroll;
        class Float { public string t; public Vector3 w; public float a; public Color c; public float size; }
        readonly List<Float> floats = new List<Float>(); readonly List<Rect> blocks = new List<Rect>();
        string ogrMsg; float ogrMsgT; string lisans;

        public void Say(string t, Vector3 world, Color c, float size = 1) { if (string.IsNullOrEmpty(t)) return; floats.Add(new Float { t = t, w = world, c = c, size = size }); if (floats.Count > 6) floats.RemoveAt(0); }
        public void Ogr(string t) { ogrMsg = t; ogrMsgT = 1.8f; }
        public bool Blocks(Vector2 guiPos) { foreach (var r in blocks) if (r.Contains(guiPos)) return true; return false; }

        void Update()
        {
            float d = Time.unscaledDeltaTime; for (int i = floats.Count - 1; i >= 0; i--) { floats[i].a += d; if (floats[i].a > 1.2f) floats.RemoveAt(i); }
            if (ogrMsgT > 0) ogrMsgT -= d;
        }

        static Texture2D Solid(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }
        void Styles()
        {
            float nu = Mathf.Min(Screen.height / 640f, Screen.width / 340f); if (big != null && Mathf.Abs(nu - u) < .001f) return; u = nu;
            big = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(32 * u), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            mid = new GUIStyle(big) { fontSize = Mathf.RoundToInt(19 * u) };
            small = new GUIStyle(big) { fontSize = Mathf.RoundToInt(13 * u), fontStyle = FontStyle.Normal };
            tiny = new GUIStyle(small) { fontSize = Mathf.RoundToInt(11 * u) };
            left = new GUIStyle(small) { alignment = TextAnchor.UpperLeft }; leftB = new GUIStyle(left) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(15 * u) };
            btn = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(15 * u), fontStyle = FontStyle.Bold, wordWrap = true };
            btn.normal.background = Solid(Kit.Hex("#22343a")); btn.hover.background = btn.active.background = Solid(Kit.Hex("#2e464e")); btn.normal.textColor = btn.hover.textColor = btn.active.textColor = Color.white;
            btnP = new GUIStyle(btn); btnP.normal.background = Solid(Kit.Hex("#d8ff5a")); btnP.hover.background = btnP.active.background = Solid(Kit.Hex("#e6ff8c")); btnP.normal.textColor = btnP.hover.textColor = btnP.active.textColor = Kit.Hex("#0d1a12");
            chip = new GUIStyle(btn) { fontSize = Mathf.RoundToInt(13 * u), fontStyle = FontStyle.Normal }; chipOn = new GUIStyle(chip) { fontStyle = FontStyle.Bold };
            chipOn.normal.background = chipOn.hover.background = chipOn.active.background = Solid(Kit.Hex("#7ad7ff")); chipOn.normal.textColor = chipOn.hover.textColor = chipOn.active.textColor = Kit.Hex("#07161c");
            card = new GUIStyle(GUI.skin.box); card.normal.background = Solid(new Color(.04f, .08f, .09f, .86f));
        }

        static void Shadowed(Rect r, string t, GUIStyle s, Color c)
        {
            var o = GUI.color; GUI.color = new Color(0, 0, 0, c.a * .6f); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), t, s);
            GUI.color = c; GUI.Label(r, t, s); GUI.color = o;
        }
        bool Button(Rect r, string t, bool primary = false) { blocks.Add(r); return GUI.Button(r, t, primary ? btnP : btn); }
        bool Chip(Rect r, string t, bool on) { blocks.Add(r); return GUI.Button(r, (on ? "✓ " : "") + t, on ? chipOn : chip); }   // seçili olan yalnız renkle değil işaretle de okunur

        float H, W, top, bot, lx, rx, BH; Rect safe;
        void Layout()
        {
            W = Screen.width; H = Screen.height; safe = Screen.safeArea;
            top = H - safe.yMax + 8 * u; bot = safe.yMin + 10 * u; lx = Mathf.Max(safe.xMin, 0) + 16 * u; rx = W - Mathf.Max(W - safe.xMax, 0) - 16 * u;
            BH = Mathf.Max(46 * u, 44 * H / 640f);
        }

        void OnGUI()
        {
            var M = root.match; if (M == null) return; Styles(); Layout(); blocks.Clear();
            if (root.ekran == Ekran.Menu) { Menu(M); return; }
            Play(M);
            if (root.paused) Pause(M);
            if (root.ekran == Ekran.Bitti) Over(M);
        }

        void Dim(float a) { GUI.color = new Color(0, 0, 0, a); GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture); GUI.color = Color.white; }
        float ColW { get { return Mathf.Min(rx - lx, 360 * u); } }
        float ColX { get { return (W - ColW) / 2; } }

        // ---- menü ----
        void Menu(Match M)
        {
            Dim(.55f); float x = ColX, w = ColW, y = top + 30 * u, g = 10 * u;
            Shadowed(new Rect(x, y, w, 50 * u), "Birdie Smash", big, Color.white); y += 46 * u;
            Shadowed(new Rect(x, y, w, 22 * u), "Unity demosu · HTML oyunla aynı kurallar", small, new Color(1, 1, 1, .75f)); y += 40 * u;
            if (panel == "ayar") { Ayarlar(x, y, w, g); return; }
            if (panel == "lisans") { Lisans(x, y, w); return; }
            float hw = (w - g) / 2;
            if (Chip(new Rect(x, y, hw, BH), "Serbest maç", !Ayar.kariyerModu)) { Ayar.kariyerModu = false; Ayar.Kaydet(); }
            if (Chip(new Rect(x + hw + g, y, hw, BH), "Kariyer", Ayar.kariyerModu)) { Ayar.kariyerModu = true; Ayar.Kaydet(); }
            y += BH + g;
            string line = Ayar.kariyerModu ? Career.Line(Ayar.kariyer) : Ayar.target + " sayı · " + (Ayar.easy ? "Kolay" : "Usta") + " · " + CourtView.SAHA[Ayar.saha].ad;
            Shadowed(new Rect(x, y, w, 24 * u), line, small, Kit.Hex("#d8ff5a")); y += 24 * u;
            if (Ayar.kariyerModu)
            {   // basamak çubuğu: geçilen dolu, sıradaki çerçeveli (yalnız renkle değil, biçimle de)
                float bw = (w - 5 * 6 * u) / 6;
                for (int i = 0; i < 6; i++) { var r = new Rect(x + i * (bw + 6 * u), y + 4 * u, bw, 8 * u); GUI.color = i < Ayar.kariyer ? Kit.Hex("#d8ff5a") : i == Ayar.kariyer ? Kit.Hex("#7ad7ff") : new Color(1, 1, 1, .2f); GUI.DrawTexture(i == Ayar.kariyer ? new Rect(r.x, r.y - 2 * u, r.width, r.height + 4 * u) : r, Texture2D.whiteTexture); }
                GUI.color = Color.white; y += 22 * u;
            }
            y += g;
            if (Button(new Rect(x, y, w, BH * 1.2f), "Oyna", true)) root.Oyna(); y += BH * 1.2f + 4 * u;
            if (!Ayar.antrenman) { Shadowed(new Rect(x, y, w, 20 * u), "İlk kez: 3 adımlık antrenman, sonra 5 sayılık maç", tiny, new Color(1, 1, 1, .7f)); y += 20 * u; }
            y += g;
            if (Button(new Rect(x, y, hw, BH), "Antrenman")) root.Antrenman();
            if (Button(new Rect(x + hw + g, y, hw, BH), "Ayarlar")) panel = "ayar"; y += BH + g;
            if (Ayar.bestRally > 0 || Ayar.wins > 0) { Shadowed(new Rect(x, y, w, 22 * u), "Rekor ralli " + Ayar.bestRally + " · Galibiyet " + Ayar.wins, small, Color.white); y += 26 * u; }
            if (Button(new Rect(x, y, w, BH), "Lisanslar ve üçüncü taraf içerik")) { panel = "lisans"; scroll = Vector2.zero; }
        }

        void Satir(ref float y, float x, float w, string baslik) { Shadowed(new Rect(x, y, w, 22 * u), baslik, left, new Color(1, 1, 1, .8f)); y += 22 * u; }
        void Ayarlar(float x, float y, float w, float g)
        {
            float hw = (w - g) / 2, tw = (w - 2 * g) / 3;
            Satir(ref y, x, w, "Zorluk");
            if (Chip(new Rect(x, y, hw, BH), "Kolay", Ayar.easy)) { Ayar.easy = true; Ayar.Kaydet(); }
            if (Chip(new Rect(x + hw + g, y, hw, BH), "Usta", !Ayar.easy)) { Ayar.easy = false; Ayar.Kaydet(); } y += BH + g;
            Satir(ref y, x, w, "Maç uzunluğu");
            int[] L = { 5, 11, 21 }; for (int i = 0; i < 3; i++) if (Chip(new Rect(x + i * (tw + g), y, tw, BH), L[i] + " sayı", Ayar.target == L[i])) { Ayar.target = L[i]; Ayar.Kaydet(); } y += BH + g;
            Satir(ref y, x, w, "Saha" + (Ayar.kariyerModu ? " (kariyerde basamağın mekânı)" : ""));
            for (int i = 0; i < 4; i++) { var id = CourtView.SIRA[i]; if (Chip(new Rect(x + (i % 2) * (hw + g), y + (i / 2) * (BH + g), hw, BH), CourtView.SAHA[id].ad, Ayar.saha == id)) { Ayar.saha = id; Ayar.Kaydet(); root.SahaDegisti(); } }
            y += 2 * (BH + g);
            Satir(ref y, x, w, "Oyuncu görünümü");
            if (Chip(new Rect(x, y, hw, BH), "Oyuncak 3B", Ayar.model == "oyuncak")) { Ayar.model = "oyuncak"; Ayar.Kaydet(); root.ModelDegisti(); }
            if (Chip(new Rect(x + hw + g, y, hw, BH), "Gerçekçi", Ayar.model == "rocketbox")) { Ayar.model = "rocketbox"; Ayar.Kaydet(); root.ModelDegisti(); } y += BH + g;
            Satir(ref y, x, w, "Hareketi azalt · Ses");
            if (Chip(new Rect(x, y, hw, BH), Ayar.reduce ? "Hareket azaltıldı" : "Hareket tam", Ayar.reduce)) { Ayar.reduce = !Ayar.reduce; Ayar.Kaydet(); }
            if (Chip(new Rect(x + hw + g, y, hw, BH), Ayar.ses ? "Ses açık" : "Ses kapalı", Ayar.ses)) { Ayar.ses = !Ayar.ses; AudioListener.volume = Ayar.ses ? 1 : 0; Ayar.Kaydet(); } y += BH + g * 2;
            if (Button(new Rect(x, y, w, BH), "Geri")) panel = "ana";
        }
        void Lisans(float x, float y, float w)
        {
            if (lisans == null) { var t = Resources.Load<TextAsset>("lisanslar"); lisans = t != null ? t.text : "Lisans metni bulunamadı (Resources/lisanslar.txt)."; }
            float h = H - y - bot - BH - 20 * u; var r = new Rect(x, y, w, h); blocks.Add(r);
            GUI.Box(r, GUIContent.none, card);
            float ch = left.CalcHeight(new GUIContent(lisans), w - 30 * u);
            scroll = GUI.BeginScrollView(new Rect(x + 6 * u, y + 6 * u, w - 12 * u, h - 12 * u), scroll, new Rect(0, 0, w - 30 * u, ch));
            GUI.Label(new Rect(0, 0, w - 30 * u, ch), lisans, left); GUI.EndScrollView();
            if (Button(new Rect(x, y + h + 10 * u, w, BH), "Geri")) panel = "ana";
        }

        // ---- maç içi ----
        void Play(Match M)
        {
            Shadowed(new Rect(0, top, W, 40 * u), M.ogr != null ? "Antrenman" : M.sc[0] + "  –  " + M.sc[1], M.ogr != null ? mid : big, Color.white);
            if (M.ogr == null) Shadowed(new Rect(0, top + 36 * u, W, 18 * u), "SEN" + new string(' ', 16) + M.RakipAd(false).ToUpper(), tiny, new Color(1, 1, 1, .7f));
            if (root.ekran == Ekran.Oyun && !root.paused && Button(new Rect(rx - BH, top, BH, BH), "II")) root.Duraklat();   // Duraklat
            float y = top + 58 * u;
            if (root.rakipKartT > 0 && M.ogr == null) y = RakipKart(M, y);
            if (M.ogr != null) y = OgrKart(M, y);
            if (!string.IsNullOrEmpty(M.serveMsg) && M.s == null && M.ogr == null && root.rakipKartT <= 0) Shadowed(new Rect(lx, y, rx - lx, 40 * u), M.serveMsg, small, Kit.Hex("#d8ff5a"));
            float hy = H - bot - 40 * u;
            if (M.s == null && M.server == "me" && !M.between && !M.over && M.ogr == null && M.svPending == null) Shadowed(new Rect(lx, hy, rx - lx, 36 * u), "Servis: yavaş = kısa · kısa sert fiske = flick · uzun hızlı = yüksek", small, Color.white);
            if (M.HitWindow() && (M.ogr == null || !M.ogr.dur)) Shadowed(new Rect(0, hy - 34 * u, W, 30 * u), "Şimdi kaydır!", mid, Kit.Hex("#d8ff5a"));
            NefesYazi(M.me, true, M); NefesYazi(M.op, false, M);
            foreach (var f in floats)
            {
                var sp = root.cam.WorldToScreenPoint(f.w + Vector3.up * (.6f + f.a * .5f)); if (sp.z < 0) continue;
                var c = f.c; c.a = Mathf.Clamp01(1.3f - f.a); var st = new GUIStyle(mid) { fontSize = Mathf.RoundToInt(19 * u * f.size) };
                float yy = Mathf.Clamp(H - sp.y - 20 * u, top + 60 * u, H - bot - 80 * u);
                Shadowed(new Rect(lx, yy, rx - lx, 40 * u), f.t, st, c);
            }
            if (ogrMsgT > 0 && ogrMsg != null) { var c = Kit.Hex("#d8ff5a"); c.a = Mathf.Clamp01(ogrMsgT * 2); Shadowed(new Rect(lx, H * .52f, rx - lx, 60 * u), ogrMsg, mid, c); }
        }
        void NefesYazi(Athlete o, bool isMe, Match M)
        {
            if (o.nefes >= .4f || M.ogr != null) return;
            var sp = root.cam.WorldToScreenPoint(Kit.W(o.x + (isMe ? .9f : -.9f), o.y, 0)); if (sp.z < 0) return;
            Shadowed(new Rect(sp.x - 50 * u, H - sp.y - 10 * u, 100 * u, 20 * u), isMe ? "Nefes" : "Yorgun", small, Kit.Hex("#ff9f7a"));
        }
        float RakipKart(Match M, float y)
        {
            var st = M.stil; float w = ColW, x = ColX, h = 92 * u; var r = new Rect(x, y, w, h); blocks.Add(r); GUI.Box(r, GUIContent.none, card);
            GUI.color = Kit.Hex(Match.OpRenk(st, Ayar.FORMA)); GUI.DrawTexture(new Rect(x, y, 6 * u, h), Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(x + 16 * u, y + 8 * u, w - 24 * u, 26 * u), st.isim, new GUIStyle(leftB) { fontSize = Mathf.RoundToInt(20 * u) });
            GUI.Label(new Rect(x + 16 * u, y + 34 * u, w - 24 * u, 18 * u), st.kariyer >= 0 ? st.ad + " · " + Match.KARIYER[st.kariyer].yer : st.ad, left);
            GUI.Label(new Rect(x + 16 * u, y + 54 * u, w - 24 * u, 36 * u), st.ipucu, new GUIStyle(left) { wordWrap = true, fontSize = Mathf.RoundToInt(12 * u) });
            if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition)) root.rakipKartT = 0;   // dokununca hemen kapanır
            return y + h + 8 * u;
        }
        float OgrKart(Match M, float y)
        {
            var o = M.ogr; var a = M.OgrA(); if (a == null) return y;
            float w = ColW, x = ColX, iw = w - 24 * u;
            string say = "Antrenman " + (o.i + 1) + "/" + o.liste.Count + " · " + new string('●', o.n) + new string('○', Match.OGR_TEKRAR - o.n) + " " + o.n + "/" + Match.OGR_TEKRAR;
            string met = (a.servis == null && !o.dur ? "Top sana gelince oyun duracak. " : "") + a.met;
            var ws = new GUIStyle(left) { wordWrap = true }; var ns = new GUIStyle(ws) { fontSize = Mathf.RoundToInt(11 * u) };
            float h1 = ws.CalcHeight(new GUIContent(met), iw), h2 = ns.CalcHeight(new GUIContent(a.neden), iw);
            string tip; float sp; root.CanliOkuma(out tip, out sp); string canli = null;
            if (tip != null) { bool ok = Match.OgrUygun(a, a.servis != null ? null : tip, a.servis != null ? tip : null, false); canli = (ok ? "✓ " : "✗ ") + "Şu an: " + (Match.OGR_AD.ContainsKey(tip) ? Match.OGR_AD[tip] : tip) + " · hız " + sp.ToString("0.0"); }
            else if (o.son != null) canli = "Çıkan: " + o.son;
            float h = 12 * u + 18 * u + 24 * u + h1 + 6 * u + h2 + (canli != null ? 22 * u : 0) + 10 * u + BH + 10 * u;
            var r = new Rect(x, y, w, h); blocks.Add(r); GUI.Box(r, GUIContent.none, card);
            float yy = y + 10 * u;
            GUI.Label(new Rect(x + 12 * u, yy, iw, 18 * u), say, new GUIStyle(left) { fontSize = Mathf.RoundToInt(11 * u) }); yy += 18 * u;
            GUI.Label(new Rect(x + 12 * u, yy, iw, 24 * u), a.bas, leftB); yy += 24 * u;
            GUI.Label(new Rect(x + 12 * u, yy, iw, h1), met, ws); yy += h1 + 6 * u;
            var nc = GUI.color; GUI.color = new Color(1, 1, 1, .7f); GUI.Label(new Rect(x + 12 * u, yy, iw, h2), a.neden, ns); GUI.color = nc; yy += h2;
            if (canli != null) { GUI.color = canli.StartsWith("✓") || canli.EndsWith("✓") ? Kit.Hex("#d8ff5a") : Kit.Hex("#ffd27a"); GUI.Label(new Rect(x + 12 * u, yy + 2 * u, iw, 20 * u), canli, leftB); GUI.color = Color.white; yy += 22 * u; }
            yy += 10 * u;
            if (Button(new Rect(x + 12 * u, yy, iw, BH), "Antrenmanı geç")) root.OgrGec();
            return y + h + 8 * u;
        }

        void Pause(Match M)
        {
            Dim(.6f); float x = ColX, w = ColW, y = H * .3f;
            Shadowed(new Rect(x, y, w, 40 * u), "Duraklatıldı", big, Color.white); y += 46 * u;
            Shadowed(new Rect(x, y, w, 24 * u), M.sc[0] + " – " + M.sc[1] + " · " + M.target + " sayı · Rakip " + M.RakipAd(false) + (M.stil.kariyer >= 0 ? " · " + Match.KARIYER[M.stil.kariyer].yer : ""), small, Color.white); y += 40 * u;
            if (Button(new Rect(x, y, w, BH * 1.1f), "Maça devam et", true)) root.Devam(); y += BH * 1.1f + 10 * u;
            if (Button(new Rect(x, y, w, BH), "Yeni maç başlat")) root.TekrarOyna(); y += BH + 10 * u;
            if (Button(new Rect(x, y, w, BH), "Ana menü")) root.AnaMenu();
        }

        void Over(Match M)
        {
            Dim(.6f); float x = ColX, w = ColW, y = H * .22f;
            float th = big.CalcHeight(new GUIContent(root.bittiBaslik), w); Shadowed(new Rect(x, y, w, th), root.bittiBaslik, root.bittiBaslik.Length > 24 ? mid : big, Color.white); y += th + 8 * u;
            Shadowed(new Rect(x, y, w, 60 * u), M.sc[0] + " – " + M.sc[1], new GUIStyle(big) { fontSize = Mathf.RoundToInt(48 * u) }, Color.white); y += 64 * u;
            float ih = small.CalcHeight(new GUIContent(root.bittiBilgi), w); Shadowed(new Rect(x, y, w, ih), root.bittiBilgi, small, new Color(1, 1, 1, .8f)); y += ih + 16 * u;
            if (Button(new Rect(x, y, w, BH * 1.15f), root.bittiDugme, true)) root.TekrarOyna(); y += BH * 1.15f + 10 * u;
            if (root.bittiIlk) { if (Button(new Rect(x, y, w, BH), "Tam antrenman")) root.Antrenman(); y += BH + 10 * u; }
            if (Button(new Rect(x, y, w, BH), "Ana menü")) root.AnaMenu();
        }
    }
}
