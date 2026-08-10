# -*- coding: utf-8 -*-
"""제출 PDF 3종 빌드 — 원본 마크다운을 공용 CSS 를 거쳐 PDF 로 변환한다.

    python docs/submission/build.py          # HTML + PDF
    python docs/submission/build.py --html   # HTML 만 (눈으로 확인용)

원본을 여기서 고치지 않는다. 내용이 틀렸으면 원본을 고치고 다시 돌린다.
"""
import io
import os
import re
import subprocess
import sys

import markdown

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.dirname(os.path.abspath(__file__))
FONTS = os.path.join(ROOT, "Treeline", "Assets", "Game", "UI", "Fonts")

CHROME = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

# (원본, 출력 이름, <title>)
DOCS = [
    ("docs/game-design/overview.md", "Treeline-게임소개", "Treeline — 게임 소개 및 설명"),
    ("docs/ai/submission.md", "Treeline-AI활용기술", "Treeline — AI 활용 기술 문서"),
    ("docs/roles.md", "Treeline-팀원롤기술서", "Treeline — 팀원 롤 기술서"),
]

TEMPLATE = """<!DOCTYPE html>
<html lang="ko">
<head>
<meta charset="utf-8">
<title>{title}</title>
<style>{css}</style>
</head>
<body>
{body}
</body>
</html>
"""


def preprocess(text):
    # python-markdown 이 모르는 GFM 취소선을 <del> 로 바꾼다.
    return re.sub(r"~~(.+?)~~", r"<del>\1</del>", text, flags=re.S)


def file_uri(path):
    return "file:///" + path.replace("\\", "/")


def build_html(src, name, title, css):
    text = io.open(os.path.join(ROOT, src), encoding="utf-8").read()
    body = markdown.markdown(preprocess(text), extensions=["tables", "fenced_code"])
    html_path = os.path.join(OUT, name + ".html")
    io.open(html_path, "w", encoding="utf-8").write(
        TEMPLATE.format(title=title, css=css, body=body)
    )
    return html_path


def print_pdf(html_path, name):
    exe = CHROME if os.path.exists(CHROME) else EDGE
    pdf_path = os.path.join(OUT, name + ".pdf")
    subprocess.run(
        [
            exe,
            "--headless=new",
            "--disable-gpu",
            "--no-pdf-header-footer",
            "--print-to-pdf=" + pdf_path,
            file_uri(html_path),
        ],
        check=True,
        capture_output=True,
        timeout=120,
    )
    return pdf_path


def main():
    css = io.open(os.path.join(OUT, "style.css"), encoding="utf-8").read()
    css = css.replace(
        "__FONT_REGULAR__", file_uri(os.path.join(FONTS, "IBMPlexSansKR-Regular.ttf"))
    ).replace(
        "__FONT_SEMIBOLD__", file_uri(os.path.join(FONTS, "IBMPlexSansKR-SemiBold.ttf"))
    )

    html_only = "--html" in sys.argv
    for src, name, title in DOCS:
        html_path = build_html(src, name, title, css)
        print("HTML:", os.path.basename(html_path))
        if not html_only:
            pdf_path = print_pdf(html_path, name)
            size = os.path.getsize(pdf_path) // 1024
            print("PDF :", os.path.basename(pdf_path), f"({size} KB)")


if __name__ == "__main__":
    main()
