"""Optional visual QA: python Check-PdfLayout.py input.pdf output.png (pdfplumber/pypdfium2)."""
import sys
import pdfplumber
import pypdfium2

with pdfplumber.open(sys.argv[1]) as pdf:
    for page in pdf.pages:
        assert abs(page.width - 841.89) < 2 and abs(page.height - 595.28) < 2, "Yatay A4 ölçüsü hatalı"
        chars = page.chars
        raw = "".join(c["text"] for c in chars)
        institution_index = raw.find("Kurum:")
        if institution_index >= 0:
            notice_index = raw.index("KONTROL TASLAĞI")
            institution_top = min(c["top"] for c in chars[institution_index:institution_index + 6])
            notice_bottom = max(c["bottom"] for c in chars[notice_index:notice_index + 14])
            assert institution_top > notice_bottom + 5, "Kurum satırı taslak uyarısıyla çakışıyor"
        assert "ÖDEME BELGESİ DEĞİLDİR" in page.extract_text(), "Taslak uyarısı eksik"
doc = pypdfium2.PdfDocument(sys.argv[1])
doc[0].render(scale=1.4).to_pil().save(sys.argv[2])
print("A4 / taslak uyarısı / kurum satırı kontrolü başarılı")
