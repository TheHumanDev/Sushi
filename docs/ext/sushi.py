# sushi.py
import re

from pygments.lexer import RegexLexer, bygroups, include, using, this, default, words
from pygments.token import * # type: ignore

class SushiLexer(RegexLexer):
    name = 'Sushi'
    aliases = ['sushi', 'sus']
    filenames = ['*.sus']

    flags = re.MULTILINE | re.DOTALL

    tokens = {
        'root': [
            (r"//[^\r\n]*", Comment.Single),
            (r'/\*', Token.Comment.Multiline, 'comment'),
            (r"[^/]+", Text),
            (r"/", Text)
        ],
        'comment': [
            (r'[^*/]+', Token.Comment.Multiline),
            (r'/\*', Token.Comment.Multiline, '#push'),
            (r'\*/', Token.Comment.Multiline, '#pop'),
            (r'[*/]', Token.Comment.Multiline),
        ]
    }
