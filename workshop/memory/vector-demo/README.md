# สาธิต vector DB: ค้นความจำตามความหมาย ผ่าน MCP (ผู้สอนรันเอง)

สาธิต 8 นาทีหลังแลบ LM ผู้เรียนไม่ต้องติดตั้งอะไร
ทุกอย่างรันในเครื่องผู้สอน ไม่ใช้ Docker ไม่ส่งเนื้อหาไป API ภายนอก ไม่ต้องใช้สิทธิ์ admin

```
vault-template/*.md + docs/*.md
        │  index.py: ตัดตามหัวข้อ (ชิ้นละไม่เกิน 450 ตัวอักษร) แล้วทำ embedding ในเครื่อง
        ▼
Chroma (PersistentClient) ที่ %LOCALAPPDATA%\opstrack-memory-demo\chroma
        │  server.py: MCP server แบบ stdio อ่านอย่างเดียว เครื่องมือ 2 ตัว
        ▼
Claude Code  →  mcp__vault-search__search_memory , mcp__vault-search__memory_stats
```

| ไฟล์ | ทำอะไร |
|---|---|
| `requirements.txt` | แพ็กเกจที่ตรึงรุ่นไว้ (รุ่นที่ทดสอบจริงบนเครื่องที่เปิด Smart App Control) |
| `common.py` | ที่อยู่ไฟล์ การตัดชิ้น Markdown และตัวทำ embedding (ONNX) |
| `index.py` | สร้างดัชนีใหม่ทั้งหมดทุกครั้ง เพิ่มโฟลเดอร์อื่นด้วย `--add <folder>` |
| `query.py` | ถามจาก terminal ใช้ฟังก์ชันค้นตัวเดียวกับ server |
| `search.py` | ฟังก์ชันค้นที่ใช้ร่วมกัน |
| `server.py` | MCP server บน MCP Python SDK ทางการ (FastMCP) |
| `mcp_smoke.py` | เปิด server แบบเดียวกับ Claude Code แล้วเรียกเครื่องมือ 1 ครั้ง |

โมเดล embedding: `sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2` ไฟล์ `onnx/model_quint8_avx2.onnx` (118 MB, Apache-2.0, รองรับภาษาไทย)
ดาวน์โหลดจาก Hugging Face ครั้งแรกครั้งเดียว แล้วอ่านจากแคช `%USERPROFILE%\.cache\huggingface\hub`

## ทำไมไม่ใช้ MCP server สำเร็จรูป

ลองตัวทางการสองตัวบนเครื่องนี้แล้ว (18 พ.ย. 2569, Windows 11 เปิด Smart App Control) ใช้กับโน้ตภาษาไทยไม่ได้

| ตัวที่ลอง | ผล |
|---|---|
| `chroma-mcp` 0.2.6 ของ Chroma (github.com/chroma-core/chroma-mcp, Apache-2.0, เครื่องมือ 12 ตัว เช่น `chroma_query_documents`) | ติดตั้งและรันได้ แต่ embedding ในเครื่องตัวเดียวที่มีคือ all-MiniLM-L6-v2 ซึ่งเป็นภาษาอังกฤษ ประโยคไทยสองประโยคที่ไม่เกี่ยวกันได้ระยะห่าง `0.0` คือมองเป็นข้อความเดียวกัน ตัวเลือก embedding อื่นของมัน (openai, cohere, jina, voyageai, roboflow) ส่งเนื้อหาออกไป API ภายนอก |
| `mcp-server-qdrant` 0.8.1 ของ Qdrant (github.com/qdrant/mcp-server-qdrant, Apache-2.0, เครื่องมือ `qdrant-store` `qdrant-find`) | รันไม่ได้ Smart App Control บล็อก `py_rust_stemmers` ที่ FastEmbed โหลดตอนเริ่ม: `DLL load failed while importing py_rust_stemmers: An Application Control policy has blocked this file.` |

จึงใช้ส่วนที่ผ่าน Smart App Control บนเครื่องนี้: Chroma (ไลบรารีทางการ) + onnxruntime + tokenizers + MCP Python SDK ทางการ แล้วเขียน server เองราว 40 บรรทัด
ถ้าโน้ตทั้งหมดเป็นภาษาอังกฤษ `uvx chroma-mcp --client-type persistent --data-dir <path>` ใช้แทนได้เลย

## สิ่งที่ต้องมี

- Python 3.12 จาก python.org (มีลายเซ็น) เครื่องนี้: `Python 3.12.10`
  - **ห้ามใช้ Python ที่ uv ดาวน์โหลดเอง** Smart App Control บล็อก: `DLL load failed while importing _ctypes: An Application Control policy has blocked this file.`
  - ทุกคำสั่งจึงใส่ `--python-preference only-system` ให้ uv ใช้ Python ของเครื่อง
  - ไม่มี Python: ติดตั้งจาก python.org แบบไม่ติ๊ก "for all users" ไม่ต้องใช้สิทธิ์ admin
- uv (เครื่องนี้ `uv 0.11.12`) ไม่ต้องติดตั้งแพ็กเกจลงเครื่อง uv สร้าง environment ในแคชของมันเอง
- เน็ตครั้งแรก: pypi.org, files.pythonhosted.org, huggingface.co หลังจากนั้นรันแบบ `--offline` ได้
- Claude Code 2.1.x (ทดสอบกับ 2.1.310 และตัวที่มากับส่วนขยาย VS Code 2.1.327)

## เตรียมก่อนวันสอน (ทำวันก่อน หรือตอนพักกลางวัน)

คำสั่งทั้งหมดรันจากโฟลเดอร์นี้ (`workshop/memory/vector-demo`) ใช้ได้ทั้ง PowerShell และ Git Bash

### 1. สร้างดัชนี

```
uv run --no-project --python-preference only-system --python 3.12 --with-requirements requirements.txt python index.py
```

ผลจริงครั้งแรก (รวมดาวน์โหลดโมเดล):

```
9 files, 50 passages, embedding ...
downloading sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2/tokenizer.json (first run only)
downloading sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2/onnx/model_quint8_avx2.onnx (first run only)
indexed 50 passages from 9 files into 'opstrack-memory' in 40.2s
data: C:\Users\TINV-026\AppData\Local\opstrack-memory-demo\chroma
source vault: C:\work\opstrack\workshop\memory\vault-template
source opstrack-docs: C:\work\opstrack\docs
```

ครั้งต่อไปใช้ 14.0 วินาที คำเตือนเรื่อง symlink ของ huggingface_hub บน Windows ไม่มีผล ข้ามได้
server โหลดดัชนีครั้งเดียวตอนเริ่ม ถ้าสร้างดัชนีใหม่ระหว่างที่ Claude Code เปิด vault-search อยู่ ให้เปิดแชตใหม่หลังสร้างเสร็จ
ถ้าไม่มีโฟลเดอร์ `docs/` ของ OpsTrack ใช้แค่ vault ก็ได้ (`skip opstrack-docs` จะขึ้นใน log)

### 2. ลองถามจาก terminal

```
uv run --offline --no-project --python-preference only-system --python 3.12 --with-requirements requirements.txt python query.py "how fast must we fix a machine that stopped the line?"
```

ผลจริง:

```
query: how fast must we fix a machine that stopped the line?
1. opstrack-docs/sla-policy.md | เวลาที่ต้องปิดงาน | distance 0.461
   นโยบาย SLA งานซ่อม / เวลาที่ต้องปิดงาน | ระดับ | ความหมาย | ต้องปิดงานภายใน | ... | P1 | เครื่องหยุด ไลน์ผลิตไม่ได้ | 4 ชั่วโมง | ...
2. vault/glossary/sla.md | แปลว่า | distance 0.508
3. vault/decisions/2026-11-19-close-conditional-update.md | เหตุผล | distance 0.536
```

คำถามภาษาไทยที่ใช้คำต่างจากเอกสาร:

```
query: เครื่องพังจนไลน์หยุด ต้องซ่อมให้เสร็จภายในกี่ชั่วโมง
1. vault/glossary/sla.md | แปลว่า | distance 0.324
2. opstrack-docs/sla-policy.md | เวลาที่ต้องปิดงาน | distance 0.364
3. vault/decisions/2026-11-19-close-conditional-update.md | ปัญหา | distance 0.419

query: สองคนกดปิดงานเดียวกันพร้อมกัน กันยังไง
1. vault/decisions/2026-11-19-close-conditional-update.md | ปัญหา | distance 0.437
2. vault/decisions/2026-11-19-close-conditional-update.md | เหตุผล | distance 0.551
3. vault/decisions/2026-11-19-close-conditional-update.md | ทางที่ไม่เลือก | distance 0.600
```

ตัวอย่างที่ **หาไม่เจอ** (ใช้สอนว่าต้องเปิดไฟล์ยืนยันเสมอ): คำตอบที่ถูกคือ `IClock` ใน `docs/standards/coding-standard.md` (CS-3) แต่ 3 อันดับแรกไม่มี

```
query: อยากรู้เวลาปัจจุบันในชั้น business ต้องใช้อะไร
1. vault/glossary/sla.md | อย่าสับสนกับ | distance 0.589
2. opstrack-docs/sla-policy.md | วิธีนับ | distance 0.631
3. opstrack-docs/sla-policy.md | เวลาที่ต้องปิดงาน | distance 0.656
```

### 3. ทดสอบ MCP server แบบเดียวกับที่ Claude Code เปิด

```
uv run --offline --no-project --python-preference only-system --python 3.12 --with-requirements requirements.txt python mcp_smoke.py "สองคนกดปิดงานเดียวกันพร้อมกัน กันยังไง"
```

ผลจริง (13.5 วินาที รวมเปิด server):

```
server: vault-search 1.30.0
tools: search_memory, memory_stats
query: สองคนกดปิดงานเดียวกันพร้อมกัน กันยังไง
1. vault/decisions/2026-11-19-close-conditional-update.md | ปัญหา | distance 0.437
...
```

### 4. ต่อกับ Claude Code ใน repo OpsTrack ที่จะใช้สาธิต

ใช้ scope local (ค่าเริ่มต้น) เพราะเป็นของเครื่องผู้สอนคนเดียว และไม่ต้องรออนุมัติแบบ scope project

Git Bash:

```
cd <OpsTrack repo>
claude mcp add --transport stdio vault-search -- uv run --offline --no-project --python-preference only-system --python 3.12 --with-requirements C:/work/opstrack/workshop/memory/vector-demo/requirements.txt python C:/work/opstrack/workshop/memory/vector-demo/server.py
```

PowerShell: **ต้องใส่ `'--'` ในเครื่องหมายคำพูด** ถ้า `claude` มาจาก npm (`claude.ps1`) PowerShell กลืน `--` เปล่า ๆ แล้ว claude ฟ้อง `error: unknown option '--offline'`
(ทดสอบแล้ว: `claude.exe` ตัว native ส่ง `--` ผ่านได้ปกติ ดูว่าเป็นตัวไหนด้วย `(Get-Command claude).Source`)

```
claude mcp add --transport stdio vault-search '--' uv run --offline --no-project --python-preference only-system --python 3.12 --with-requirements C:/work/opstrack/workshop/memory/vector-demo/requirements.txt python C:/work/opstrack/workshop/memory/vector-demo/server.py
```

ตรวจ: `claude mcp list` ต้องเห็นบรรทัด `vault-search: uv run --offline ... server.py - ✔ Connected` (ผลจริงจากเครื่องนี้ ถ้าเพิ่มแบบ `--scope project` จะขึ้น `Pending approval (run claude to approve)` ต้องเปิด claude อนุมัติก่อน)

อนุญาตเครื่องมือค้นไว้ล่วงหน้าใน `.claude/settings.local.json` ของ repo สาธิต จะได้ไม่ต้องกดอนุมัติกลางสาธิต (เป็นเครื่องมืออ่านอย่างเดียว):

```json
{ "permissions": { "allow": ["mcp__vault-search__search_memory", "mcp__vault-search__memory_stats"] } }
```

ผลจริงจาก `claude -p` (Haiku, ใน repo ทดลอง):

```
claude -p --model haiku --mcp-config .mcp.json --strict-mcp-config --allowedTools "mcp__vault-search__search_memory" "Use the vault-search MCP tool search_memory to answer: how fast must we fix a machine that stopped the production line? Answer in one short sentence and name the source file."

เครื่องที่หยุดไลน์ผลิต (ระดับ P1) ต้องปิดงานซ่อมภายใน 4 ชั่วโมงนับจากเวลาที่แจ้งงาน (source: `opstrack-docs/sla-policy.md`)
```

(3 turns, ไม่มีการปฏิเสธสิทธิ์, ค่าใช้จ่าย 0.013 USD)

## สคริปต์สาธิต 8 นาที

| นาที | ทำ | พูด |
|---|---|---|
| 0-1 | เปิดแชตใหม่ใน VS Code ถาม "ใช้ memory_stats บอกว่าดัชนีมีอะไรบ้าง" | ดัชนีนี้คือ vault ที่ทุกคนเพิ่งทำ กับเอกสาร OpsTrack 9 ไฟล์ ตัดเป็น 50 ชิ้น |
| 1-3 | ถาม "ค้นในความจำ: เครื่องพังจนไลน์หยุด ต้องซ่อมให้เสร็จภายในกี่ชั่วโมง" | คำในคำถามไม่ตรงกับเอกสาร ("เครื่องหยุด ไลน์ผลิตไม่ได้") แต่ค้นเจอเพราะค้นตามความหมาย |
| 3-4 | ถามภาษาอังกฤษ "how fast must we fix a machine that stopped the line?" | ถามอังกฤษ เจอเอกสารไทย คำค้นแบบตรงตัวทำไม่ได้ |
| 4-5 | ถาม "อยากรู้เวลาปัจจุบันในชั้น business ต้องใช้อะไร" แล้วให้ Claude ใช้ Grep หา `IClock` | ค้นตามความหมายพลาดได้ ใช้คู่กับค้นคำ และเปิดไฟล์ต้นทางยืนยันทุกครั้ง |
| 5-7 | เปิดสไลด์ M9 RAG กลาง ชี้ว่าสาธิตนี้ทำ INGEST CHUNK INDEX RETRIEVE แล้ว | ที่ยังขาด: กรองสิทธิ์รายผู้ใช้ ชุดวัดความถูก ติดตามต้นทุน |
| 7-8 | ถามห้อง | ใช้เมื่อโน้ตหลายร้อย หรือเอกสารหลายระบบ ถ้าโน้ตไม่กี่สิบ Markdown กับค้นคำพอแล้ว |

## ถ้าสาธิตพัง

| อาการ | ทำ |
|---|---|
| `An Application Control policy has blocked this file` | ดูว่ารันด้วย Python ของ python.org (`--python-preference only-system`) ถ้ายังโดนกับไฟล์อื่น ห้ามปิด Smart App Control ให้ข้ามไปใช้ผลจริงในไฟล์นี้แทน |
| เน็ตบริษัทบล็อก pypi.org หรือ huggingface.co | ต้องเตรียมก่อนวันสอนบนเน็ตที่เข้าได้ หลังเตรียมแล้วใช้ `--offline` ทุกคำสั่ง ไม่ต้องใช้เน็ต |
| Claude ไม่เห็นเครื่องมือ `vault-search` | `claude mcp list` ดูสถานะ รัน `mcp_smoke.py` ถ้า smoke ผ่านแต่ Claude ไม่ผ่าน ให้เปิดแชตใหม่ ถ้า server เปิดช้าเกิน ตั้ง `MCP_TIMEOUT=60000` ก่อนเปิด claude |
| tool call ค้างไม่ตอบ | server ต้องเปิด Chroma ก่อน event loop (ทำไว้แล้วใน `server.py`) ถ้าแก้โค้ดเอง อย่าย้ายการเปิด Chroma เข้าไปในเครื่องมือ ทดสอบแล้วค้างบน Windows |
| ตัวไทยใน terminal เพี้ยน | ใช้ Windows Terminal หรือ Git Bash, `query.py` ตั้ง stdout เป็น UTF-8 ให้แล้ว |
| ทุกอย่างพัง | เปิดผลจริงในไฟล์นี้ขึ้นจอ เล่าตามสคริปต์ แล้วไปต่อ อย่าแก้หน้าห้อง |

## เก็บกวาดหลังสอน

```
claude mcp remove vault-search
```

แล้วลบโฟลเดอร์ `%LOCALAPPDATA%\opstrack-memory-demo` (ดัชนี) ถ้าไม่ใช้แล้ว แคชของ uv ล้างด้วย `uv cache clean` (ล้างทั้งแคช)
