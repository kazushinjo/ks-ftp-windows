"""
Test recursive directory download/upload via Python ftplib.
Verifies the test FTP server supports directory operations.
"""
import ftplib
import os
import shutil
import tempfile

HOST = "127.0.0.1"
PORT = 2121
USER = "testuser"
PASS = "testpass"
ROOT = r"C:\claude\ks-ftp-windows\ftproot"

# Ensure test subdirectory with nested files exists
subdir = os.path.join(ROOT, "testdir")
os.makedirs(subdir, exist_ok=True)
nested = os.path.join(subdir, "nested")
os.makedirs(nested, exist_ok=True)
with open(os.path.join(subdir, "file1.txt"), "w") as f:
    f.write("File 1 in testdir\n")
with open(os.path.join(subdir, "file2.txt"), "w") as f:
    f.write("File 2 in testdir\n")
with open(os.path.join(nested, "deep.txt"), "w") as f:
    f.write("Deep nested file\n")

print("Remote directory structure:")
for root, dirs, files in os.walk(ROOT):
    level = root.replace(ROOT, '').count(os.sep)
    indent = ' ' * 2 * level
    print(f"{indent}{os.path.basename(root)}/")
    for f in files:
        print(f"{indent}  {f}")

print()

# Test: recursive download simulation using ftplib
def list_recursive(ftp, path="/"):
    results = []
    try:
        items = []
        ftp.retrlines(f"LIST {path}", items.append)
        for line in items:
            parts = line.split()
            is_dir = line.startswith('d')
            name = parts[-1]
            if name in (".", ".."):
                continue
            item_path = path.rstrip('/') + '/' + name
            if is_dir:
                results.append(('dir', item_path))
                results.extend(list_recursive(ftp, item_path))
            else:
                results.append(('file', item_path))
    except Exception as e:
        print(f"  Error listing {path}: {e}")
    return results

with ftplib.FTP() as ftp:
    ftp.connect(HOST, PORT, timeout=5)
    ftp.login(USER, PASS)

    print("Recursive directory listing from FTP server:")
    items = list_recursive(ftp, "/testdir")
    for kind, path in items:
        print(f"  [{kind}] {path}")

    assert any(kind == 'dir'  and '/testdir/nested' in path for kind, path in items), \
        "Expected nested/ dir"
    assert any(kind == 'file' and 'file1.txt' in path for kind, path in items), \
        "Expected file1.txt"
    assert any(kind == 'file' and 'deep.txt' in path for kind, path in items), \
        "Expected deep.txt"
    print("\n[OK] Recursive directory structure confirmed on FTP server")

# Test: upload a local directory tree
test_upload_dir = tempfile.mkdtemp(prefix="ksftp_upload_")
os.makedirs(os.path.join(test_upload_dir, "sub"), exist_ok=True)
with open(os.path.join(test_upload_dir, "root.txt"), "w") as f:
    f.write("root file\n")
with open(os.path.join(test_upload_dir, "sub", "child.txt"), "w") as f:
    f.write("child file\n")

def ftp_upload_dir(ftp, local_dir, remote_dir):
    try:
        ftp.mkd(remote_dir)
    except:
        pass
    for entry in os.listdir(local_dir):
        local_path = os.path.join(local_dir, entry)
        remote_path = remote_dir.rstrip('/') + '/' + entry
        if os.path.isdir(local_path):
            ftp_upload_dir(ftp, local_path, remote_path)
        else:
            with open(local_path, 'rb') as f:
                ftp.storbinary(f"STOR {remote_path}", f)

with ftplib.FTP() as ftp:
    ftp.connect(HOST, PORT, timeout=5)
    ftp.login(USER, PASS)
    ftp_upload_dir(ftp, test_upload_dir, "/uploaded_dir")

    items = list_recursive(ftp, "/uploaded_dir")
    assert any('root.txt' in p for _, p in items), "Expected root.txt"
    assert any('child.txt' in p for _, p in items), "Expected child.txt"
    assert any('sub' in p for _, p in items), "Expected sub/"
    print("[OK] Recursive directory upload verified")

shutil.rmtree(test_upload_dir)
print("\nAll directory operation tests passed!")
