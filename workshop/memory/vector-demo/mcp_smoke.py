"""Start server.py the way Claude Code does (stdio), list its tools and call one.

Usage: python mcp_smoke.py ["question"]
"""
from __future__ import annotations

import asyncio
import pathlib
import sys

from mcp import ClientSession
from mcp.client.stdio import StdioServerParameters, stdio_client

HERE = pathlib.Path(__file__).resolve().parent


async def main(question: str) -> None:
    params = StdioServerParameters(command=sys.executable, args=[str(HERE / "server.py")], cwd=str(HERE))
    async with stdio_client(params) as (read, write):
        async with ClientSession(read, write) as session:
            init = await session.initialize()
            print(f"server: {init.serverInfo.name} {init.serverInfo.version}")
            tools = await session.list_tools()
            print("tools:", ", ".join(t.name for t in tools.tools))
            result = await session.call_tool("search_memory", {"query": question, "n_results": 3})
            print(result.content[0].text)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    asyncio.run(main(sys.argv[1] if len(sys.argv) > 1 else "how fast must we fix a machine that stopped the line?"))
