"""Writes a fitted model into the player as Heuristic/BidModelWeights.cs (BidModel.Default); see README.md.

usage: embed_model.py MODEL.txt "where it came from"
"""
import os
import sys

TARGET = os.path.join(os.path.dirname(__file__), '..', '..', '..', 'src', 'AI', 'Belot.AI.ClaudePlayer', 'Heuristic', 'BidModelWeights.cs')


def render(model_text, provenance):
    lines = []
    for line in model_text.strip().splitlines():
        parts = line.split()
        if parts[0] in ('gate', 'cell'):
            lines.append(' '.join(parts))
            continue
        numbers = []
        for x in parts[2:]:
            v = round(float(x), 3)
            numbers.append('0' if v == 0 else f'{v:g}')
        lines.append(' '.join(parts[:2] + numbers))
    body = '\n'.join(lines)
    return f'''namespace Belot.AI.ClaudePlayer.Heuristic
{{
    /// <summary>
    /// The learned bidding's weights (see <see cref="BidModel"/> for the format and
    /// HEURISTIC_PLAYER.md for how they were fitted). {provenance}
    /// </summary>
    internal static class BidModelWeights
    {{
        public const string Text = @"
{body}
";
    }}
}}
'''


def main():
    text = render(open(sys.argv[1]).read(), sys.argv[2])
    with open(TARGET, 'w', encoding='utf-8-sig', newline='') as handle:
        handle.write(text.replace('\n', '\r\n'))
    print('wrote', os.path.normpath(TARGET))


if __name__ == '__main__':
    main()
