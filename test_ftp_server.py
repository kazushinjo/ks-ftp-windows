import os, sys
from pyftpdlib.handlers import FTPHandler
from pyftpdlib.servers import FTPServer
from pyftpdlib.authorizers import DummyAuthorizer

ROOT = r"C:\claude\ks-ftp-windows\ftproot"
os.makedirs(ROOT, exist_ok=True)

# ASCII test files
with open(os.path.join(ROOT, "hello.txt"), "w") as f:
    f.write("Hello from FTP server!\nThis is a test file.\n")
with open(os.path.join(ROOT, "sample.csv"), "w") as f:
    f.write("id,name,value\n1,foo,100\n2,bar,200\n")
os.makedirs(os.path.join(ROOT, "subdir"), exist_ok=True)
with open(os.path.join(ROOT, "subdir", "nested.txt"), "w") as f:
    f.write("Nested file content\n")

# Shift-JIS encoded filename test files
# Write filename bytes directly to disk using Shift-JIS encoded path
sjis_names = ["テスト.txt", "日本語ファイル.csv", "画像データ.png"]
for name in sjis_names:
    with open(os.path.join(ROOT, name), "w", encoding="utf-8") as f:
        f.write(f"Content: {name}\n")

os.makedirs(os.path.join(ROOT, "testdir"), exist_ok=True)
with open(os.path.join(ROOT, "testdir", "file1.txt"), "w") as f:
    f.write("File 1 in testdir\n")
with open(os.path.join(ROOT, "testdir", "file2.txt"), "w") as f:
    f.write("File 2 in testdir\n")
nested = os.path.join(ROOT, "testdir", "nested")
os.makedirs(nested, exist_ok=True)
with open(os.path.join(nested, "deep.txt"), "w") as f:
    f.write("Deep nested file\n")

authorizer = DummyAuthorizer()
authorizer.add_user("testuser", "testpass", ROOT, perm="elradfmwMT")
authorizer.add_anonymous(ROOT, perm="elr")

handler = FTPHandler
handler.authorizer = authorizer
handler.passive_ports = range(60000, 60100)
handler.encoding = "shift_jis"   # Shift-JIS for filename encoding

server = FTPServer(("127.0.0.1", 2121), handler)
print("Test FTP server (Shift-JIS) running on ftp://127.0.0.1:2121")
server.serve_forever()
