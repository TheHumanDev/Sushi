import sys
import os

sys.path.append(os.path.abspath("./ext"))

from sushi import SushiLexer

def setup(app):
    app.add_lexer("sushi", SushiLexer)
    app.add_lexer("sus", SushiLexer)
    app.add_css_file("css/style.css")

project = "Sushi"
author = "TheHumanDev"
version = release = "unreleased"

exclude_patterns = [
    "_build",
    "Thumbs.db",
    ".DS_Store",
]
extensions = [
    "myst_parser"
]
language = "en"
master_doc = "index"
source_suffix = { ".rst": "restructuredtext", ".txt": "markdown", ".md": "markdown" }

html_theme = "shibuya"
html_theme_options = {
    "accent_color": "pink",
}
html_context = {
    "source_type": "github",
    "source_user": "TheHumanDev",
    "source_repo": "Sushi",
    "source_version": "master",
    "source_docs_path": "/docs/"
}
html_static_path = ["_static"]
