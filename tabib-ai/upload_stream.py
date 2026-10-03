#!/usr/bin/env python3
"""
Upload Tabib AI v1.1.0 release assets to GitHub with streaming to avoid large memory usage.
"""
import os
import sys
import hashlib
import requests
import time
from pathlib import Path

# Configuration
GITHUB_TOKEN = os.environ.get("GITHUB_TOKEN")
REPO_OWNER = "nighttrix03-crypto"
REPO_NAME = "tabib-ai"
RELEASE_TAG = "v1.1.0"
RELEASE_NAME = "Tabib AI v1.1.0 - Medical AI Assistant"

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
    """Get or create release."""
    url = f"{API_BASE}/repos/{REPO_OWNER}/{REPO_NAME}/releases/tags/{RELEASE_TAG}"
    resp = requests.get(url, headers=HEADERS)
    if resp.status_code == 200:
        release = resp.json()
        print(f"✅ Release {RELEASE_TAG} exists (ID: {release['id']})")
        return release['id'], release['upload_url']
    # Create new release
    print(f"📦 Creating release {RELEASE_TAG}...")
    url = f"{API_BASE}/repos/{REPO_OWNER}/{REPO_NAME}/releases"
    data = {
        "tag_name": RELEASE_TAG,
        "name": RELEASE_NAME,
        "body": open("/home/mezouarsohaib/tabib-ai/release-files/RELEASE-NOTES-v1.1.0.md").read(),
        "draft": False,
        "prerelease": False
    }
    resp = requests.post(url, headers=HEADERS, json=data)
    resp.raise_for_status()
    release = resp.json()
    print(f"✅ Release created (ID: {release['id']})")
    return release['id'], release['upload_url']

class HashingFileReader:
    """File-like object that reads a file in chunks, updates hash, and yields chunks."""
    def __init__(self, file_path, hash_algo=hashlib.sha256):
        self.file_path = file_path
        self.hash = hash_algo()
        self._file = open(file_path, "rb")
        self.size = Path(file_path).stat().st_size
        self.pos = 0

    def read(self, size=-1):
        data = self._file.read(size)
        if data:
            self.hash.update(data)
            self.pos += len(data)
        return data

    def __iter__(self):
        return self

    def __next__(self):
        chunk = self.read(1024 * 1024)  # 1MB chunks
        if not chunk:
            raise StopIteration
        return chunk

    def close(self):
        self._file.close()

    def hexdigest(self):
        return self.hash.hexdigest()

def upload_asset(upload_url, file_path, content_type="application/octet-stream"):
    """Upload a single asset to the release using streaming."""
    file_name = Path(file_path).name
    file_size = Path(file_path).stat().st_size
    print(f"⬆️  Uploading {file_name} ({file_size / 1024 / 1024:.1f} MB)...")

    # Prepare hashing reader
    reader = HashingFileReader(file_path)
    upload_headers = HEADERS.copy()
    upload_headers["Content-Type"] = content_type

    # Replace {?name,label} in upload_url
    upload_url_final = upload_url.split("{")[0] + f"?name={file_name}"

    start_time = time.time()
    try:
        resp = requests.post(
            upload_url_final,
            headers=upload_headers,
            data=reader,  # requests will iterate over the generator
            timeout=3600,
        )
        elapsed = time.time() - start_time
    finally:
        reader.close()

    if resp.status_code in (200, 201):
        asset = resp.json()
        print(f"   SHA256: {reader.hexdigest()}")
        print(f"   ✅ Uploaded in {elapsed:.1f}s (ID: {asset['id']}, URL: {asset['browser_download_url']})")
        return asset
    else:
        print(f"   ❌ Failed: {resp.status_code} - {resp.text}")
        resp.raise_for_status()

def main():
    print("🚀 Tabib AI v1.1.0 Release Upload (Streaming)")
    print("=" * 50)

    # Files to upload
    assets = [
        ("/home/mezouarsohaib/tabib-ai/installer/dist/TabibAI-Setup.exe", "application/octet-stream"),
        ("/home/mezouarsohaib/tabib-ai/dist/TabibAI.exe", "application/octet-stream"),
        ("/home/mezouarsohaib/tabib-ai/tabib-ai_1.1.0-1_amd64.deb", "application/vnd.debian.binary-package"),
        ("/home/mezouarsohaib/tabib-ai/TabibAI-source.zip", "application/zip"),
        ("/home/mezouarsohaib/tabib-ai/release-files/gemma2-2b.gguf", "application/octet-stream"),
        ("/home/mezouarsohaib/tabib-ai/release-files/SHA256SUMS-v1.1.0.txt", "text/plain"),
    ]

    # Verify all files exist
    for file_path, _ in assets:
        if not Path(file_path).exists():
            print(f"❌ File not found: {file_path}")
            sys.exit(1)
        print(f"✅ Found: {Path(file_path).name} ({Path(file_path).stat().st_size / 1024 / 1024:.1f} MB)")

    # Get release
    release_id, upload_url = get_release_id()

    # Upload each asset
    uploaded = []
    for file_path, content_type in assets:
        try:
            asset = upload_asset(upload_url, file_path, content_type)
            uploaded.append(asset)
        except Exception as e:
            print(f"❌ Failed to upload {Path(file_path).name}: {e}")
            sys.exit(1)

    print("\n" + "=" * 50)
    print("🎉 All assets uploaded successfully!")
    print(f"📋 Release: https://github.com/{REPO_OWNER}/{REPO_NAME}/releases/tag/{RELEASE_TAG}")
    print("\n📦 Uploaded assets:")
    for asset in uploaded:
        print(f"   - {asset['name']} ({asset['size'] / 1024 / 1024:.1f} MB)")

if __name__ == "__main__":
    main()
