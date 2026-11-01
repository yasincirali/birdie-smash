# Üçüncü taraf içerikler / Third-party content

Birdie Smash oyuncu figürleri şu açık lisanslı içeriklerden üretilir. Sprite'lar bu modellerden render edilmiştir.

- **Oyuncak 3B**: MakeHuman veri dosyaları (taban ağ, hedefler, iskelet ağırlıkları; makehumancommunity.org), CC0 1.0, atıf gerekmez. Kıyafet, yüz, saç, ayakkabı ve bantlar Birdie Smash'in kendi geometrisidir (2026-10-26). 2026-10-27 oyuncak oranı: yüz aynı paketteki 'child' yaş hedefleriyle karışır (yine MakeHuman CC0); oran dönüşümü kendi kodumuz, yeni kaynak yok. Önceki sürüm KayKit Character Pack: Adventurers 1.0 (Kay Lousberg, CC0 1.0) arşivde, oyunda kullanılmıyor.
- **Gerçekçi (Rocketbox)**: Microsoft Rocketbox Avatar Library, Sports_Male_02 (oyun; kıyafet dokusu Birdie Smash'in badminton boyaması, 2026-10-26) ve Sports_Male_04 (HTML sprite). MIT License, aşağıdaki metin.
  2026-10-27 aksesuarları (`tool/karakter/rocketbox_aksesuar.py`, `rocketbox_kit.py`): şort ve beş ayakkabı modeli aynı paketin ağından türetilmiş parçalar (MIT, aynı metin); kafa bandı, bileklik, bilekli ayakkabı yakalığı ve forma kumaş desenleri (`rocketbox_forma_*.jpg`) Birdie Smash'in kendi bpy/numpy üretimi. Yeni üçüncü taraf kaynak yok.
- **Dolap şapka ve pelerinleri (2026-11-01)**: KayKit Character Pack: Adventurers 1.0 ve KayKit Character Pack: Skeletons 1.0 (Kay Lousberg, www.kaylousberg.com), CC0 1.0, atıf gerekmez (gönüllü teşekkür: Kay Lousberg). Kaynak: github.com/KayKit-Game-Assets (resmi, bedava sürümler). Kullanılan parçalar: Mage_Hat, Mage_Cape, Knight_Helmet, Knight_Cape, Barbarian_Hat, Barbarian_Cape, Rogue_Cape, Skeleton_Mage_Hat, Skeleton_Rogue_Hood, Skeleton_Rogue_Cape, Skeleton_Warrior_Helmet, Skeleton_Warrior_Cloak; `tool/karakter/kaykit_aksesuar.py` ile ağ olarak dönüştürüldü, bazılarının rengi değiştirildi.
- Eski sportif figür: MakeHuman veri dosyaları, CC0 1.0.
- **Yazı tipleri (2026-10-29, Tüycükler menü dili)**: Baloo 2 (The Baloo 2 Project Authors, EkType; github.com/EkType/Baloo2) ve Nunito (The Nunito Project Authors; github.com/googlefonts/nunito), Google Fonts. Kullanılan dosyalar: Baloo 2 ExtraBold (yalnız Latin karakterlere indirilmiş, satır yüksekliği daraltılmış), Nunito SemiBold, Bold, ExtraBold (değişken yazı tipinden sabit ağırlık örnekleri). SIL Open Font License 1.1, aşağıdaki metin; ayrılmış yazı tipi adı yok. Uygulamanın içinde gömülü dağıtılır.

## Microsoft Rocketbox: MIT License

Copyright (c) 2020 Microsoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## Baloo 2, Nunito: SIL Open Font License 1.1

Copyright 2019 The Baloo 2 Project Authors (https://github.com/EkType/Baloo2)
Copyright 2014 The Nunito Project Authors (https://github.com/googlefonts/nunito)

This Font Software is licensed under the SIL Open Font License, Version 1.1.
This license is copied below, and is also available with a FAQ at:
http://scripts.sil.org/OFL


-----------------------------------------------------------
SIL OPEN FONT LICENSE Version 1.1 - 26 February 2007
-----------------------------------------------------------

PREAMBLE
The goals of the Open Font License (OFL) are to stimulate worldwide
development of collaborative font projects, to support the font creation
efforts of academic and linguistic communities, and to provide a free and
open framework in which fonts may be shared and improved in partnership
with others.

The OFL allows the licensed fonts to be used, studied, modified and
redistributed freely as long as they are not sold by themselves. The
fonts, including any derivative works, can be bundled, embedded, 
redistributed and/or sold with any software provided that any reserved
names are not used by derivative works. The fonts and derivatives,
however, cannot be released under any other type of license. The
requirement for fonts to remain under this license does not apply
to any document created using the fonts or their derivatives.

DEFINITIONS
"Font Software" refers to the set of files released by the Copyright
Holder(s) under this license and clearly marked as such. This may
include source files, build scripts and documentation.

"Reserved Font Name" refers to any names specified as such after the
copyright statement(s).

"Original Version" refers to the collection of Font Software components as
distributed by the Copyright Holder(s).

"Modified Version" refers to any derivative made by adding to, deleting,
or substituting -- in part or in whole -- any of the components of the
Original Version, by changing formats or by porting the Font Software to a
new environment.

"Author" refers to any designer, engineer, programmer, technical
writer or other person who contributed to the Font Software.

PERMISSION & CONDITIONS
Permission is hereby granted, free of charge, to any person obtaining
a copy of the Font Software, to use, study, copy, merge, embed, modify,
redistribute, and sell modified and unmodified copies of the Font
Software, subject to the following conditions:

1) Neither the Font Software nor any of its individual components,
in Original or Modified Versions, may be sold by itself.

2) Original or Modified Versions of the Font Software may be bundled,
redistributed and/or sold with any software, provided that each copy
contains the above copyright notice and this license. These can be
included either as stand-alone text files, human-readable headers or
in the appropriate machine-readable metadata fields within text or
binary files as long as those fields can be easily viewed by the user.

3) No Modified Version of the Font Software may use the Reserved Font
Name(s) unless explicit written permission is granted by the corresponding
Copyright Holder. This restriction only applies to the primary font name as
presented to the users.

4) The name(s) of the Copyright Holder(s) or the Author(s) of the Font
Software shall not be used to promote, endorse or advertise any
Modified Version, except to acknowledge the contribution(s) of the
Copyright Holder(s) and the Author(s) or with their explicit written
permission.

5) The Font Software, modified or unmodified, in part or in whole,
must be distributed entirely under this license, and must not be
distributed under any other license. The requirement for fonts to
remain under this license does not apply to any document created
using the Font Software.

TERMINATION
This license becomes null and void if any of the above conditions are
not met.

DISCLAIMER
THE FONT SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO ANY WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT
OF COPYRIGHT, PATENT, TRADEMARK, OR OTHER RIGHT. IN NO EVENT SHALL THE
COPYRIGHT HOLDER BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
INCLUDING ANY GENERAL, SPECIAL, INDIRECT, INCIDENTAL, OR CONSEQUENTIAL
DAMAGES, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF THE USE OR INABILITY TO USE THE FONT SOFTWARE OR FROM
OTHER DEALINGS IN THE FONT SOFTWARE.
