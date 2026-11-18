"""Shared parts of the memory demo: paths, Markdown chunking, local multilingual embeddings.

Everything runs on the instructor's laptop. Nothing is sent to an external embedding API:
the model (ONNX, Apache-2.0) is downloaded once from Hugging Face and then read from the local cache.
"""
from __future__ import annotations

import os
import pathlib
import re
import sys

HERE = pathlib.Path(__file__).resolve().parent
LAB = HERE.parent.parent

# Default sources: the vault template and the OpsTrack docs from the lab repo.
# Two layouts: course repo (workshop/memory/vector-demo + docs)
# and the public workshop repo (workshop/memory/vector-demo + docs/ at the repo root).
_DOCS_CANDIDATES = [LAB / "opstrack" / "docs", LAB.parent / "docs"]
DEFAULT_SOURCES = {
    "vault": HERE.parent / "vault-template",
    "opstrack-docs": next((p for p in _DOCS_CANDIDATES if p.is_dir()), _DOCS_CANDIDATES[0]),
}
SKIP_DIRS = {"templates", "setup", ".obsidian", ".trash", ".git"}
SKIP_FILES = {"README.md"}

# Index data lives outside the repo so git status stays clean.
DATA_DIR = pathlib.Path(
    os.environ.get("MEMORY_DEMO_DATA")
    or pathlib.Path(os.environ.get("LOCALAPPDATA", pathlib.Path.home())) / "opstrack-memory-demo"
)
COLLECTION = "opstrack-memory"

# Multilingual model (50+ languages, Thai included), int8 ONNX build for AVX2 CPUs, about 118 MB.
MODEL_REPO = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2"
MODEL_FILE = os.environ.get("MEMORY_DEMO_MODEL_FILE", "onnx/model_quint8_avx2.onnx")
MAX_TOKENS = 128  # max_seq_length of this model


def log(msg: str) -> None:
    """Log to stderr. stdout belongs to the MCP protocol when running as a server."""
    print(msg, file=sys.stderr, flush=True)


def _hf_file(filename: str) -> str:
    from huggingface_hub import hf_hub_download

    try:
        return hf_hub_download(MODEL_REPO, filename, local_files_only=True)
    except Exception:
        log(f"downloading {MODEL_REPO}/{filename} (first run only)")
        return hf_hub_download(MODEL_REPO, filename)


class Embedder:
    """Mean-pooled, L2-normalised sentence embeddings from the ONNX model."""

    def __init__(self) -> None:
        import onnxruntime as ort
        from tokenizers import Tokenizer

        self.tok = Tokenizer.from_file(_hf_file("tokenizer.json"))
        self.tok.enable_truncation(max_length=MAX_TOKENS)
        pad_id = self.tok.token_to_id("<pad>")
        self.tok.enable_padding(pad_id=pad_id if pad_id is not None else 1, pad_token="<pad>")
        self.sess = ort.InferenceSession(_hf_file(MODEL_FILE), providers=["CPUExecutionProvider"])
        self.inputs = {i.name for i in self.sess.get_inputs()}

    def embed(self, texts: list[str], batch: int = 16) -> list[list[float]]:
        import numpy as np

        out: list[list[float]] = []
        for start in range(0, len(texts), batch):
            enc = self.tok.encode_batch(texts[start : start + batch])
            ids = np.array([e.ids for e in enc], dtype=np.int64)
            mask = np.array([e.attention_mask for e in enc], dtype=np.int64)
            feeds = {"input_ids": ids, "attention_mask": mask}
            if "token_type_ids" in self.inputs:
                feeds["token_type_ids"] = np.zeros_like(ids)
            hidden = self.sess.run(None, feeds)[0]
            m = mask[..., None].astype(np.float32)
            vec = (hidden * m).sum(axis=1) / np.clip(m.sum(axis=1), 1e-9, None)
            vec = vec / np.clip(np.linalg.norm(vec, axis=1, keepdims=True), 1e-9, None)
            out.extend(vec.tolist())
        return out


def open_collection(create: bool = False):
    import chromadb
    from chromadb.config import Settings

    DATA_DIR.mkdir(parents=True, exist_ok=True)
    client = chromadb.PersistentClient(
        path=str(DATA_DIR / "chroma"), settings=Settings(anonymized_telemetry=False)
    )
    if create:
        try:
            client.delete_collection(COLLECTION)
        except Exception:
            pass
        # We pass our own vectors, so the collection has no embedding function of its own.
        return client.create_collection(
            COLLECTION, embedding_function=None, metadata={"hnsw:space": "cosine"}
        )
    return client.get_collection(COLLECTION, embedding_function=None)


def split_frontmatter(text: str) -> tuple[dict, str]:
    meta: dict = {}
    if text.startswith("---"):
        end = text.find("\n---", 3)
        if end != -1:
            for line in text[3:end].splitlines():
                if ":" in line:
                    key, value = line.split(":", 1)
                    meta[key.strip()] = value.strip()
            text = text[end + 4 :]
    return meta, text


def chunk_markdown(text: str, max_chars: int = 450) -> tuple[str, dict, list[tuple[str, str]]]:
    """Split a note into (heading, passage) pieces. Long sections are cut at line breaks."""
    meta, body = split_frontmatter(text)
    title = ""
    sections: list[tuple[str, list[str]]] = [("", [])]
    for line in body.splitlines():
        m = re.match(r"^(#{1,4})\s+(.*)", line)
        if m:
            if not title and m.group(1) == "#":
                title = m.group(2).strip()
            sections.append((m.group(2).strip(), []))
        else:
            sections[-1][1].append(line)
    chunks: list[tuple[str, str]] = []
    for head, lines in sections:
        piece = ""
        for line in lines:
            if piece and len(piece) + len(line) + 1 > max_chars:
                chunks.append((head, piece.strip()))
                piece = ""
            piece += line + "\n"
        if piece.strip():
            chunks.append((head, piece.strip()))
    return title, meta, chunks


def iter_markdown(sources: dict[str, pathlib.Path]):
    for label, root in sources.items():
        if not root.is_dir():
            log(f"skip {label}: folder not found {root}")
            continue
        for path in sorted(root.rglob("*.md")):
            rel = path.relative_to(root)
            if set(rel.parts[:-1]) & SKIP_DIRS or (len(rel.parts) == 1 and rel.name in SKIP_FILES):
                continue
            yield label, rel.as_posix(), path
