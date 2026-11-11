"""TestFlight çökme raporlarını App Store Connect API'den indirir (2026-11-11, yasin: "TF'de maç içinde crash oldu").

Neden: bulutta cihaz ve Xcode Organizer yok; testçinin "çökmeyi paylaş" dediği raporlar ASC'de
betaFeedbackCrashSubmissions olarak durur. Depo herkese açık: e-posta, ad ve testçi kimliği YAZILMAZ;
yalnız tarih, build, cihaz, iOS, yorum ve çökme metni.
Kullanım: ASC_KEY_ID, ASC_ISSUER_ID, ASC_KEY_CONTENT (p8 düz metin) ortamda; python tf_cokme.py <cikti_klasoru>
"""
import json, os, sys, time, urllib.request, urllib.error
import jwt  # PyJWT

APP_ID = "6824684283"
API = "https://api.appstoreconnect.apple.com"


def token():
    now = int(time.time())
    return jwt.encode({"iss": os.environ["ASC_ISSUER_ID"], "iat": now, "exp": now + 1100, "aud": "appstoreconnect-v1"},
                      os.environ["ASC_KEY_CONTENT"], algorithm="ES256", headers={"kid": os.environ["ASC_KEY_ID"], "typ": "JWT"})


def get(path, tok):
    req = urllib.request.Request(API + path, headers={"Authorization": "Bearer " + tok})
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            return json.load(r)
    except urllib.error.HTTPError as e:
        print("HTTP", e.code, path, e.read()[:300].decode("utf-8", "replace"))
        return None


def main(out):
    os.makedirs(out, exist_ok=True)
    tok = token()
    lst = get(f"/v1/apps/{APP_ID}/betaFeedbackCrashSubmissions?limit=15&sort=-createdDate&include=build&fields[builds]=version", tok)
    if lst is None:
        return 1
    builds = {b["id"]: b["attributes"].get("version") for b in lst.get("included", []) if b["type"] == "builds"}
    ozet = []
    for i, s in enumerate(lst.get("data", [])):
        a = s["attributes"]; rel = s.get("relationships", {}).get("build", {}).get("data") or {}
        bilgi = {k: a.get(k) for k in ("createdDate", "comment", "deviceModel", "osVersion", "appUptimeInMilliseconds", "batteryPercentage", "deviceFamily", "architecture", "connectionType")}
        bilgi["build"] = builds.get(rel.get("id"))
        log = get(f"/v1/betaFeedbackCrashSubmissions/{s['id']}/crashLog", tok)
        metin = (log or {}).get("data", {}).get("attributes", {}).get("logText") or ""
        ad = f"{i:02d}_{(a.get('createdDate') or 'x')[:19].replace(':', '')}_b{bilgi['build']}.txt"
        with open(os.path.join(out, ad), "w") as f:
            f.write(json.dumps(bilgi, ensure_ascii=False, indent=1) + "\n\n" + metin)
        ozet.append(ad + "  " + json.dumps(bilgi, ensure_ascii=False))
    with open(os.path.join(out, "ozet.txt"), "w") as f:
        f.write("\n".join(ozet) or "rapor yok")
    print("\n".join(ozet) or "rapor yok")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
