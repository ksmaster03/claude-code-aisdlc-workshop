"""Ask the index a question from the terminal (same search the MCP server runs).

Usage:
  python query.py "how fast must we fix a machine that stopped the line?"
  python query.py "..." -k 5
"""
from __future__ import annotations

import argparse
import sys

from search import search


def main() -> None:
    sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser()
    ap.add_argument("question")
    ap.add_argument("-k", type=int, default=3)
    args = ap.parse_args()
    print(search(args.question, args.k))


if __name__ == "__main__":
    main()
