"""Build the demo index: chunk every Markdown note, embed it locally, store it in Chroma.

Usage:
  python index.py                       # vault template + docs
  python index.py --add "G:/My Drive/claude-vault"   # also index another folder (label = folder name)

The index is rebuilt from scratch on every run.
"""
from __future__ import annotations

import argparse
import pathlib
import time

from common import COLLECTION, DATA_DIR, DEFAULT_SOURCES, Embedder, chunk_markdown, iter_markdown, log, open_collection


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--add", action="append", default=[], help="extra folder of Markdown notes")
    ap.add_argument("--only", action="store_true", help="index only the --add folders")
    args = ap.parse_args()

    sources = {} if args.only else dict(DEFAULT_SOURCES)
    for folder in args.add:
        p = pathlib.Path(folder).expanduser().resolve()
        sources[p.name] = p

    t0 = time.time()
    ids, docs, metas = [], [], []
    files = 0
    for label, rel, path in iter_markdown(sources):
        files += 1
        title, meta, chunks = chunk_markdown(path.read_text(encoding="utf-8"))
        for n, (head, passage) in enumerate(chunks):
            ids.append(f"{label}/{rel}#{n}")
            # Title and heading go into the text so a passage still makes sense on its own.
            docs.append(f"{title} / {head}\n{passage}" if head and head != title else f"{title}\n{passage}")
            metas.append({"source": f"{label}/{rel}", "heading": head or title, "type": meta.get("type", "doc")})

    if not docs:
        raise SystemExit("no Markdown files found")
    log(f"{files} files, {len(docs)} passages, embedding ...")
    vectors = Embedder().embed(docs)
    col = open_collection(create=True)
    col.add(ids=ids, documents=docs, metadatas=metas, embeddings=vectors)
    print(f"indexed {col.count()} passages from {files} files into '{COLLECTION}' in {time.time() - t0:.1f}s")
    print(f"data: {DATA_DIR / 'chroma'}")
    for label, root in sources.items():
        print(f"source {label}: {root}")


if __name__ == "__main__":
    main()
