import sys
import os

sys.path.append(os.path.abspath('./ext'))

from sushi import SushiLexer

def setup(app):
    from sphinx.highlighting import lexers
    lexers['sushi'] = SushiLexer(startinline=True)
    app.add_css_file('css/style.css')

project = 'Sushi'
author = 'TheHumanDev'
version = release = '0.1'

exclude_patterns = [
    '_build',
    'Thumbs.db',
    '.DS_Store',
]
extensions = [
    'myst_parser'
]
language = 'en'
master_doc = 'index'
source_suffix = { '.rst': 'restructuredtext', '.txt': 'markdown', '.md': 'markdown' }
templates_path = ['_templates']

html_theme = 'shibuya'
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
html_static_path = ['_static']
