# Stüdyo ajanları: kaynak ve uyarlama

yasin 2026-11-05: "Bunu birdie smash için ekleyelim" (github.com/IdoCohen560/claude-unity-game-studio, MIT).
Ajanlar o paketin `game-studios-template/.claude/agents/` klasöründen (özgün: Donchitos/Claude-Code-Game-Studios, MIT)
seçildi; Unity bilgi skill'leri Nice-Wolf-Studio/unity-claude-skills (MIT), oyun hissi ve mobil skill'leri
devdavv/unity-ai-workflow ve tjboudreaux/cc-plugin-unity-gamedev (MIT).

Uyarlama (neden):
- 49 ajandan 11'i alındı. Godot/Unreal, ağ, anlatı, canlı operasyon ve yönetim katmanı (producer, director'lar)
  bu oyunda iş görmüyor; her biri bağlam ve token yer.
- Her ajanın "Collaboration Protocol" bölümü "Birdie Smash çalışma kuralı" ile değiştirildi. Özgün metin her bölüm
  için kullanıcıdan onay istiyor; alt ajan kullanıcıya soramaz ve yasin'in kuralı "onay beklemeden yap" (CLAUDE.md).
- `skills:`, `memory:` ve `Task` aracı çıkarıldı: başvurulan şablon skill'leri burada yok; ajan içinden ajan doğurmak
  maliyeti katlıyor.
- Şablonun hook'ları, settings.json'u, statusline'ı ve CLAUDE.md'si alınmadı: Windows/şablon yollarına bağlı, push ve
  commit'i engelleyen doğrulayıcılar Birdie'nin CI ve ekran turu akışıyla çakışıyor.
- Unity MCP (IvanMurzak) alınmadı: Editor gerektiriyor, yasin'in PC'sinde zaten `mcp-unity` planlı (birdie-unity skill).
