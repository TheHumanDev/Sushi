# sushi.py
import re

from pygments.lexer import RegexLexer, bygroups, include, using, this, default, words
from pygments.token import *

class SushiLexer(RegexLexer):
    name = 'Sushi'
    aliases = ['sushi', 'sus']
    filenames = ['*.sus']

    flags = re.MULTILINE | re.DOTALL

    tokens = {
        'root': []
    }
