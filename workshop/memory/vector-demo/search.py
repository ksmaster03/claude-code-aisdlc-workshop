"""The one search function used by both query.py and server.py."""
from __future__ import annotations

from common import Embedder, log, open_collection

_embedder = None
_collection = None


def _load():
    global _embedder, _collection
    if _collection is None:
        log("opening index ...")
        _collection = open_collection()
        log("loading embedding model ...")
        _embedder = Embedder()
        log("ready")
    return _embedder, _collection


def search(question: str, k: int = 5) -> str:
    embedder, col = _load()
    k = max(1, min(int(k), 10))
    res = col.query(
        query_embeddings=embedder.embed([question]),
        n_results=k,
        include=["documents", "metadatas", "distances"],
    )
    lines = [f"query: {question}"]
    for rank, (doc, meta, dist) in enumerate(
        zip(res["documents"][0], res["metadatas"][0], res["distances"][0]), start=1
    ):
        snippet = " ".join(doc.split())[:220]
        lines.append(f"{rank}. {meta['source']} | {meta['heading']} | distance {dist:.3f}")
        lines.append(f"   {snippet}")
    return "\n".join(lines)


def stats() -> str:
    _, col = _load()
    sources = sorted({m["source"] for m in col.get(include=["metadatas"])["metadatas"]})
    return f"{col.count()} passages from {len(sources)} files:\n" + "\n".join(sources)
