#!/usr/bin/env python3
"""Upload remaining large assets (.deb and model) to existing GitHub release with flushed output."""
import os
import sys
import hashlib
import requests
import time
from pathlib import Path

GITHUB_TOKEN = os.environ.get("GITHUB_TOKEN")
REPO_OWNER = "nighttrix03-crypto"
REPO_NAME = "tabib-ai"
RELEASE_TAG = "v1.1.0"

if not GITHUB_TOKEN:
    print("❌ GITHUB_TOKEN environment variable not set")
    sys.exit(1)

HEADERS = {
    "Authorization": f"Bearer {GITHUB_TOKEN}",
    "Accept": "application/vnd.github+json",
    "X-GitHub-Api-Version": "2022-11-28",
}
API_BASE = "https://api.github.com"

def get_release_id():
    url = f"{API_BASE}/repos/{REPO_OWNER}/{REPO_NAME}/releases/tags/{RELEASE_TAG}"
    resp = requests.get(url, headers=HEADERS)
    if resp.status_code != 200:
        print(f"❌ Failed to get release: {resp.status_code}")
        resp.raise_for_status()
    release = resp.json()
    print(f"✅ Release {RELEASE_TAG} exists (ID: {release['id']})", flush=True)
    return release['id'], release['upload_url']

class HashingFileReader:
    def __init__(self, file_path, hash_algo=hashlib.sha256):
        self.file_path = file_path
        self.hash = hash_algo()
        self._file = open(file_path, "rb")
        self.size = Path(file_path).stat().st_size
    def read(self, size=-1):
        data = self._file.read(size)
        if data:
            self.hash.update(data)
        return data
    def __iter__(self):
        return self
    def __next__(self):
        chunk = self.read(1024 * 1024)  # 1MB
        if not chunk:
            raise StopIteration
        return chunk
    def close(self):
        self._file.close()
    def hexdigest(self):
        return self.hash.hexdigest()

def upload_asset(upload_url, file_path, content_type):
    file_name = Path(file_path).name
    file_size = Path(file_path).stat().st_size
    print(f"⬆️  Uploading {file_name} ({file_size / 1024 / 1024:.1f} MB)...", flush=True)
    reader = HashingFileReader(file_path)
    upload_headers = HEADERS.copy()
    upload_headers["Content-Type"] = content_type
    upload_url_final = upload_url.split("{")[0] + f"?name={file_name}"
    start_time = time.time()
    try:
        resp = requests.post(
            upload_url_final,
            headers=upload_headers,
            data=reader,
            timeout=3600,
        )
        elapsed = time.time() - start_time
    finally:
        reader.close()
    if resp.status_code in (200, 201):
        asset = resp.json()
        print(f"   SHA256: {reader.hexdigest()}", flush=True)
        print(f"   ✅ Uploaded in {elapsed:.1f}s (ID: {asset['id']})", flush=True)
        return asset
    else:
        print(f"   ❌ Failed: {resp.status_code} - {resp.text}", flush=True)
        resp.raise_for_status()

def main():
    print("🚀 Uploading remaining assets for Tabib AI v1.1.0", flush=True)
    print("=" * 50, flush=True)
    assets = [
        ("/home/mezouarsohaib/tabib-ai/tabib-ai_1.1.0-1_amd64.deb", "application/vnd.debian.binary-package"),
        ("/home/mezouarsohaib/tabib-ai/release-files/gemma2-2b.gguf", "application/octet-stream"),
    ]
    for f, _ in assets:
        if not Path(f).exists():
            print(f"❌ Missing: {f}", flush=True)
            sys.exit(1)
        print(f"✅ Found: {Path(f).name} ({Path(f).stat().st_size / 1024 / 1024:.1f} MB)", flush=True)
    _, upload_url = get_release_id()
    uploaded = []
    for f, ct in assets:
        try:
            a = upload_asset(upload_url, f, ct)
            uploaded.append(a)
        except Exception as e:
            print(f"❌ Error uploading {Path(f).name}: {e}", flush=True)
            sys.exit(1)
    print("\n" + "=" * 50, flush=True)
    print("🎉 Remaining assets uploaded!", flush=True)
    print(f"📋 Release: https://github.com/{REPO_OWNER}/{REPO_NAME}/releases/tag/{RELEASE_TAG}", flush=True)
    print("\n📦 Uploaded assets:", flush=True)
    for a in uploaded:
        print(f"   - {a['name']} ({a['size'] / 1024 / 1024:.1f} MB)", flush=True)

if __name__ == "__main__":
    main()
