"""
Functional test: verify download/upload via Python ftplib against the test server.
Run AFTER test_ftp_server.py is running.
"""
import ftplib
import os
import tempfile

HOST = "127.0.0.1"
PORT = 2121
USER = "testuser"
PASS = "testpass"

def test_download():
    with ftplib.FTP() as ftp:
        ftp.connect(HOST, PORT, timeout=5)
        ftp.login(USER, PASS)

        # list files
        names = ftp.nlst()
        assert "hello.txt" in names, f"Expected hello.txt in {names}"

        # download
        with tempfile.NamedTemporaryFile(delete=False, suffix=".txt") as tmp:
            tmp_path = tmp.name
        try:
            with open(tmp_path, "wb") as f:
                ftp.retrbinary("RETR hello.txt", f.write)
            content = open(tmp_path).read()
            assert "Hello from FTP server" in content, f"Wrong content: {content}"
            print(f"[OK] Download: {len(content)} bytes")
        finally:
            os.unlink(tmp_path)

def test_upload():
    with ftplib.FTP() as ftp:
        ftp.connect(HOST, PORT, timeout=5)
        ftp.login(USER, PASS)

        # upload
        content = b"Uploaded from test script\n"
        with tempfile.NamedTemporaryFile(delete=False, suffix=".txt") as tmp:
            tmp.write(content)
            tmp_path = tmp.name

        try:
            with open(tmp_path, "rb") as f:
                ftp.storbinary("STOR upload_test.txt", f)
            names = ftp.nlst()
            assert "upload_test.txt" in names, f"Upload failed, files: {names}"
            print(f"[OK] Upload: {len(content)} bytes -> upload_test.txt")
        finally:
            os.unlink(tmp_path)

if __name__ == "__main__":
    print("Testing FTP server at localhost:2121...")
    test_download()
    test_upload()
    print("All tests passed!")
