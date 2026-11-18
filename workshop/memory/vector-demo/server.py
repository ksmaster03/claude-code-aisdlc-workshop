"""MCP server (stdio) for the memory demo. Read-only: two tools, no write tool.

Built on the official MCP Python SDK (FastMCP). Claude Code starts it with:
  claude mcp add --transport stdio vault-search -- uv run --no-project --python-preference only-system --with-requirements <dir>/requirements.txt python <dir>/server.py
"""
from __future__ import annotations

from mcp.server.fastmcp import FastMCP

import search as memory
from search import search, stats

mcp = FastMCP("vault-search")


@mcp.tool()
def search_memory(query: str, n_results: int = 5) -> str:
    """Semantic search over the team memory vault and the OpsTrack docs.

    Finds passages by meaning, not exact words, in Thai or English.
    Returns the closest passages with source file, heading and cosine distance (lower is closer).
    Use it before answering questions about past decisions, gotchas, team terms or OpsTrack rules,
    then open the source file to confirm.
    """
    return search(query, n_results)


@mcp.tool()
def memory_stats() -> str:
    """List how many passages are indexed and from which files."""
    return stats()


if __name__ == "__main__":
    # Open Chroma and the model before the event loop starts. Opening Chroma inside a tool call
    # (inside the running loop) hung on Windows in our test, so load once here.
    memory._load()
    mcp.run()
