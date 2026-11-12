#!/usr/bin/env python3
"""Game Center başarımlarını App Store Connect'e girer (store_listing/game_center/basarimlar.json + <id>.png).

2026-11-11 (mağaza 1.0, yasin "store sürecine başlayalım"): 23 başarım × 2 dil × görsel elle ~45 dk ve tek
kimlik hatası başarımı bozar (oyun Basarim.cs'teki kimlikle gönderir). Kaynak tek: basarimlar.json; YAPMAN_GEREKENLER
tablosu bilgi içindir. Betik yeniden koşulabilir: var olan başarım/dil/görsel atlanır, puan farklıysa güncellenir.
Silmez. Başarımlar canlıya çıkana kadar ASC'de silinebilir; canlıdan sonra yalnız metin/puan değişir.

Ortam: ASC_KEY_ID, ASC_ISSUER_ID, ASC_KEY_CONTENT (fastlane'in kullandığı anahtar, .p8 metni), ASC_APP_ID.
--kuru: yalnız okur ve ne yapacağını yazar.
"""
import argparse
import json
import os
import sys
import time
from pathlib import Path

import jwt  # PyJWT
import requests

API = "https://api.appstoreconnect.apple.com"
KOK = Path(__file__).resolve().parents[2]
VERI = KOK / "store_listing" / "game_center"
DILLER = {"en": "en-US", "tr": "tr"}


class Asc:
    def __init__(self):
        self._tok, self._exp = None, 0
        self.s = requests.Session()

    def _token(self):
        if time.time() > self._exp - 60:
            simdi = int(time.time())
            self._exp = simdi + 15 * 60
            self._tok = jwt.encode(
                {"iss": os.environ["ASC_ISSUER_ID"], "iat": simdi, "exp": self._exp, "aud": "appstoreconnect-v1"},
                os.environ["ASC_KEY_CONTENT"], algorithm="ES256",
                headers={"kid": os.environ["ASC_KEY_ID"], "typ": "JWT"})
        return self._tok

    def istek(self, yontem, yol, govde=None, izin=(200, 201, 204)):
        r = self.s.request(yontem, API + yol, json=govde, headers={"Authorization": "Bearer " + self._token()})
        if r.status_code not in izin:
            # Apple'ın hata gövdesi gizli bilgi taşımaz; neyin eksik olduğunu (rol, alan, kimlik) söyler.
            raise SystemExit(f"{yontem} {yol} -> {r.status_code}\n{r.text[:2000]}")
        return r.json() if r.content else {}

    def hepsi(self, yol):
        veri, sonraki = [], yol
        while sonraki:
            j = self.istek("GET", sonraki)
            veri += j.get("data", [])
            n = j.get("links", {}).get("next")
            sonraki = n[len(API):] if n else None
        return veri


def gc_detay(asc, uyg, kuru):
    j = asc.istek("GET", f"/v1/apps/{uyg}/gameCenterDetail", izin=(200, 404))
    if j.get("data"):
        return j["data"]["id"]
    if kuru:
        print("[kuru] Game Center ayrıntısı yok, açılacak")
        return None
    j = asc.istek("POST", "/v1/gameCenterDetails", {"data": {"type": "gameCenterDetails",
        "relationships": {"app": {"data": {"type": "apps", "id": uyg}}}}})
    print("Game Center açıldı")
    return j["data"]["id"]


def gorsel_yukle(asc, lok_id, dosya):
    b = dosya.read_bytes()
    j = asc.istek("POST", "/v1/gameCenterAchievementImages", {"data": {"type": "gameCenterAchievementImages",
        "attributes": {"fileName": dosya.name, "fileSize": len(b)},
        "relationships": {"gameCenterAchievementLocalization": {"data": {
            "type": "gameCenterAchievementLocalizations", "id": lok_id}}}}})
    gid = j["data"]["id"]
    for op in j["data"]["attributes"].get("uploadOperations") or []:
        bas, uz = op["offset"], op["length"]
        h = {x["name"]: x["value"] for x in op.get("requestHeaders", [])}
        r = requests.request(op["method"], op["url"], data=b[bas:bas + uz], headers=h)
        if r.status_code >= 300:
            raise SystemExit(f"görsel parçası yüklenemedi ({dosya.name}): {r.status_code}")
    asc.istek("PATCH", f"/v1/gameCenterAchievementImages/{gid}", {"data": {"type": "gameCenterAchievementImages",
        "id": gid, "attributes": {"uploaded": True}}})  # 2026-11-12: bu kaynakta sourceFileChecksum yok (409)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--kuru", action="store_true")
    a = ap.parse_args()
    liste = json.loads((VERI / "basarimlar.json").read_text(encoding="utf-8"))
    assert sum(x["puan"] for x in liste) <= 1000, "Apple sınırı 1000 puan"
    for x in liste:
        assert (VERI / f"{x['id']}.png").exists(), x["id"] + ".png yok"

    asc, uyg = Asc(), os.environ["ASC_APP_ID"]
    gcd = gc_detay(asc, uyg, a.kuru)
    var = {}
    if gcd:
        for d in asc.hepsi(f"/v1/gameCenterDetails/{gcd}/gameCenterAchievements?limit=200"):
            var[d["attributes"]["vendorIdentifier"]] = d
    print(f"ASC'de {len(var)} başarım var, listede {len(liste)}")

    for x in liste:
        bid = x["id"]
        if bid in var:
            d = var[bid]
            if d["attributes"].get("points") != x["puan"]:
                print(f"{bid}: puan {d['attributes'].get('points')} -> {x['puan']}")
                if not a.kuru:
                    asc.istek("PATCH", f"/v1/gameCenterAchievements/{d['id']}", {"data": {
                        "type": "gameCenterAchievements", "id": d["id"], "attributes": {"points": x["puan"]}}})
        else:
            print(f"{bid}: yeni ({x['puan']} puan)")
            if a.kuru:
                continue
            # Puan düşürülenler önce güncellendiği için sıra önemli değil; yine de toplam 1000'i aşan ara durum
            # olmasın diye yeni başarımlar var olanların puanı düzeltildikten sonra eklenir (aynı döngü sırası).
            d = asc.istek("POST", "/v1/gameCenterAchievements", {"data": {"type": "gameCenterAchievements",
                "attributes": {"referenceName": x["en"]["ad"], "vendorIdentifier": bid, "points": x["puan"],
                               "showBeforeEarned": True, "repeatable": False},
                "relationships": {"gameCenterDetail": {"data": {"type": "gameCenterDetails", "id": gcd}}}}})["data"]
        if a.kuru and bid not in var:
            continue
        lok = {l["attributes"]["locale"]: l for l in
               asc.hepsi(f"/v1/gameCenterAchievements/{d['id']}/localizations?limit=50")}
        for k, loc in DILLER.items():
            m = x[k]
            if loc in lok:
                l = lok[loc]
            else:
                print(f"  {bid} {loc}: dil ekleniyor")
                if a.kuru:
                    continue
                l = asc.istek("POST", "/v1/gameCenterAchievementLocalizations", {"data": {
                    "type": "gameCenterAchievementLocalizations",
                    "attributes": {"locale": loc, "name": m["ad"], "beforeEarnedDescription": m["aciklama"],
                                   "afterEarnedDescription": m["aciklama"]},
                    "relationships": {"gameCenterAchievement": {"data": {
                        "type": "gameCenterAchievements", "id": d["id"]}}}}})["data"]
            g = asc.istek("GET", f"/v1/gameCenterAchievementLocalizations/{l['id']}/gameCenterAchievementImage",
                          izin=(200, 404))
            gd = g.get("data")
            durum = ((gd or {}).get("attributes", {}).get("assetDeliveryState") or {}).get("state")
            if gd and durum not in ("COMPLETE", "UPLOAD_COMPLETE"):
                # Yarım kalmış yükleme (önceki koşu PATCH'te düştü): kaydı silip baştan yükle.
                print(f"  {bid} {loc}: yarım görsel ({durum}) siliniyor")
                if not a.kuru:
                    asc.istek("DELETE", f"/v1/gameCenterAchievementImages/{gd['id']}")
                gd = None
            if not gd:
                print(f"  {bid} {loc}: görsel yükleniyor")
                if not a.kuru:
                    gorsel_yukle(asc, l["id"], VERI / f"{bid}.png")
    print("Bitti. ASC › Game Center › Achievements listesini kontrol et.")


if __name__ == "__main__":
    sys.exit(main())
